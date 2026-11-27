using System;

namespace DataWF.ModelView;

public class ViewModelEventArgs : EventArgs
{
    public ViewModelEventArgs(object item)
    {
        Item = item;
    }

    public object Item { get; }
}
