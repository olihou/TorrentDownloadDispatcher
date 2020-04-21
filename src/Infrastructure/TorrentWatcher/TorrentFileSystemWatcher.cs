using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using ApplicationCore.Configurations.TorrentWatcher;
using ApplicationCore.Contract;
using ApplicationCore.Messages.Notification;
using ApplicationCore.Utils.Observable;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.TorrentWatcher
{
    public class TorrentFileSystemWatcher : FileSystemWatcher, ITorrentWatcher
    {
        private readonly FileSystemWatcherConfiguration _config;
        private readonly ILogger _logger;

        public TorrentFileSystemWatcher(
            IOptions<FileSystemWatcherConfiguration> config,
            ILogger<TorrentFileSystemWatcher> logger,
            IMediator mediator)
        {
            _config = config.Value;
            _logger = logger;

            if (_config.Path == null || !Directory.Exists(_config.Path))
            {
                throw new DirectoryNotFoundException();
            }

            this.Path = _config.Path;
            this.Filter = "*.torrent";
            this.NotifyFilter = NotifyFilters.FileName;
        }

        IObservable<NewTorrent> ITorrentWatcher.Handler => Observable.FromEvent<FileSystemEventHandler, FileSystemEventArgs>(handler =>
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
                this.EnableRaisingEvents = true;
            },
            (obs) =>
            {
                this.EnableRaisingEvents = false;
                this.Changed -= obs;
                this.Created -= obs;
            })
            .Buffer(TimeSpan.FromSeconds(2))
            .Select(p => p.GroupBy(gb => gb.FullPath))
            .SelectMany(p => p.Select(p => p.First()))
            .Do((arg) => _logger.LogInformation("New file detected ({0}) : {1}", arg.ChangeType, arg.Name))
            .Select(arg => new NewTorrent
            {
                Name = arg.Name,
                Content = File.ReadAllBytes(arg.FullPath)
            });
    }
}
