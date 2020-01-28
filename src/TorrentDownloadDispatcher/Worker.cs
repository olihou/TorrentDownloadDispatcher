using ApplicationCore.Messages.Notification;
using ApplicationCore.Messages.Request;
using ApplicationCore.Messages.Response;
using ApplicationCore.Services;
using Mediatr.ObservableExtentions;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TorrentDownloadDispatcher
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ITorrentWatcher _watcher;
        private readonly IMediator _mediatr;

        public Worker(ILogger<Worker> logger, ITorrentWatcher watcher, IMediator mediatr)
        {
            _logger = logger;
            _watcher = watcher;
            _mediatr = mediatr;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _watcher
                .NewFileObservable
                .Mediate<NewTorrent, TorrentHttpDownloadConverter, TorrentConvertedToHttpFile>(_mediatr, notification => new TorrentHttpDownloadConverter
                {
                    Id = Guid.NewGuid(),
                    Content = notification.Content,
                    Name = notification.Name
                })
                .Mediate(_mediatr, (unrestictTorrent) => new InvokeDownload
                {
                    Id = unrestictTorrent.Id,
                    FilesToDownload = unrestictTorrent.Files
                }, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
                await Task.Delay(1000);

            _watcher.Dispose();
        }
    }
}
