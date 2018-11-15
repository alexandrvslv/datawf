using System.Collections;

namespace DataWF.Common
{
    public interface INamedList : IList
    {
        INamed Get(string name);
        void Set(INamed value);
        void Set(INamed value, int index);
    }
}

