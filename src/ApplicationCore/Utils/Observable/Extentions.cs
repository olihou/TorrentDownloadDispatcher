using System;
using System.Threading;
using ApplicationCore.Contract;
using ApplicationCore.Messages.Notification;
using ApplicationCore.Messages.Response;
using ApplicationCore.Models;

namespace ApplicationCore.Utils.Observable
{
    public static class Extentions
    {
        public static IObservable<TorrentConvertedToHttpFile> ConvertTorrentToHttp(this IObservable<DownloadBase> source, ITorrentToHttpConverter instance, CancellationToken ct)
        {
            return instance.Handler(source, ct);
        }

        public static IDisposable HttpDownloadInvoker(this IObservable<DownloadBase> source, IHttpDownloadInvoker instance)
        {
            return source.Subscribe(instance.Handler);
        }

        public static void HttpDownloadInvoker(this IObservable<DownloadBase> source, IHttpDownloadInvoker instance, CancellationToken ct)
        {
            source.Subscribe(instance.Handler, ct);
        }
    }
}
