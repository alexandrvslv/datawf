using System;

namespace DataWF.Common
{
    public class ModelViewEventArgs : EventArgs
    {
        public ModelViewEventArgs(object item)
        {
            Item = item;
        }

        public object Item { get; }
    }
}