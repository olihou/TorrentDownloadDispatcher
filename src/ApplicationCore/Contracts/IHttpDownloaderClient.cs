using ApplicationCore.Messages.Notification;
using MediatR;

namespace ApplicationCore.Contracts
{
    public interface IHttpDownloaderClient : INotificationHandler<InvokeDownload>
    {
    }
}
