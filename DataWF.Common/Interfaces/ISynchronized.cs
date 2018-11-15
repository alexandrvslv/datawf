using System.Collections.Generic;

namespace DataWF.Common
{
    public interface ISynchronized
    {
        bool? IsSynchronized { get; set; }
        ISet<string> Changes { get; }
    }
}

