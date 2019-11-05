using System;
using System.Threading;
using System.Threading.Tasks;
using ApplicationCore.Messages.Notification;
using ApplicationCore.Messages.Request;
using MediatR;

namespace ApplicationCore.Services
{
    public class NewTorrentHandler : INotificationHandler<NewTorrent>
    {
        private readonly IMediator _mediator;

        public NewTorrentHandler(
            IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task Handle(NewTorrent notification, CancellationToken cancellationToken)
        {
            var unrestictTorrent = await _mediator.Send(new TorrentHttpDownloadConverter
            {
                Id = Guid.NewGuid(),
                Content = notification.Content,
                Name = notification.Name
            });

            _ = _mediator.Publish(new InvokeDownload
            {
                Id = unrestictTorrent.Id,
                FilesToDownload = unrestictTorrent.Files
            });
        }
    }
}
