using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApplicationCore.Contract;
using ApplicationCore.Messages.Request;
using ApplicationCore.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ApplicationCore.Utils.Observable;
using ApplicationCore.Messages.Notification;

namespace TorrentDownloadDispatcher
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ITorrentWatcher _watcher;
        private readonly ITorrentToHttpConverter _torrentConverter;
        private readonly IHttpDownloadInvoker _httpDownloader;

        public Worker(ILogger<Worker> logger, ITorrentWatcher watcher, ITorrentToHttpConverter torrentConverter, IHttpDownloadInvoker httpDownloader)
        {
            _logger = logger;
            _watcher = watcher;
            _torrentConverter = torrentConverter;
            _httpDownloader = httpDownloader;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var chain =
                _watcher.Handler
                .Select(notification => new TorrentHttpDownloadConverter
                {
                    Id = Guid.NewGuid(),
                    Content = notification.Content,
                    Name = notification.Name
                })
                .ConvertTorrentToHttp(_torrentConverter)
                .Select(notif => new InvokeDownload
                {
                    Id = notif.Id,
                    FilesToDownload = notif.Files,
                })
                .HttpDownloadInvoker(_httpDownloader);
            
            while (!stoppingToken.IsCancellationRequested)
                await Task.Delay(1000);
        }
    }
}
