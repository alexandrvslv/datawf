using System;
using System.Collections.Generic;

namespace DataWF.Common
{
    public interface IModelSchema: INamed
    {
        IModelProvider Provider { get; set; }
        IEnumerable<IModelTable> Tables { get; }

        IModelTable<T> GetTable<T>();
        IModelTable GetTable(string name);
        IModelTable GetTable(Type type);
        IModelTable GetTable(Type type, int typeId);
    }
}