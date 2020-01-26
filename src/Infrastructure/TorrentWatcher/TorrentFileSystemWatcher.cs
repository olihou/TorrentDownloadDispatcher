using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using ApplicationCore.Configurations.TorrentWatcher;
using ApplicationCore.Messages.Notification;
using ApplicationCore.Services;
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
            .Buffer(1, 2)
            .Subscribe(PublishNotification);
        }

        public void Start() => this.EnableRaisingEvents = true;

        void PublishNotification(IList<FileSystemEventArgs> args)
        {
            var arg = args.First();
            _logger.LogInformation("New file detected ({0}) : {1}", arg.ChangeType, arg.Name);
            _mediator.Publish(new NewTorrent
            {
                Name = arg.Name,
                Content = File.ReadAllBytes(arg.FullPath)
            });
        }
    }
}
