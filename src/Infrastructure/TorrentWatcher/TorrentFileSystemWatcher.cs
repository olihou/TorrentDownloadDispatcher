using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using ApplicationCore.Configurations.TorrentWatcher;
using ApplicationCore.Contracts;
using ApplicationCore.Messages.Notification;
using ApplicationCore.Services;
using Infrastructure.TorrentHttpConverter.RealDebrid;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.TorrentWatcher
{
    public class TorrentFileSystemWatcher : FileSystemWatcher, ITorrentWatcher
    {
        private readonly FileSystemWatcherConfiguration _config;
        private readonly ILogger _logger;
        private readonly IMediator _mediator;

        public TorrentFileSystemWatcher(
            IOptions<FileSystemWatcherConfiguration> config,
            ILogger<TorrentFileSystemWatcher> logger,
            IMediator mediator)
        {
            _config = config.Value;
            _logger = logger;
            _mediator = mediator;

            if (_config.Path == null || !Directory.Exists(_config.Path))
            {
                throw new DirectoryNotFoundException();
            }

            this.Path = _config.Path;
            this.Filter = "*.torrent";
            this.NotifyFilter = NotifyFilters.FileName;
            Observable.FromEvent<FileSystemEventHandler, FileSystemEventArgs>(handler =>
            {
                FileSystemEventHandler fsHandler = (sender, e) =>
                {
                    handler(e);
                };

                return fsHandler;
            },
            (obs) =>
            {
                this.Changed += obs;
                this.Created += obs;
            },
            (obs) =>
            {
                this.Changed -= obs;
                this.Created -= obs;
            })
            .Where(p => System.IO.File.ReadAllBytes(p.FullPath).Any())
            .Subscribe(PublishNotification)
            .PublishMediator<RealDebridClient>()
            .Notify<IHttpDownloaderClient>();
        }

        public void Start() => this.EnableRaisingEvents = true;

        void PublishNotification(FileSystemEventArgs args)
        {
            _logger.LogInformation("New file detected ({0}) : {1}", args.ChangeType, args.Name);
            _mediator.Publish(new NewTorrent
            {
                Name = args.Name,
                Content = File.ReadAllBytes(args.FullPath)
            });
        }
    }
}
