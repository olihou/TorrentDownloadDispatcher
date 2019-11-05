using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApplicationCore.Configurations;
using ApplicationCore.Messages.Request;
using ApplicationCore.Messages.Response;
using MediatR;
using Microsoft.Extensions.Options;

namespace ApplicationCore.Services
{
    public class TorrentFilesSelectorHandler : IRequestHandler<SelectFileOfTorrent, SelectedFileOfTorrent>
    {
        private readonly TorrentConfiguration _config;

        public TorrentFilesSelectorHandler(IOptions<TorrentConfiguration> config)
        {
            _config = config.Value;
        }

        public Task<SelectedFileOfTorrent> Handle(SelectFileOfTorrent request, CancellationToken cancellationToken)
        {
            var result = from file in request.Filenames
                         where !_config.ExcludeExtentions.Any(p => file.EndsWith(p, StringComparison.OrdinalIgnoreCase))
                         select file;

            return Task.FromResult(new SelectedFileOfTorrent
            {
                Filenames = result.ToList()
            }); 
        }
    }
}
