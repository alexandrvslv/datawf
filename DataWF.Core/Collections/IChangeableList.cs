using System;
using System.Collections;
using System.Collections.Generic;

namespace DataWF.Collections;

public interface IChangeableList : IList
{
    IEnumerable GetChanged();
    void CheckStatus(object item);
    void SetStatus(object item, SynchronizedStatus status);

    event EventHandler<NotifyStatusChangedEventArgs> ItemStatusChanged;
}

public interface IChangeableList<T> : IChangeableList
{
    new ICollection<T> GetChanged();
    void CheckStatus(T item);
    void SetStatus(T item, SynchronizedStatus status);

    void IChangeableList.CheckStatus(object item) => CheckStatus((T)item);
    void IChangeableList.SetStatus(object item, SynchronizedStatus status) => SetStatus((T)item, status);
}


