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
        public FileWatcher(string filePath, IFileModel model, IFileModelView modelView, bool enabled = true, FileWatcherService service = null)
        {
            Model = model;
            ModelView = modelView;
            FilePath = filePath;
            Watcher = new FileSystemWatcher
            {
                Path = Path.GetDirectoryName(filePath),
                NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
               | NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Size,
                Filter = Path.GetFileName(filePath),
                EnableRaisingEvents = enabled
            };

            // Add event handlers.
            Watcher.Changed += new FileSystemEventHandler(OnChanged);
            Watcher.Created += new FileSystemEventHandler(OnChanged);
            Watcher.Deleted += new FileSystemEventHandler(OnDeleted);
            Watcher.Renamed += new RenamedEventHandler(OnRenamed);

            Service = service ?? FileWatcherService.Instance;
            if (enabled)
            {
                Service.WatchList.Add(this);
            }
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            Debug.WriteLine($"{e.OldFullPath} - {e.ChangeType}");
            Service.OnRenamed(this, e);
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
            Service.OnChanged(this, e);
        }

        public void Dispose()
        {
            Service.OnDeleted(this, EventArgs.Empty);
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
                    Service.OnEnabledChanged(this, EventArgs.Empty);
                    OnPropertyChanged();
                }

            }
        }

        public IFileModel Model { get; set; }

        public IFileModelView ModelView { get; }

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
