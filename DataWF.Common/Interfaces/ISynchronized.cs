using System.Collections.Generic;

namespace DataWF.Common
{
    public interface ISynchronized
    {
        SynchronizedStatus SyncStatus { get; set; }
        IDictionary<string, object> Changes { get; }
    }
}

