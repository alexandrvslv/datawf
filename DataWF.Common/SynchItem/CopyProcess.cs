using DataWF.Common;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

[assembly: Invoker(typeof(CopyProcess), nameof(CopyProcess.Category), typeof(CopyProcess.CategoryInvoker))]
[assembly: Invoker(typeof(CopyProcess), nameof(CopyProcess.Date), typeof(CopyProcess.DateInvoker))]
[assembly: Invoker(typeof(CopyProcess), nameof(CopyProcess.FileName), typeof(CopyProcess.FileNameInvoker))]
[assembly: Invoker(typeof(CopyProcess), nameof(CopyProcess.File), typeof(CopyProcess.FileInvoker))]
[assembly: Invoker(typeof(CopyProcess), nameof(CopyProcess.Progress), typeof(CopyProcess.ProgressInvoker))]
[assembly: Invoker(typeof(CopyProcess), nameof(CopyProcess.FileSize), typeof(CopyProcess.FileSizeInvoker))]
[assembly: Invoker(typeof(CopyProcess), nameof(CopyProcess.Speed), typeof(CopyProcess.SpeedInvoker))]
[assembly: Invoker(typeof(CopyProcess), nameof(CopyProcess.SpeedFormat), typeof(CopyProcess.SpeedFormatInvoker))]
[assembly: Invoker(typeof(CopyProcess), nameof(CopyProcess.Finished), typeof(CopyProcess.FinishedInvoker))]
namespace DataWF.Common
{
    //https://www.codeproject.com/Articles/356297/Copy-a-Stream-with-Progress-Reporting
    public class CopyProcess : DefaultItem
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
        private string lenght;
        private string info;
        private string filePath;
        private int fileCount;
        private string size;

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

        //public int Lenght
        //{
        //    get => lenght;
        //    set
        //    {
        //        lenght = value;
        //        OnPropertyChanged();
        //    }
        //}

        public string Info
        {
            get => info;
            set
            {
                info = value;
                OnPropertyChanged();
            }
        }
        public string FilePath
        {
            get => filePath;
            set
            {
                filePath = value;
                OnPropertyChanged();
            }
        }
        public string Size
        {
            get => size;
            set
            {
                size = value;
                OnPropertyChanged();
            }
        }
        public int FileCount
        {
            get => fileCount;
            set
            {
                fileCount = value;
                OnPropertyChanged();
            }
        }
        public async Task StartAsync(long size, Stream sourceStream, Stream destinationStream, string path = "")
        {
            await Task.Run(() => Start(size, sourceStream, destinationStream, path));
        }

        public void Start(long size, Stream sourceStream, Stream targetStream, string path)
        {
            FileCount = fileCount;
            Prepare(size, sourceStream, targetStream);
            if (!string.IsNullOrEmpty(path))
                FilePath = path;
            try
            {
                DownloadStart?.Invoke(this);
                stopWatch = new Stopwatch();
                listen = new ManualResetEventSlim(false);
                _ = Task.Run(() => Listen());

                int count;
                var buffer = new byte[BufferSize];

                stopWatch.Start();
                Info = "Downloading...";
                double len = 0;
                double total = ((float)FileSize) / 1048576;
                while ((count = sourceStream.Read(buffer, 0, BufferSize)) != 0 && !(Token?.IsCancelled ?? false))
                {
                    targetStream.Write(buffer, 0, count);
                    length += count;
                    len = Math.Round(len += count, 0);
                    Size = $"{Math.Round(len / 1048576, 0)} МБ/{Math.Round(total, 0)} МБ";
                    Progress = (Math.Round(((len / 1048576) * 100) / total)) / 100;
                }
                if (!Info.Equals("Canceled"))
                    Info = "Completed";
                stopWatch.Stop();
            }
            catch (Exception ex)
            {
                Helper.OnException(ex);
                Info = "Error";
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

        public class CategoryInvoker : Invoker<CopyProcess, CopyProcessCategory>
        {
            public static readonly CategoryInvoker Instance = new CategoryInvoker();
            public override string Name => nameof(CopyProcess.Category);

            public override bool CanWrite => false;

            public override CopyProcessCategory GetValue(CopyProcess target) => target.Category;

            public override void SetValue(CopyProcess target, CopyProcessCategory value) { }
        }

        public class DateInvoker : Invoker<CopyProcess, DateTime>
        {
            public static readonly DateInvoker Instance = new DateInvoker();
            public override string Name => nameof(CopyProcess.Date);

            public override bool CanWrite => false;

            public override DateTime GetValue(CopyProcess target) => target.Date;

            public override void SetValue(CopyProcess target, DateTime value) { }
        }

        public class FileNameInvoker : Invoker<CopyProcess, string>
        {
            public static readonly FileNameInvoker Instance = new FileNameInvoker();
            public override string Name => nameof(CopyProcess.FileName);

            public override bool CanWrite => true;

            public override string GetValue(CopyProcess target) => target.FileName;

            public override void SetValue(CopyProcess target, string value) => target.FileName = value;
        }

        public class FileInvoker : Invoker<CopyProcess, IFileModel>
        {
            public static readonly FileInvoker Instance = new FileInvoker();
            public override string Name => nameof(CopyProcess.File);

            public override bool CanWrite => true;

            public override IFileModel GetValue(CopyProcess target) => target.File;

            public override void SetValue(CopyProcess target, IFileModel value) => target.File = value;
        }

        public class ProgressInvoker : Invoker<CopyProcess, double>
        {
            public static readonly ProgressInvoker Instance = new ProgressInvoker();
            public override string Name => nameof(CopyProcess.Progress);

            public override bool CanWrite => true;

            public override double GetValue(CopyProcess target) => target.Progress;

            public override void SetValue(CopyProcess target, double value) => target.Progress = value;
        }

        public class FileSizeInvoker : Invoker<CopyProcess, long>
        {
            public static readonly FileSizeInvoker Instance = new FileSizeInvoker();
            public override string Name => nameof(CopyProcess.FileSize);

            public override bool CanWrite => true;

            public override long GetValue(CopyProcess target) => target.FileSize;

            public override void SetValue(CopyProcess target, long value) { }
        }

        public class SpeedInvoker : Invoker<CopyProcess, double>
        {
            public static readonly SpeedInvoker Instance = new SpeedInvoker();
            public override string Name => nameof(CopyProcess.Speed);

            public override bool CanWrite => true;

            public override double GetValue(CopyProcess target) => target.Speed;

            public override void SetValue(CopyProcess target, double value) { }
        }

        public class SpeedFormatInvoker : Invoker<CopyProcess, string>
        {
            public static readonly SpeedFormatInvoker Instance = new SpeedFormatInvoker();
            public override string Name => nameof(CopyProcess.SpeedFormat);

            public override bool CanWrite => true;

            public override string GetValue(CopyProcess target) => target.SpeedFormat;

            public override void SetValue(CopyProcess target, string value) { }
        }

        public class FinishedInvoker : Invoker<CopyProcess, bool>
        {
            public static readonly FinishedInvoker Instance = new FinishedInvoker();
            public override string Name => nameof(CopyProcess.Finished);

            public override bool CanWrite => true;

            public override bool GetValue(CopyProcess target) => target.Finished;

            public override void SetValue(CopyProcess target, bool value) { }
        }

    }
}