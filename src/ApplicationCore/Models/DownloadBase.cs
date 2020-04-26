using System;
namespace ApplicationCore.Models
{
    public abstract class DownloadBase
    {   
        public Guid Id { get; set; }
        public string Name { get; set; }

        public string[] FileAvailable { get; set; }
        public string[] FileSelected { get; set; }
    }
}
