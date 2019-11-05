using System;
using System.Collections.Generic;
using MediatR;

namespace ApplicationCore.Messages.Notification
{
    public class InvokeDownload : INotification
    {
        public Guid Id { get; set; }
        public List<Uri> FilesToDownload { get; set; }
    }
}
