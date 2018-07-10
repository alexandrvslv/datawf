using System;
using System.Collections.Specialized;
using System.ComponentModel;

namespace DataWF.Common
{
    [System.Runtime.InteropServices.ComVisible(false)]
    public class NotifyListPropertyChangedEventArgs : NotifyCollectionChangedEventArgs
    {
        public static NotifyListPropertyChangedEventArgs Build(NotifyCollectionChangedAction type, object item, object oldItem, int index, int oldIndex, string property)
        {
            switch (type)
            {
                case NotifyCollectionChangedAction.Reset:
                    if (item != null)
                    {
                        return new NotifyListPropertyChangedEventArgs(item, property);
                    }
                    else
                    {
                        return new NotifyListPropertyChangedEventArgs();
                    }
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Remove:
                    return new NotifyListPropertyChangedEventArgs(type, item, index);
                case NotifyCollectionChangedAction.Replace:
                    return new NotifyListPropertyChangedEventArgs(item, oldItem, index);
                default:
                    return new NotifyListPropertyChangedEventArgs(item, index, oldIndex);
            }
        }

        public NotifyListPropertyChangedEventArgs()
            : base(NotifyCollectionChangedAction.Reset)
        { }

        public NotifyListPropertyChangedEventArgs(object item, string property)
            : base(NotifyCollectionChangedAction.Reset)
        {
            Item = item;
            Property = property;
        }

        public NotifyListPropertyChangedEventArgs(object item, object oldItem, int index)
          : base(NotifyCollectionChangedAction.Replace, item, oldItem, index)
        {
            Item = item;
        }

        public NotifyListPropertyChangedEventArgs(object item, int index, int oldIndex, string property)
          : base(NotifyCollectionChangedAction.Move, item, index, oldIndex)
        {
            Item = item;
            Property = property;
        }

        public NotifyListPropertyChangedEventArgs(NotifyCollectionChangedAction type, object item, int index)
            : base(type, item, index)
        {
            Item = item;
        }

        public object Item { get; set; }

        public string Property { get; set; }


    }
}
