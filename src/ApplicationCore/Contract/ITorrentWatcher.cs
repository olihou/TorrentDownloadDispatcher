using System;
using ApplicationCore.Messages.Notification;

namespace ApplicationCore.Contract
{
    public interface ITorrentWatcher : IDisposable
    {
        IObservable<NewTorrent> Handler { get; }
    }
}
