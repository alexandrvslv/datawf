using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace DataWF.Common
{
    public interface IInvokerExtension
    {
        IListIndex CreateIndex(bool concurrent);
        IListIndex CreateIndex<T>(bool concurrent);
        
        IQueryParameter CreateParameter(Type type);
        IQueryParameter CreateParameter(Type type, CompareType comparer, object value);
        IQueryParameter CreateParameter(Type type, LogicType logic, CompareType comparer, object value = null, QueryGroup group = QueryGroup.None);

        IQueryParameter<TT> CreateParameter<TT>(CompareType comparer, object value);
        IQueryParameter<TT> CreateParameter<TT>(LogicType logic, CompareType comparer, object value = null, QueryGroup group = QueryGroup.None);

        IComparer CreateComparer(Type type, ListSortDirection direction = ListSortDirection.Ascending);
        IComparer<TT> CreateComparer<TT>(ListSortDirection direction = ListSortDirection.Ascending);
    }
}
