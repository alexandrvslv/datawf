﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace DataWF.Common
{
    public class FileWatcher : IDisposable, INotifyPropertyChanged
    {
        private bool isChanged;
        private FileWatcherService service;
        private bool disposed;
        private IFileModel model;

        //https://stackoverflow.com/a/721743
        public FileWatcher(string filePath, IFileModel model, object modelView, bool enabled = true, FileWatcherService service = null)
        {
            Model = model;
            ModelToken = model?.Token ?? 0;
            ModelView = modelView;
            FilePath = filePath;
            Service = service ?? FileWatcherService.Instance;
            Enabled = enabled;
        }

        public string FilePath { get; set; }

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
                        Service.WatchList.Remove(this);
                        if (Watcher != null)
                        {
                            Watcher.EnableRaisingEvents = false;
                            Watcher.Dispose();
                            Watcher = null;
                        }
                    }
                    Service.OnEnabledChanged(this, EventArgs.Empty);

                    OnPropertyChanged();
                }

            }
        }

        public IFileModel Model
        {
            get => model;
            set
            {
                model = value;
            }
        }

        public uint ModelToken { get; set; }

        public object ModelView { get; }

        public bool IsChanged
        {
            get => isChanged;
            set
            {
                if (IsChanged != value)
                {
                    isChanged = value;
                    if (Model is SynchronizedItem synched
                        && synched.SyncStatus != SynchronizedStatus.New
                        && synched.SyncStatus != SynchronizedStatus.Load)
                    {
                        if (value)
                        {
                            synched.Changes[nameof(IFileModel.FileWatcher)] = this;
                            synched.SyncStatus = SynchronizedStatus.Edit;
                        }
                        else if (synched.SyncStatus == SynchronizedStatus.Edit
                            && synched.Changes.ContainsKey(nameof(IFileModel.FileWatcher))
                            && synched.Changes.Count == 1)
                        {
                            synched.SyncStatus = SynchronizedStatus.Actual;
                        }

                        synched.OnPropertyChanged(nameof(IFileModel.FileWatcher));
                    }
                    OnPropertyChanged();
                }
            }
        }

        public FileWatcherService Service { get => service; set => service = value; }

        public event PropertyChangedEventHandler PropertyChanged;

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
            if (!disposed)
            {
                disposed = true;
                try { Service.OnDeleting(this, EventArgs.Empty); }
                finally { Watcher?.Dispose(); }
            }
        }

        public DateTime GetLastWrite() => File.GetLastWriteTimeUtc(FilePath);

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public async Task Rename(string fileName)
        {
            var newPath = Path.Combine(Path.GetDirectoryName(FilePath), fileName);

            if (Service?.CanRename ?? false)
            {
                await Service.OnRenaming(this, new RenamedEventArgs(WatcherChangeTypes.Renamed, Path.GetDirectoryName(FilePath), fileName, Path.GetFileName(FilePath)));
            }
            else
            {
                if (File.Exists(FilePath))
                {
                    if (File.Exists(newPath))
                    {
                        File.Delete(newPath);
                    }
                    File.Move(FilePath, newPath);
                }
            }
            FilePath = newPath;
            if (Watcher != null)
            {
                Watcher.Filter = fileName;
            }
        }

        public void Remove()
        {
            if (File.Exists(FilePath))
            {
                Enabled = false;
                File.Delete(FilePath);
            }
        }

        public virtual FileStream OpenRead()
        {
            return new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        public virtual FileStream OpenWrite()
        {
            return new FileStream(FilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
        }


    }
}
