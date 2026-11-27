using System;

namespace DataWF.Collections;

public class NotifyStatusChangedEventArgs : EventArgs
{
    public NotifyStatusChangedEventArgs(ISynchronized item)
    {
        Item = item;
    }

    public ISynchronized Item { get; }
}