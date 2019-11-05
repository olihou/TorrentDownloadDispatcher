using System;
using ApplicationCore.Messages.Response;
using MediatR;

namespace ApplicationCore.Messages.Request
{
    public class TorrentHttpDownloadConverter : IRequest<TorrentConvertedToHttpFile>
    {
        public Guid Id { get; set; }
        public byte[] Content { get; set; }
        public string Name { get; set; }
    }
}
