using System;
using ApplicationCore.Messages.Notification;

namespace ApplicationCore.Contract
{
    public interface IDownloadProgressTracker : IDisposable
    {
        IObserver<ProgressTracker> GetProgressReportDisplay();
    }
}
