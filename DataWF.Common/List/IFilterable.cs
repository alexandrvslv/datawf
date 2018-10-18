using System.Collections;
using System.Collections.Generic;

namespace DataWF.Common
{
    public interface IFilterable : IList
    {
        IEnumerable Source { get; set; }
        IQuery FilterQuery { get; }
        void UpdateFilter();
    }

    public interface IFilterable<T> : IFilterable, IList<T>
    {
        new Query<T> FilterQuery { get; }
    }
}
