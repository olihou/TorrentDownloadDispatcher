using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApplicationCore.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TorrentDownloadDispatcher
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ITorrentWatcher _watcher;

        public Worker(ILogger<Worker> logger, ITorrentWatcher watcher)
        {
            _logger = logger;
            _watcher = watcher;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _watcher.Start();

            while (!stoppingToken.IsCancellationRequested)
                await Task.Delay(1000);

            _watcher.Dispose();
        }
    }
}
