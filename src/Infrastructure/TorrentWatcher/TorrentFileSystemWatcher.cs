using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ApplicationCore.Configurations.TorrentWatcher;
using ApplicationCore.Contract;
using ApplicationCore.Messages.Request;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.TorrentWatcher
{
    public class TorrentFileSystemWatcher : FileSystemWatcher, ITorrentWatcher
    {
        private readonly ILogger logger;
        private FileSystemWatcherConfiguration config;
        private readonly IOptionsMonitor<FileSystemWatcherConfiguration> configMonitor;
        private readonly IOptionsMonitorCache<FileSystemWatcherConfiguration> configMonitorCache;
        private readonly IOptionsChangeTokenSource<FileSystemWatcherConfiguration> configct;
        private readonly Subject<Unit> reloadNotification = new Subject<Unit>();

        IObservable<Unit> ITorrentWatcher.ReloadCommand => reloadNotification;

        public TorrentFileSystemWatcher(
            IOptionsMonitor<FileSystemWatcherConfiguration> config,
            //IOptionsMonitorCache<FileSystemWatcherConfiguration> configCache,
            //IOptionsChangeTokenSource<FileSystemWatcherConfiguration> configct,
            ILogger<TorrentFileSystemWatcher> logger)
        {
            this.logger = logger;
            this.configMonitor = config;
            //this.configct = configct;
            //this.configMonitorCache = configCache;
            config.OnChange(OnConfigurationChange);
            ConfigureFileWatcher(config.CurrentValue);
        }

        private void ConfigureFileWatcher(FileSystemWatcherConfiguration config)
        {
            this.config = config;

            if (this.config.Path == null || !Directory.Exists(this.config.Path))
            {
                throw new DirectoryNotFoundException(this.config.Path);
            }

            base.Path = this.config.Path;
            base.Filter = "*.torrent";
            base.NotifyFilter = NotifyFilters.FileName;
        }

        private void OnConfigurationChange(FileSystemWatcherConfiguration configu, string _)
        {
            ConfigureFileWatcher(configu);
            reloadNotification.OnNext(Unit.Default);
        }

        IObservable<TorrentHttpDownloadConverter> ITorrentWatcher.Handler =>
            Observable.FromEvent<FileSystemEventHandler, FileSystemEventArgs>(handler =>
            {
                void fsHandler(object sender, FileSystemEventArgs e)
                {
                    handler(e);
                }

                return fsHandler;
            },
            (fsHandler) =>
            {
                this.Changed += fsHandler;
                this.Created += fsHandler;
                this.EnableRaisingEvents = true;
            },
            (fsHandler) =>
            {
                this.EnableRaisingEvents = false;
                this.Changed -= fsHandler;
                this.Created -= fsHandler;
            })
            .Buffer(TimeSpan.FromSeconds(2))
            .Select(p => p.GroupBy(gb => gb.FullPath))
            .SelectMany(p => p.Select(p => p.First()))
            .Do((arg) => logger.LogTrace("New file detected ({0}) : {1}", arg.ChangeType, arg.Name))
            .Select(notification => new TorrentHttpDownloadConverter
            {
                Id = Guid.NewGuid(),
                Content = File.ReadAllBytes(notification.FullPath),
                Name = notification.Name
            })
            .TakeUntil(reloadNotification);
    }
}
