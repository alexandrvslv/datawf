using System;
using System.Collections;
using System.Collections.Generic;

namespace DataWF.Common
{
    public interface IReferenceList : IChangeableList
    {
        SynchronizedItem Owner { get; set; }
        string OwnerProperty { get; set; }
    }

    public interface IChangeableList : IList
    {
        IEnumerable GetChanged();
        void CheckStatus(object item);

        event EventHandler<NotifyStatusChangedEventArgs> ItemStatusChanged;
    }

    public interface IChangeableList<T> : IChangeableList
    {
        new IEnumerable<T> GetChanged();
        void CheckStatus(T item);
    }

    public class NotifyStatusChangedEventArgs : EventArgs
    {
        public NotifyStatusChangedEventArgs(ISynchronized item)
        {
            Item = item;
        }

        public ISynchronized Item { get; }
    }
}