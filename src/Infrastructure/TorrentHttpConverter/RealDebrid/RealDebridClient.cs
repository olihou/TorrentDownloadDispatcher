using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using ApplicationCore.Configurations.TorrentHttpConverter;
using ApplicationCore.Contract;
using ApplicationCore.Exceptions;
using ApplicationCore.Messages.Notification;
using ApplicationCore.Messages.Request;
using ApplicationCore.Messages.Response;
using ApplicationCore.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.TorrentHttpConverter.RealDebrid
{
    public class RealDebridClient : ITorrentToHttpConverter
    {
        private readonly HttpClient _httpClient;
        private readonly RealDebridConfiguration _configuration;
        private readonly IMediator _mediatr;
        private readonly ILogger<RealDebridClient> _logger;
        Subject<ProgressTracker> _progressHandler { get; }

        public RealDebridClient(
            IOptions<RealDebridConfiguration> configuration,
            IMediator mediatr,
            ILogger<RealDebridClient> logger)
        {
            _configuration = configuration.Value;
            _progressHandler = new Subject<ProgressTracker>();
            _mediatr = mediatr;
            _logger = logger;

            _httpClient = new HttpClient
            {
                BaseAddress = _configuration.Uri
            };
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _configuration.ApiKey);
        }

        async Task<string> UploadTorrent(byte[] content)
        {
            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Put, "torrents/addTorrent")
            {
                Content = new ByteArrayContent(content)
            };

            var resp = await _httpClient.SendAsync(message);

            try
            {
                resp.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Torrent upload failed, check your API key or your subscruption");
                _logger.LogInformation(await resp.Content.ReadAsStringAsync());

                //Implement logging depends on reqponse code 
                //HTTP Status Code Reason
                //400 Bad Request(see error message)
                //401 Bad token(expired, invalid)
                //403 Permission denied(account locked, not premium)
                //503 Service unavailable(see error message)

                throw;
            }

            TorrentAdd result = TorrentAdd.FromJson(await resp.Content.ReadAsStringAsync());

            return result.Id;
        }

        async Task SelectFile(string id, DownloadBase download)
        {
            var fileToDownload = new List<string>(1) { "all" };


            using var GetFilesMessage = new HttpRequestMessage(HttpMethod.Get, $"torrents/info/{id}");

            var resp = await _httpClient.SendAsync(GetFilesMessage);
            resp.EnsureSuccessStatusCode();
            var torrentInfo = TorrentInfo.FromJson(await resp.Content.ReadAsStringAsync());

            download.FileAvailable = torrentInfo.Files.Select(p => p.Path).ToArray();

            var fileToKeep = await _mediatr.Send(new SelectFileOfTorrent(torrentInfo.Files.Select(p => p.Path)));

            var files = from file in torrentInfo.Files
                        join fileToKeepSelector in fileToKeep.Filenames on file.Path equals fileToKeepSelector
                        select file.Id.ToString();

            if (files.Count() != torrentInfo.Files.Count)
            {
                fileToDownload = files.ToList();
                download.FileSelected = files.ToArray();
            }
            else
            {
                download.FileSelected = download.FileAvailable;
            }

            using var selectFileMessage = new HttpRequestMessage(HttpMethod.Post, $"torrents/selectFiles/{id}")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                        {
                            "files", String.Join(",", fileToDownload)
                        }
                })
            };

            var res = await _httpClient.SendAsync(selectFileMessage);
            res.EnsureSuccessStatusCode();
        }

        async Task WaitForDownloadCompletion(string id, DownloadBase request, CancellationToken ct)
        {
            Torrent result;

            using var cancelOperation = ct.Register(() => _httpClient.SendAsync(
                new HttpRequestMessage(HttpMethod.Delete, $"torrents/info/{id}")));

            do
            {
                var message = new HttpRequestMessage(HttpMethod.Get, $"torrents/info/{id}");
                //Even the first time to let API donwload file
                Thread.Sleep(_configuration.IntervalCheckDownload);
                var req = await _httpClient.SendAsync(message);
                result = TorrentInfo.FromJson(await req.Content.ReadAsStringAsync());

                switch (result.Status)
                {
                    case "queued":
                    case "downloading":
                    case "uploading":
                        _progressHandler.OnNext(new ProgressTracker
                        {
                            DownloadStep = DownloadStep.RemoteTorrentDownloadToHttpServer,
                            Progress = result.Progress,
                            Torrent = request
                        });
                        break;
                    case "error":
                    case "virus":
                    case "dead":
                        _progressHandler.OnError(new RemoteTorrentDownloadException(result.Status, request));
                        break;

                }
            } while (result.Status != "downloaded");

            _logger.LogTrace("Torrent downloaded");
        }

        async Task<IEnumerable<Uri>> UnrestrictLink(string id)
        {
            var links = new ConcurrentBag<Uri>();
            using var message = new HttpRequestMessage(HttpMethod.Get, $"torrents/info/{id}");

            var resp = await _httpClient.SendAsync(message);

            try
            {
                resp.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError("Torrent upload failed, check your subscription or your torrent", ex);
                throw;
            }

            var info = TorrentInfo.FromJson(await resp.Content.ReadAsStringAsync());

            Parallel.ForEach(info.Links, link =>
            {
                using var message = new HttpRequestMessage(HttpMethod.Post, "unrestrict/link")
                {
                    Content = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        {
                            "link", link.ToString()
                        }
                    })
                };

                // It inside an action so async / await is unusable.
                var response = _httpClient.SendAsync(message).GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();

                var result = Link.FromJson(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());

                links.Add(result.Download);
            });

            return links.ToList();
        }

        public void OnCompleted()
        {
            _httpClient.Dispose();
        }

        public void OnError(Exception error)
        {
            _logger.LogError(error, "Error occured");
        }

        public async Task<TorrentConvertedToHttpFile> Handle(DownloadBase request, CancellationToken cancellationToken = default)
        {
            var downloadRequest = request as TorrentHttpDownloadConverter;

            var torrentId = await UploadTorrent(downloadRequest.Content);

            await SelectFile(torrentId, request);

            _logger.LogTrace("Begin wait for remote download torrent completion : {0}", request.Id);

            await WaitForDownloadCompletion(torrentId, request, cancellationToken);

            _logger.LogTrace("remote torrent downloaded : {0}", request.Id);


            var links = await UnrestrictLink(torrentId);

            return new TorrentConvertedToHttpFile
            {
                Id = request.Id,
                Name = request.Name,
                Files = links.ToList()
            };
        }

        IObservable<TorrentConvertedToHttpFile> ITorrentToHttpConverter.Handler(IObservable<DownloadBase> source, CancellationToken ct)
        {
            return Observable.Create<TorrentConvertedToHttpFile>((obs) =>
            {
                var subscription = source.Subscribe(
                    async val => obs.OnNext(await Handle(val, ct)),
                    OnError,
                    OnCompleted);

                return subscription;
            });
        }

        public IObservable<ProgressTracker> ProgressHandler() => _progressHandler;
    }
}
