using System;
using ApplicationCore.Messages.Request;

namespace ApplicationCore.Contract
{
    public interface ITorrentWatcher : IDisposable
    {
        IObservable<TorrentHttpDownloadConverter> Handler { get; }
    }
}
