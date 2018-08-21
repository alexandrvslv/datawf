using System.Collections;
using System.Collections.Generic;

namespace DataWF.Common
{
    public interface IQuery
    {
        IEnumerable<IQueryParameter> Parameters { get; }

        IQueryParameter Add(LogicType logic, string property, CompareType comparer, object value);

        IQueryParameter AddTreeParameter();

        void Clear();

        void Sort(IList list);
    }
}