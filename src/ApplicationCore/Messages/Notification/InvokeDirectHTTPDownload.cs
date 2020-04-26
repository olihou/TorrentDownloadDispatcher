using System;
using System.Collections.Generic;
using ApplicationCore.Models;
using MediatR;

namespace ApplicationCore.Messages.Notification
{
    public class InvokeDirectHTTPDownload : DownloadBase
    {
        public List<Uri> FilesToDownload { get; set; }
    }
}
