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

        bool Suspending { get; set; }

        event EventHandler ParametersChanged;

        IQueryParameter Add(string property, object value);

        IQueryParameter Add(LogicType logic, IInvoker invoker, CompareType comparer, object value);

        IQueryParameter AddOrUpdate(LogicType logic, IInvoker invoker, CompareType comparer, object value);

        InvokerComparer AddOrder(IInvoker invoker, ListSortDirection sortDirection);

        IQueryParameter AddTreeParameter();

        void Add(IQueryParameter parameter);

        bool Remove(IQueryParameter parameter);

        void Clear();

        void ClearValues();

        void Sort(IList list);
    }
}