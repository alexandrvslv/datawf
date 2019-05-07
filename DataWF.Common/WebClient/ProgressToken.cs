using System.Threading;

namespace DataWF.Common
{
    public class ProgressToken
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
                CancellationTokenSource = new CancellationTokenSource();
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
                        process.CancellationToken = CancellationToken;
                        process.Progressable = Progressable;
                    }
                }
            }
        }

        public IProgressable Progressable { get; }

        public void Cancel()
        {
            if (CancellationTokenSource != null)
            {
                CancellationTokenSource.Cancel();
            }
        }
    }

}
