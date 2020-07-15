using System;
using System.ComponentModel;

namespace DataWF.Common
{
    public interface IInvokerExtension
    {
        IListIndex CreateIndex(bool concurrent);

        IQueryParameter CreateParameter(Type type);
        QueryParameter<TT> CreateParameter<TT>();

        InvokerComparer CreateComparer(Type type, ListSortDirection direction = ListSortDirection.Ascending);
        InvokerComparer<TT> CreateComparer<TT>(ListSortDirection direction = ListSortDirection.Ascending);
    }
}
