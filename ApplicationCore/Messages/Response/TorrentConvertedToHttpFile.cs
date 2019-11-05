using System;
using System.Collections.Generic;

namespace ApplicationCore.Messages.Response
{
    public class TorrentConvertedToHttpFile
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public List<Uri> Files { get; set; }
    }
}
