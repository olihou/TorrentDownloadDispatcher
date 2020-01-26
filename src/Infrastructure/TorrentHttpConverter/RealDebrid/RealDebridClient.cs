using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using ApplicationCore.Configurations.TorrentHttpConverter;
using ApplicationCore.Messages.Request;
using ApplicationCore.Messages.Response;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.TorrentHttpConverter.RealDebrid
{
    public class RealDebridClient : IRequestHandler<TorrentHttpDownloadConverter, TorrentConvertedToHttpFile>
    {
        private readonly HttpClient _httpClient;
        private readonly RealDebridConfiguration _configuration;
        private readonly IMediator _mediatr;
        private readonly ILogger<RealDebridClient> _logger;

        public RealDebridClient(
            IOptions<RealDebridConfiguration> configuration,
            IMediator mediatr,
            ILogger<RealDebridClient> logger)
        {
            _configuration = configuration.Value;
            _mediatr = mediatr;
            _logger = logger;

            _httpClient = new HttpClient
            {
                BaseAddress = _configuration.Uri
            };
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _configuration.ApiKey);
        }

        public async Task<TorrentConvertedToHttpFile> Handle(TorrentHttpDownloadConverter request, CancellationToken cancellationToken)
        {
            var torrentId = await UploadTorrent(request.Content);

            await SelectFile(torrentId);

            await WaitForDownloadCompletion(torrentId);

            var links = await UnrestrictLink(torrentId);

            return new TorrentConvertedToHttpFile
            {
                Id = request.Id,
                Name = request.Name,
                Files = links.ToList()
            };
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
                throw;
            }

            TorrentAdd result = TorrentAdd.FromJson(await resp.Content.ReadAsStringAsync());

            return result.Id;
        }

        async Task SelectFile(string id)
        {
            var fileToDownload = new List<string>(1) { "all" };


            using var GetFilesMessage = new HttpRequestMessage(HttpMethod.Get, $"torrents/info/{id}");

            var resp = await _httpClient.SendAsync(GetFilesMessage);
            resp.EnsureSuccessStatusCode();
            var torrentInfo = TorrentInfo.FromJson(await resp.Content.ReadAsStringAsync());

            var fileToKeep = await _mediatr.Send(new SelectFileOfTorrent(torrentInfo.Files.Select(p => p.Path)));

            var files = from file in torrentInfo.Files
                        join fileToKeepSelector in fileToKeep.Filenames on file.Path equals fileToKeepSelector
                        select file.Id.ToString();

            if (files.Count() != torrentInfo.Files.Count)
            {
                fileToDownload = files.ToList();
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

        async Task WaitForDownloadCompletion(string id)
        {
            Torrent result;
            do
            {
                var message = new HttpRequestMessage(HttpMethod.Get, $"torrents/info/{id}");
                //Even the first time to let API donwload file
                Thread.Sleep(_configuration.IntervalCheckDownload);
                var req = await _httpClient.SendAsync(message);
                result = TorrentInfo.FromJson(await req.Content.ReadAsStringAsync());
            } while (result.Status != "downloaded");
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
    }
}
