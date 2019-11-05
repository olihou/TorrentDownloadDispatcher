using System.IO;
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
            this.Changed += PublishNotification;
            this.Created += PublishNotification;
        }

        public void Start() => this.EnableRaisingEvents = true;

        void PublishNotification(object sender, FileSystemEventArgs args) =>
            _mediator.Publish(new NewTorrent
            {
                Name = args.Name,
                Content = File.ReadAllBytes(args.FullPath)
            });
    }
}
