using System;
using System.Threading;
using ApplicationCore.Messages.Notification;
using ApplicationCore.Messages.Response;
using ApplicationCore.Models;

namespace ApplicationCore.Contract
{
    public interface ITorrentToHttpConverter
    {
        IObservable<TorrentConvertedToHttpFile> Handler(IObservable<DownloadBase> source, CancellationToken ct);
        IObservable<ProgressTracker> ProgressHandler();
    }
}
