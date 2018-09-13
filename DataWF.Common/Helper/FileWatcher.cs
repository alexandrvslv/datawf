using System;
using System.Diagnostics;
using System.IO;

namespace DataWF.Common
{
    //TODO service?
    public class FileWatcher : IDisposable
    {
        public static event EventHandler<RenamedEventArgs> Renamed;
        public static event EventHandler<FileSystemEventArgs> Changed;
        public static event EventHandler<EventArgs> EnabledChanged;
        public static event EventHandler<EventArgs> Deleted;

        //https://stackoverflow.com/a/721743
        public FileWatcher(string filePath)
        {
            FilePath = filePath;
            Watcher = new FileSystemWatcher();
            Watcher.Path = Path.GetDirectoryName(filePath);
            /* Watch for changes in LastAccess and LastWrite times, and 
               the renaming of files or directories. */
            Watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
               | NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Size;
            // Only watch text files.
            Watcher.Filter = Path.GetFileName(filePath);

            // Add event handlers.
            Watcher.Changed += new FileSystemEventHandler(OnChanged);
            Watcher.Created += new FileSystemEventHandler(OnChanged);
            Watcher.Deleted += new FileSystemEventHandler(OnDeleted);
            Watcher.Renamed += new RenamedEventHandler(OnRenamed);

            // Begin watching.
            Watcher.EnableRaisingEvents = true;
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            Debug.WriteLine($"{e.OldFullPath} - {e.ChangeType}");
            Renamed?.Invoke(Tag ?? this, e);
            //FilePath = e.FullPath;
            //Watcher.Path = Path.GetDirectoryName(e.FullPath);
            //Watcher.Filter = Path.GetFileName(e.FullPath);
            OnChanged(sender, e);
        }

        private void OnDeleted(object sender, FileSystemEventArgs e)
        {
            Debug.WriteLine($"{e.FullPath} - {e.ChangeType}");
            //Dispose();
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            Debug.WriteLine($"{e.FullPath} - {e.ChangeType}");
            IsChanged = true;
            Changed?.Invoke(Tag ?? this, e);
        }

        public void Dispose()
        {
            Watcher?.Dispose();
            Deleted?.Invoke(Tag ?? this, EventArgs.Empty);
        }

        public string FilePath { get; set; }

        public FileSystemWatcher Watcher { get; }

        public bool Enabled
        {
            get => Watcher.EnableRaisingEvents;
            set
            {
                if (Enabled != value)
                {
                    Watcher.EnableRaisingEvents = value;
                    EnabledChanged?.Invoke(Tag ?? this, EventArgs.Empty);
                }

            }
        }

        public object Tag { get; set; }

        public bool IsChanged { get; set; }
    }
}
