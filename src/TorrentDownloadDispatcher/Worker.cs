using System.Threading;
using System.Threading.Tasks;
using ApplicationCore.Services;
using Microsoft.Extensions.Hosting;

namespace TorrentDownloadDispatcher
{
    public class Worker : BackgroundService
    {
        private readonly DownloadPipelineBinder pipelineBinder;

        public Worker(DownloadPipelineBinder pipelineBinder)
        {
            this.pipelineBinder = pipelineBinder;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Run(() => pipelineBinder.Connect(stoppingToken));
        }
    }
}
