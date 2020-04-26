using System;
using ApplicationCore.Messages.Response;
using ApplicationCore.Models;
using MediatR;

namespace ApplicationCore.Messages.Request
{
    public class TorrentHttpDownloadConverter : DownloadBase
    {
        public byte[] Content { get; set; }
    }
}
