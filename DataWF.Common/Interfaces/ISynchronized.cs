using System.Collections.Generic;
using System.Reflection;

namespace DataWF.Common
{
    public interface ISynchronized
    {
        SynchronizedStatus SyncStatus { get; set; }
        IDictionary<string, object> Changes { get; }
        bool PropertyChangeOverride(string propertyName);
    }
}

