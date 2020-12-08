using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace DataWF.Common
{
    public interface IQuery
    {
        ICollection<IQueryParameter> Parameters { get; }

        ICollection<IComparer> Orders { get; }

        ITreeComparer TreeComparer { get; set; }

        bool Suspending { get; set; }

        event EventHandler ParametersChanged;

        IQueryParameter GetParameter(string name);

        IQueryParameter Add(string property, object value);

        IQueryParameter Add(LogicType logic, IInvoker invoker, CompareType comparer, object value);

        IQueryParameter AddOrUpdate(LogicType logic, IInvoker invoker, CompareType comparer, object value);

        InvokerComparer AddOrder(IInvoker invoker, ListSortDirection sortDirection);

        void Clear();

        void ClearValues();

        void Sort(IList list);

        IEnumerable Select(IEnumerable items, IListIndexes indexes = null);

        bool CheckItem(object item);
    }
}