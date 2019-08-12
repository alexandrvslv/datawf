using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

namespace DataWF.Common
{
    /// <summary>
    /// Comparer used ReflectionAccessor to get property of item.
    /// </summary>
    public class InvokerComparer : IComparer, INotifyPropertyChanged, INamed
    {
        private ListSortDirection direction;
        private IInvoker invoker;
        private string name;

        //bool hash = true;
        public InvokerComparer()
        { }

        public InvokerComparer(Type type, string property, ListSortDirection direction = ListSortDirection.Ascending)
            : this(EmitInvoker.Initialize(type, property), direction)
        {

        }

        public InvokerComparer(PropertyInfo info, ListSortDirection direction = ListSortDirection.Ascending)
            : this(info, null, direction)
        {
        }

        public InvokerComparer(PropertyInfo info, object index, ListSortDirection direction = ListSortDirection.Ascending)
            : this(index == null ? EmitInvoker.Initialize(info, true) : EmitInvoker.Initialize(info, index), direction)
        {
        }

        public InvokerComparer(IInvoker accesor, ListSortDirection direction)
        {
            Invoker = accesor;
            Direction = direction;
        }

        public ListSortDirection Direction
        {
            get => direction;
            set
            {
                if (direction != value)
                {
                    direction = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Name
        {
            get => name;
            set
            {
                if (name != value)
                {
                    name = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonIgnore, XmlIgnore]
        public virtual IInvoker Invoker
        {
            get => invoker;
            set
            {
                invoker = value;
                Name = invoker?.Name;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public virtual int Compare(object x, object y)
        {
            object xValue = x == null ? null : Invoker.GetValue(x);
            object yValue = y == null ? null : Invoker.GetValue(y);
            int rez = ListHelper.Compare(xValue, yValue, null, Direction, false);
            //if (hash && rez == 0 && xWord != null && yWord != null)
            //    rez = xWord.GetHashCode().CompareTo(yWord.GetHashCode());
            return rez;
        }

        public new bool Equals(object xWord, object yWord)
        {
            return xWord.Equals(yWord);
        }

        public int GetHashCode(object obj)
        {
            return obj.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is InvokerComparer)
            {
                bool byasc = Direction == ((InvokerComparer)obj).Direction;
                bool byacc = Invoker == ((InvokerComparer)obj).Invoker;
                if (Invoker != null)
                    byacc = Invoker.Equals(((InvokerComparer)obj).Invoker);
                return byasc && byacc;
            }
            return object.ReferenceEquals(this, obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public class InvokerComparer<T> : InvokerComparer, IComparer<T>
    {
        public InvokerComparer()
        { }

        public InvokerComparer(PropertyInfo info, ListSortDirection direction = ListSortDirection.Ascending)
            : base(info, null, direction)
        {
        }

        public InvokerComparer(IInvoker accessor, ListSortDirection direction = ListSortDirection.Ascending)
            : base(accessor, direction)
        {
        }

        public InvokerComparer(string property, ListSortDirection direction = ListSortDirection.Ascending)
            : base(typeof(T), property, direction)
        {
        }

        public override IInvoker Invoker
        {
            get => base.Invoker ?? (base.Invoker = EmitInvoker.Initialize<T>(Name));
            set => base.Invoker = value;
        }

        public int CompareVal(T x, object key)
        {
            object val = Invoker.GetValue(x);
            return ListHelper.Compare(val, key, null, false);
        }

        public virtual int Compare(T x, T y)
        {
            return base.Compare(x, y);
        }

        public bool Equals(T x, T y)
        {
            return x.Equals(y);
        }

        public int GetHashCode(T obj)
        {
            return obj.GetHashCode();
        }
    }
}
