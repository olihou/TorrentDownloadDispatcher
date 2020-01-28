using ApplicationCore.Messages.Notification;
using System;

namespace ApplicationCore.Services
{
    public interface ITorrentWatcher : IDisposable
    {
        IObservable<NewTorrent> NewFileObservable { get; }
    }
}
