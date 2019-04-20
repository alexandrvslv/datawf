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
            Stamp = model is IStampKey stamp ? (stamp.Stamp ?? DateTime.UtcNow) : DateTime.UtcNow;
            ModelView = modelView;
            FilePath = filePath;
            Service = service ?? FileWatcherService.Instance;
            Enabled = enabled;
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

        public DateTime Stamp { get; set; }

        public FileSystemWatcher Watcher { get; private set; }

        public bool Enabled
        {
            get => Watcher?.EnableRaisingEvents ?? false;
            set
            {
                if (Enabled != value)
                {
                    if (value)
                    {
                        if (Watcher == null)
                        {
                            Watcher = new FileSystemWatcher
                            {
                                Path = Path.GetDirectoryName(FilePath),
                                NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
                  | NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Size,
                                Filter = Path.GetFileName(FilePath),
                            };

                            // Add event handlers.
                            Watcher.Changed += new FileSystemEventHandler(OnChanged);
                            Watcher.Created += new FileSystemEventHandler(OnChanged);
                            Watcher.Deleted += new FileSystemEventHandler(OnDeleted);
                            Watcher.Renamed += new RenamedEventHandler(OnRenamed);
                        }
                        Watcher.EnableRaisingEvents = true;
                        Service.WatchList.Add(this);
                    }
                    else
                    {
                        if (Watcher != null)
                        {
                            Watcher.EnableRaisingEvents = false;
                        }
                        Service.WatchList.Remove(this);
                    }
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
