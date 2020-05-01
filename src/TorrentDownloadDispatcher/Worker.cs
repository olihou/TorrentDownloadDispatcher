using System.Threading;
using System.Threading.Tasks;
using ApplicationCore.Services;
using Microsoft.Extensions.Hosting;

namespace TorrentDownloadDispatcher.Console
{
    public class Worker : BackgroundService
    {
        private readonly DownloadPipelineBuilder pipelineBinder;

        public Worker(DownloadPipelineBuilder pipelineBinder)
        {
            this.pipelineBinder = pipelineBinder;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await Task.Run(function: () => pipelineBinder.Connect(cancellationToken));
        }
    }
}
