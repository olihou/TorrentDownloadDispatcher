using ApplicationCore.Models;

namespace ApplicationCore.Messages.Notification
{
    public class ProgressTracker
    {
        public DownloadBase Torrent { get; set; }
        public DownloadStep DownloadStep { get; set; }
        public double Progress { get; set; }
    }
}
