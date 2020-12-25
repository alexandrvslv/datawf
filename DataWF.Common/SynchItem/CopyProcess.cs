using DataWF.Common;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DataWF.Common
{
    //https://www.codeproject.com/Articles/356297/Copy-a-Stream-with-Progress-Reporting
    [InvokerGenerator]
    public partial class CopyProcess : DefaultItem
    {
        public static Action<CopyProcess> DownloadStart;
        public static Action<CopyProcess> DownloadFinish;

        private double percent;
        private long length;
        private double progress;
        private long fileSize;
        private ManualResetEventSlim listen;

        public CopyProcess(CopyProcessCategory category, int bufferSize = 81920)
        {
            Date = DateTime.Now;
            Category = category;
            BufferSize = bufferSize;
        }

        public ProgressToken Token = null;

        private double speed;
        private Stopwatch stopWatch;

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
            get => Helper.LenghtFormat(speed) + "/s";
        }

        public bool Finished => length == fileSize;

        public IProgressable Progressable { get; internal set; }

        public IFileModel File { get; internal set; }

        public async Task StartAsync(long size, Stream sourceStream, Stream destinationStream)
        {
            await Task.Run(() => Start(size, sourceStream, destinationStream));
        }

        public void Start(long size, Stream sourceStream, Stream targetStream)
        {
            Prepare(size, sourceStream, targetStream);

            try
            {
                DownloadStart?.Invoke(this);
                stopWatch = new Stopwatch();
                listen = new ManualResetEventSlim(false);
                _ = Task.Run(() => Listen());

                int count;
                var buffer = new byte[BufferSize];

                stopWatch.Start();
                while ((count = sourceStream.Read(buffer, 0, BufferSize)) != 0 && !(Token?.IsCancelled ?? false))
                {
                    targetStream.Write(buffer, 0, count);
                    length += count;
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

        private void Listen()
        {
            Progress = 0;

            while (!listen.Wait(50))
            {
                long currentLength = length;
                speed = currentLength / stopWatch.Elapsed.TotalSeconds;
                Progress = (currentLength / percent) / 100;
                if (Progress == 1)
                    break;
            }
            speed = length / stopWatch.Elapsed.TotalSeconds;

            listen.Dispose();
            listen = null;
            Progress = (length / percent) / 100;
        }       

    }
}