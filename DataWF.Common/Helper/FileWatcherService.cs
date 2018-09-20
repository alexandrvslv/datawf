using System;
using System.IO;

namespace DataWF.Common
{
    public class FileWatcherService
    {
        public event EventHandler<RenamedEventArgs> Renamed;
        public event EventHandler<FileSystemEventArgs> Changed;
        public event EventHandler<EventArgs> EnabledChanged;
        public event EventHandler<EventArgs> Deleted;

        internal void OnChanged(object sender, FileSystemEventArgs e)
        {
            Changed?.Invoke(sender, e);
        }

        internal void OnRenamed(object sender, RenamedEventArgs e)
        {
            Renamed?.Invoke(sender, e);
        }

        internal void OnDeleted(object sender, EventArgs e)
        {
            Deleted?.Invoke(sender, e);
        }

        internal void OnEnabledChanged(object sender, EventArgs e)
        {
            EnabledChanged?.Invoke(sender, e);
        }
    }
}
