using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DataWF.Common
{
    //https://www.codeproject.com/Articles/356297/Copy-a-Stream-with-Progress-Reporting
    public class DownloadProcess : DefaultItem
    {
        public static Action<DownloadProcess> DownloadStart;
        public static Action<DownloadProcess> DownloadFinish;

        private double percent;
        private long length;
        private int running;
        private double progress;


        public DownloadProcess(string fileName, int bufferSize, int fileSize)
        {
            BufferSize = bufferSize;
            FileName = fileName;
            FileSize = fileSize;
            percent = FileSize / 100D;
        }


        public int BufferSize { get; }

        public int FileSize { get; }

        public string FileName { get; }

        public double Progress
        {
            get => progress;
            set
            {
                OnPropertyChanging();
                progress = value;
                OnPropertyChanged();
            }
        }

        public bool Finished => running == 0;

        public Action<long, double> ProgressAction { get; set; }

        public CancellationToken CancellationToken { get; set; }

        public async Task StartAsync(Stream sourceStream, Stream destinationStream, CancellationToken cancellationToken)
        {
            await Task.Run(() => Start(sourceStream, destinationStream, cancellationToken));
        }

        public void Start(Stream sourceStream, Stream targetStream, CancellationToken cancellationToken)
        {
            CancellationToken = cancellationToken;
            DownloadStart?.Invoke(this);
            Interlocked.Exchange(ref running, 1);

            int count;
            var buffer = new byte[BufferSize];

            while ((count = sourceStream.Read(buffer, 0, BufferSize)) != 0 && !cancellationToken.IsCancellationRequested)
            {
                targetStream.Write(buffer, 0, count);
                Interlocked.Exchange(ref length, length + count);
            }
            Interlocked.Exchange(ref running, 0);
            DownloadFinish?.Invoke(this);
        }

        public async Task ListenAsync()
        {
            await Task.Run(() => Listen());
        }

        private void Listen()
        {
            while (running == 1)
            {
                Thread.Sleep(100);
                long currentLength = Interlocked.Read(ref length);
                Progress = (currentLength / percent) / 100;
                ProgressAction?.Invoke(currentLength, Progress);
            }
            Progress = 1;
            OnPropertyChanged(nameof(Finished));
        }


    }
}