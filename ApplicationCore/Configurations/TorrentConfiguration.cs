namespace ApplicationCore.Configurations
{
    public class TorrentConfiguration
    {
        public string[] ExcludeExtentions { get; set; }
        public int MaxDegreeParallelism => 3;
        public int MaxSimultaneousDownload => 1;
    }
}
