using System;
using MediatR;

namespace ApplicationCore.Messages.Notification
{
    public class NewTorrent : INotification
    {
        public string Name { get; set; }
        public byte[] Content { get; set; }
    }
}
