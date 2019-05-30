using System;
using System.Threading;

namespace DataWF.Common
{
    public class ProgressToken : IDisposable
    {
        public static readonly ProgressToken None = new ProgressToken(null);

        public CancellationToken CancellationToken = CancellationToken.None;
        public CancellationTokenSource CancellationTokenSource = null;

        private CopyProcess process;

        public ProgressToken(IProgressable prograssable)
        {
            Progressable = prograssable;
            if (Progressable != null)
            {
                Progressable.Progress = 0;
                CancellationTokenSource = new CancellationTokenSource
                {

                };
                CancellationToken = CancellationTokenSource.Token;
            }
        }

        public CopyProcess Process
        {
            get => process;
            set
            {
                if (process != value)
                {
                    if (process != null && Progressable != null)
                    {
                    }
                    process = value;
                    if (process != null)
                    {
                        process.Token = this;
                        process.Progressable = Progressable;
                        process.File = File;
                    }
                }
            }
        }

        public bool IsCancelled => CancellationTokenSource?.IsCancellationRequested ?? false;

        public IProgressable Progressable { get; }

        public IFileModel File { get; set; }

        public void Cancel()
        {
            if (CancellationTokenSource != null)
            {
                CancellationTokenSource.Cancel();
            }
        }

        public void Dispose()
        {
            if (CancellationTokenSource != null)
            {
                CancellationTokenSource.Dispose();
                CancellationTokenSource = null;
            }
        }
    }

}
