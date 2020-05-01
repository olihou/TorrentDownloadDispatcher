using System;
using System.Reactive;
using ApplicationCore.Messages.Request;

namespace ApplicationCore.Contract
{
    public interface ITorrentWatcher : IDisposable
    {
        IObservable<TorrentHttpDownloadConverter> Handler { get; }
        IObservable<Unit> ReloadCommand { get; }
    }
}
