using System.Collections.Generic;
using System.Linq;
using ApplicationCore.Messages.Response;
using MediatR;

namespace ApplicationCore.Messages.Request
{
    public class SelectFileOfTorrent : IRequest<SelectedFileOfTorrent>
    {
        public SelectFileOfTorrent(IEnumerable<string> filenames) => Filenames = filenames.ToList();

        public List<string> Filenames { get; set; }
    }
}
