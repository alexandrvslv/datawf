using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace DataWF.Common
{
    public class FileWatcher : IDisposable, INotifyPropertyChanged
    {
        private bool isChanged;
        private FileWatcherService service;

        //https://stackoverflow.com/a/721743
        public FileWatcher(FileWatcherService service, string filePath)
        {
            Service = service;
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

            Enabled = true;
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            Debug.WriteLine($"{e.OldFullPath} - {e.ChangeType}");
            Service.OnRenamed(Tag ?? this, e);
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
            Service.OnChanged(Tag ?? this, e);
        }

        public void Dispose()
        {
            Service.OnDeleted(Tag ?? this, EventArgs.Empty);
            Watcher?.Dispose();
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
                    Service.OnEnabledChanged(Tag ?? this, EventArgs.Empty);
                    OnPropertyChanged();
                }

            }
        }

        public object Tag { get; set; }

        public bool IsChanged
        {
            get => isChanged;
            set
            {
                if (IsChanged != value)
                {
                    isChanged = value;
                    OnPropertyChanged();
                }
            }
        }

        public FileWatcherService Service { get => service; set => service = value; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


    }
}
