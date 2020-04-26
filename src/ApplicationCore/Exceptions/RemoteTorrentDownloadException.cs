using System;
using ApplicationCore.Models;

namespace ApplicationCore.Exceptions
{
    public class RemoteTorrentDownloadException : Exception
    {
        public string Reason { get; set; }
        public DownloadBase Download { get; set; }

        public RemoteTorrentDownloadException(string reason, DownloadBase download)
        {
            Reason = reason;
            Download = download;
        }
    }
}
