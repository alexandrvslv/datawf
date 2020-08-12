using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace DataWF.Common
{
    public interface IInvokerExtension
    {
        IListIndex CreateIndex(bool concurrent);

        IQueryParameter CreateParameter(Type type);
        QueryParameter<TT> CreateParameter<TT>();

        IComparer CreateComparer(Type type, ListSortDirection direction = ListSortDirection.Ascending);
        IComparer<TT> CreateComparer<TT>(ListSortDirection direction = ListSortDirection.Ascending);
    }
}
