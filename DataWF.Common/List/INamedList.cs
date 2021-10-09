using System.Collections;
using System.Collections.Generic;

namespace DataWF.Common
{
    public interface INamedList : IList
    {
        INamed Get(string name);
        void Set(INamed value);
        void Set(INamed value, int index);
    }

    public interface INamedOutList<out T> : INamedList, IReadOnlyList<T> where T : INamed
    {
        new T Get(string name);
    }

    public interface INamedInList<in T> : INamedList  where T : INamed
    {
        void Set(T value);
        void Set(T value, int index);
    }

    public interface INamedList<T> : INamedOutList<T>, INamedInList<T>, IList<T> where T : INamed
    {
        
    }
}

