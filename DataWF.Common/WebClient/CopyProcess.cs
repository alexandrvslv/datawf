using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DataWF.Common
{
    public enum CopyProcessCategory
    {
        None,
        Download,
        Upload,
        Buffering,
    }

    //https://www.codeproject.com/Articles/356297/Copy-a-Stream-with-Progress-Reporting
    public class CopyProcess : DefaultItem
    {
        public static Action<CopyProcess> DownloadStart;
        public static Action<CopyProcess> DownloadFinish;

        private double percent;
        private long length;
        private double progress;
        private long fileSize;
        private ManualResetEvent listen;

        public CopyProcess(CopyProcessCategory category, int bufferSize = 81920)
        {
            Date = DateTime.Now;
            Category = category;
            BufferSize = bufferSize;
        }
        public CancellationToken CancellationToken = CancellationToken.None;
        private double speed;

        public DateTime Date { get; }

        public CopyProcessCategory Category { get; }

        public int BufferSize { get; }

        public long FileSize
        {
            get => fileSize;
            set
            {
                if (fileSize != value)
                {
                    fileSize = value;
                    percent = fileSize / 100D;
                    OnPropertyChanged();
                }
            }
        }

        public string FileName { get; set; }

        public double Progress
        {
            get => progress;
            set
            {
                if (progress != value)
                {
                    progress = value;
                    if (Progressable != null)
                    {
                        Progressable.Progress = value;
                    }
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Speed));
                }
            }
        }

        public double Speed
        {
            get => speed;
            private set
            {
                speed = value;
            }
        }

        public string SpeedFormat
        {
            get => Helper.LenghtFormat(speed);
        }

        public bool Finished => length == fileSize;

        public IProgressable Progressable { get; internal set; }

        public async Task StartAsync(long size, Stream sourceStream, Stream destinationStream)
        {
            await Task.Run(() => Start(size, sourceStream, destinationStream));
        }

        public void Start(long size, Stream sourceStream, Stream targetStream)
        {
            Prepare(size, sourceStream, targetStream);            

            try
            {
                listen = new ManualResetEvent(false);
                _ = ListenAsync();
                DownloadStart?.Invoke(this);
                int count;
                var buffer = new byte[BufferSize];
                var stopWatch = new Stopwatch();
                stopWatch.Start();
                while ((count = sourceStream.Read(buffer, 0, BufferSize)) != 0 && !CancellationToken.IsCancellationRequested)
                {
                    targetStream.Write(buffer, 0, count);
                    Interlocked.Exchange(ref length, length + count);
                    Interlocked.Exchange(ref speed, length / (stopWatch.ElapsedMilliseconds / 1000D));
                }
                stopWatch.Stop();
            }
            catch (Exception ex)
            {
                Helper.OnException(ex);
                throw ex;
            }
            finally
            {
                listen?.Set();

                if (sourceStream.CanSeek)
                {
                    sourceStream.Position = 0;
                }
                if (targetStream.CanSeek)
                {
                    targetStream.Position = 0;
                }
            }
            OnPropertyChanged(nameof(Finished));
            DownloadFinish?.Invoke(this);
        }

        private void Prepare(long size, Stream sourceStream, Stream targetStream)
        {
            if (targetStream is FileStream targetDileStream)
            {
                FileName = Path.GetFileName(targetDileStream.Name);
            }
            else if (sourceStream is FileStream sourceFileStream)
            {
                FileName = Path.GetFileName(sourceFileStream.Name);
            }
            if (sourceStream.CanSeek)
            {
                sourceStream.Position = 0;
            }
            if (targetStream.CanSeek)
            {
                targetStream.Position = 0;
            }
            FileSize = size;

        }

        private async Task ListenAsync()
        {
            await Task.Run(() => Listen());
        }

        private void Listen()
        {
            Progress = 0;
            
            while (!listen.WaitOne(50))
            {
                long currentLength = Interlocked.Read(ref length);
                Progress = (currentLength / percent) / 100;
                if (Progress == 1)
                    break;
            }
            listen.Dispose();
            listen = null;
            Progress = (length / percent) / 100;
        }
    }
}