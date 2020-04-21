using System;
using ApplicationCore.Messages.Notification;
using ApplicationCore.Messages.Response;

namespace ApplicationCore.Contract
{
    public interface IHttpDownloadInvoker
    {
        IObserver<InvokeDownload> Handler { get; }
    }
}
