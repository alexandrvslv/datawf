using System.Collections.Generic;

namespace DataWF.Common
{
    public interface IIdCollection<T> : ICollection<T>
    {
        T GetById(object id);
    }
}

