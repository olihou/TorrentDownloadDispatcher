using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApplicationCore.Contract;
using ApplicationCore.Messages.Notification;
using ApplicationCore.Messages.Request;
using ApplicationCore.Models;
using ApplicationCore.Utils.Observable;
using Microsoft.Extensions.Logging;

namespace ApplicationCore.Services
{
    public class DownloadPipelineBinder
    {
        private readonly ILogger _logger;
        private readonly ITorrentWatcher _watcher;
        private readonly ITorrentToHttpConverter _torrentConverter;
        private readonly IHttpDownloadInvoker _httpDownloader;
        private readonly IDownloadProgressTracker _progressTracker;

        public DownloadPipelineBinder(
            ILogger<DownloadPipelineBinder> logger,
            ITorrentWatcher watcher,
            ITorrentToHttpConverter torrentConverter,
            IHttpDownloadInvoker httpDownloader,
            IDownloadProgressTracker progressTracker)
        {
            _logger = logger;
            _watcher = watcher;
            _torrentConverter = torrentConverter;
            _httpDownloader = httpDownloader;
            _progressTracker = progressTracker;
        }

        public async Task Connect(CancellationToken ct)
        {
            var tasks = new[]
            {
                Task.Run(() => ConnectPipeline(ct)),
                Task.Run(() => ConnectProgressTracker(ct))
            };

            await Task.WhenAll(tasks);
        }

        private void ConnectPipeline(CancellationToken ct)
        {
            IObservable<DownloadBase> downloadInput =
                _watcher.Handler
                .Select(notification => new TorrentHttpDownloadConverter
                {
                    Id = Guid.NewGuid(),
                    Content = notification.Content,
                    Name = notification.Name
                });

            var isTorrentConverterEnable = _torrentConverter != null;

            if (isTorrentConverterEnable)
            {
                downloadInput = downloadInput
                    .ConvertTorrentToHttp(_torrentConverter, ct)
                    .Select(notif => new InvokeDirectHTTPDownload
                    {
                        Id = notif.Id,
                        FilesToDownload = notif.Files,
                    });
            }

            downloadInput.HttpDownloadInvoker(_httpDownloader, ct);
        }

        private void ConnectProgressTracker(CancellationToken ct)
        {
            _torrentConverter.ProgressHandler().Subscribe(_progressTracker.GetProgressReportDisplay(), ct);
        }
    }
}