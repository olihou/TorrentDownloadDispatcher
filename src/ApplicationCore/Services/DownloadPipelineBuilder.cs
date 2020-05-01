using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApplicationCore.Contract;
using ApplicationCore.Messages.Notification;
using ApplicationCore.Models;
using ApplicationCore.Utils.Observable;
using Microsoft.Extensions.Logging;

namespace ApplicationCore.Services
{
    public class DownloadPipelineBuilder : IDisposable
    {
        private readonly ILogger logger;
        private readonly ITorrentWatcher watcher;
        private readonly ITorrentToHttpConverter torrentConverter;
        private readonly IHttpDownloadInvoker httpDownloader;
        private readonly IDownloadProgressTracker progressTracker;

        private IDisposable cancellationFromWatcherReload;
        
        public DownloadPipelineBuilder(
            ILogger<DownloadPipelineBuilder> logger,
            ITorrentWatcher watcher,
            ITorrentToHttpConverter torrentConverter,
            IHttpDownloadInvoker httpDownloader,
            IDownloadProgressTracker progressTracker)
        {
            this.logger = logger;
            this.watcher = watcher;
            this.torrentConverter = torrentConverter;
            this.httpDownloader = httpDownloader;
            this.progressTracker = progressTracker;
        }

        public Task Connect(CancellationToken mainCancellationToken)
        {
            ConnectProgressTracker(mainCancellationToken);
            ConnectWorkLoad(mainCancellationToken);

            return Task.FromResult(0);
        }

        private void ConnectWorkLoad(CancellationToken mainCancellationToken)
        {
            CancellationToken cancelOrRefreshCt = RegisterMainCancellationToken(mainCancellationToken);
            cancelOrRefreshCt.Register(() => ConnectWorkLoad(mainCancellationToken));
            ConnectDownloadPipeline(cancelOrRefreshCt);
        }

        private CancellationToken RegisterMainCancellationToken(CancellationToken externalCancellationToken)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            cancellationFromWatcherReload?.Dispose();
            cancellationFromWatcherReload = watcher.ReloadCommand.Subscribe((_) => cts.Cancel());
            return CancellationTokenSource.CreateLinkedTokenSource(cts.Token, externalCancellationToken).Token;
        }

        private void ConnectDownloadPipeline(CancellationToken ct)
        {
            try
            {
                IObservable<DownloadBase> downloadInput = watcher.Handler;

                var isTorrentConverterEnable = torrentConverter != null;

                if (isTorrentConverterEnable)
                {
                    downloadInput = downloadInput
                        .ConvertTorrentToHttp(torrentConverter, ct)
                        .Select(notif => new InvokeDirectHTTPDownload
                        {
                            Id = notif.Id,
                            FilesToDownload = notif.Files,
                        });
                }
                downloadInput.HttpDownloadInvoker(httpDownloader, ct);
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "Error during pipeline binding");
            }
        }

        private void ConnectProgressTracker(CancellationToken ct)
        {
            torrentConverter.ProgressHandler().Subscribe(progressTracker.GetProgressReportDisplay(), ct);
        }

        public void Dispose()
        {
            watcher?.Dispose();
            progressTracker?.Dispose();
            cancellationFromWatcherReload?.Dispose();
        }
    }
}