using System;
using ApplicationCore.Contract;
using ApplicationCore.Messages.Notification;
using ApplicationCore.Messages.Request;
using ApplicationCore.Messages.Response;

namespace ApplicationCore.Utils.Observable
{
    public static class Extentions
    {
        public static IObservable<TorrentConvertedToHttpFile> ConvertTorrentToHttp(this IObservable<TorrentHttpDownloadConverter> source, ITorrentToHttpConverter instance)
        {
            return instance.Handler(source);
        }

        public static IDisposable HttpDownloadInvoker(this IObservable<InvokeDownload> source, IHttpDownloadInvoker instance)
        {
            return source.Subscribe(instance.Handler);
        }
    }
}
