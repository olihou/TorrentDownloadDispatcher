using System;
namespace ApplicationCore.Configurations.TorrentHttpConverter
{
    public class RealDebridConfiguration
    {
        public Uri Uri { get; set; }
        public string ApiKey { get; set; }
        public int IntervalCheckDownload => 1500;
    }
}
