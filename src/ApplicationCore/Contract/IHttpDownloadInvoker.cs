using System;
using ApplicationCore.Messages.Notification;
using ApplicationCore.Messages.Response;
using ApplicationCore.Models;

namespace ApplicationCore.Contract
{
    public interface IHttpDownloadInvoker
    {
        IObserver<DownloadBase> Handler { get; }
    }
}
