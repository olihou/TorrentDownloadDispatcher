using System;
namespace ApplicationCore.Services
{
    public interface ITorrentWatcher : IDisposable
    {
        void Start();
    }
}
