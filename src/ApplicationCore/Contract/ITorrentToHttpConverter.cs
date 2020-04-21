using System;
using ApplicationCore.Messages.Request;
using ApplicationCore.Messages.Response;

namespace ApplicationCore.Contract
{
    public interface ITorrentToHttpConverter
    {
        IObservable<TorrentConvertedToHttpFile> Handler(IObservable<TorrentHttpDownloadConverter> source);
    }
}
