using System;
using MediatR;

namespace ApplicationCore.Messages.Notification
{
    public class NewTorrent
    {
        public string Name { get; set; }
        public byte[] Content { get; set; }
    }
}
