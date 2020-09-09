using System;
using System.IO;

namespace DataWF.Common
{
    public class FileWatcherService
    {
        public static FileWatcherService Instance { get; } = new FileWatcherService();

        public SelectableList<FileWatcher> WatchList { get; } = new SelectableList<FileWatcher>();

        public event EventHandler<RenamedEventArgs> Renamed;
        public event EventHandler<FileSystemEventArgs> Changed;
        public event EventHandler<EventArgs> EnabledChanged;
        public event EventHandler<EventArgs> Deleting;

        internal void OnChanged(object sender, FileSystemEventArgs e)
        {
            Changed?.Invoke(sender, e);
        }

        internal void OnRenamed(object sender, RenamedEventArgs e)
        {
            Renamed?.Invoke(sender, e);
        }

        internal void OnDeleting(object sender, EventArgs e)
        {
            Deleting?.Invoke(sender, e);
            WatchList.Remove(sender);
        }

        internal void OnEnabledChanged(object sender, EventArgs e)
        {
            EnabledChanged?.Invoke(sender, e);
        }
    }
}
