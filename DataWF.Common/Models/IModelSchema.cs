using System;
using System.Collections.Generic;

namespace DataWF.Common
{
    public interface IModelSchema: INamed
    {
        IModelProvider Provider { get; }
        IEnumerable<IModelTable> Tables { get; }

        IModelTable<T> GetTable<T>();
        IModelTable GetTable(Type type);
        IModelTable GetTable(Type type, int typeId);
    }
}