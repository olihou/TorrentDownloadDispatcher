using ApplicationCore.Configurations.TorrentWatcher;
using ApplicationCore.Messages.Notification;
using ApplicationCore.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;

namespace Infrastructure.TorrentWatcher
{
    public class TorrentFileSystemWatcher : FileSystemWatcher, ITorrentWatcher
    {
        private readonly FileSystemWatcherConfiguration _config;
        private readonly ILogger _logger;



        public TorrentFileSystemWatcher(
            IOptions<FileSystemWatcherConfiguration> config,
            ILogger<TorrentFileSystemWatcher> logger)
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

        IObservable<NewTorrent> ITorrentWatcher.NewFileObservable => Observable.FromEvent<FileSystemEventHandler, FileSystemEventArgs>(handler =>
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
        .Where(p => File.ReadAllBytes(p.FullPath).Any())
        .Do(args => _logger.LogInformation("New file detected ({0}) : {1}", args.ChangeType, args.Name))
        .Select(args => new NewTorrent
        {
            Content = File.ReadAllBytes(args.FullPath),
            Name = args.FullPath
        });
    }
}
