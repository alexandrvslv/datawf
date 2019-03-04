using System.Collections.Generic;

namespace DataWF.Common
{
    public interface ISynchronized
    {
        SynchronizedStatus SyncStatus { get; set; }
        ISet<string> Changes { get; }
    }
}

