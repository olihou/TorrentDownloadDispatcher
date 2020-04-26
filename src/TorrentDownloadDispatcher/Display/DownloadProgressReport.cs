using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Runtime.InteropServices;
using ApplicationCore.Contract;
using ApplicationCore.Exceptions;
using ApplicationCore.Messages.Notification;
using ApplicationCore.Models;
using ShellProgressBar;

namespace TorrentDownloadDispatcher.Display
{
    public class DownloadProgressReport : IDownloadProgressTracker
    {
        IDictionary<Guid, List<IProgressBar>> progressTracker;
        IProgressBar mainProgressBar;

        public DownloadProgressReport()
        {
            progressTracker = new Dictionary<Guid, List<IProgressBar>>();
            var overProgressOptions = new ProgressBarOptions
            {
                BackgroundColor = ConsoleColor.DarkGray,
                EnableTaskBarProgress = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            };

            mainProgressBar = new ProgressBar(0, "download progress tracker", overProgressOptions);
        }

        public IObserver<ProgressTracker> GetProgressReportDisplay()
        {
            return Observer.Create<ProgressTracker>(ShowReport, ShowErrors, Dispose);
        }

        private void ShowReport(ProgressTracker progress)
        {
            if (!progressTracker.ContainsKey(progress.Torrent.Id))
            {
                var mainDownloadBar = mainProgressBar.Spawn(
                    100,
                    $"{progress.Torrent.Name} ({progress.Torrent.FileSelected.Count()} / {progress.Torrent.FileAvailable.Count()} seleted file)",
                    new ProgressBarOptions
                    {
                        ForegroundColor = ConsoleColor.Cyan,
                        ForegroundColorDone = ConsoleColor.DarkGreen,
                        ProgressCharacter = '─',
                        BackgroundColor = ConsoleColor.DarkGray,
                        CollapseWhenFinished = true,
                        EnableTaskBarProgress = false
                    }
                );

                var remoteDownloadTorrentBar = mainDownloadBar.Spawn(100, "Torrent -> HTTP Server", new ProgressBarOptions
                {
                    ForegroundColor = ConsoleColor.Yellow,
                    ProgressCharacter = '─',
                    BackgroundColor = ConsoleColor.DarkGray
                });

                var localDownloadBar = mainDownloadBar.Spawn(100, "HTTP Server -> Local", new ProgressBarOptions
                {
                    ForegroundColor = ConsoleColor.Yellow,
                    ProgressCharacter = '─',
                    BackgroundColor = ConsoleColor.DarkGray
                });

                progressTracker.Add(progress.Torrent.Id, new List<IProgressBar>
                {
                    mainDownloadBar as IProgressBar,
                    remoteDownloadTorrentBar as IProgressBar,
                    localDownloadBar as IProgressBar
                });

                if (progress.Torrent.FileSelected.Count() > 1)
                { 
                    List<IProgressBar> filesbar = new List<IProgressBar>();

                    foreach (var file in progress.Torrent.FileSelected)
                    {
                        filesbar.Add(mainDownloadBar.Spawn(100, file, new ProgressBarOptions
                        {
                            ForegroundColor = ConsoleColor.Yellow,
                            ProgressCharacter = '─',
                            BackgroundColor = ConsoleColor.DarkGray
                        }));
                    }
                }
                return;
            }

            switch (progress.DownloadStep)
            {
                case DownloadStep.RemoteTorrentDownloadToHttpServer:
                    RefreshRemoteTorrentDownload(progress);
                    break;
                case DownloadStep.DownloadFromHttpServer:
                    RefreshLocalDownload(progress);
                    break;
            }
        }

        private void RefreshLocalDownload(ProgressTracker progress)
        {
            throw new NotImplementedException();
        }

        private void RefreshRemoteTorrentDownload(ProgressTracker downloadState)
        {
            var bars = progressTracker[downloadState.Torrent.Id];
            var mainDownloadBar = bars.ElementAt(0);
            var remoteDownloadTorrentBar = bars.ElementAt(1);

            int mainDownloadBarPercentage = Convert.ToInt32(
                    100 / (
                    (downloadState.Torrent.FileSelected.Count() + 1) /
                    (downloadState.Progress / 100))
                );

            mainDownloadBar.Tick(mainDownloadBarPercentage);
            remoteDownloadTorrentBar.Tick(Convert.ToInt32(downloadState.Progress));
        }

        private void ShowErrors(Exception error)
        {
            switch (error)
            {
                case RemoteTorrentDownloadException remoteDownloadException:
                    Console.WriteLine($"{remoteDownloadException.Download.Id:N} => report an event");
                    Console.WriteLine($"Reason : {remoteDownloadException.Reason}");
                    break;
            }
        }

        public void Dispose()
        {
            mainProgressBar.Dispose();
        }
    }
}
