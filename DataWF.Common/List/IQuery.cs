using System.Collections;
using System.Collections.Generic;

namespace DataWF.Common
{
    public interface IQuery
    {
        IEnumerable<IQueryParameter> Parameters { get; }

        IQueryParameter Add(LogicType logic, IInvoker invoker, CompareType comparer, object value);

        IQueryParameter AddTreeParameter();

        void Add(IQueryParameter parameter);

        bool Remove(IQueryParameter parameter);

        void Clear();

        void ClearValues();

        void Sort(IList list);
    }
}