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
    public class InvokerComparer : IComparer, IEqualityComparer, INotifyPropertyChanged, INamed
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

        [Newtonsoft.Json.JsonIgnore, System.Text.Json.Serialization.JsonIgnore, XmlIgnore]
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

        public virtual int CompareVal(object x, object key)
        {
            var val = Invoker.GetValue(x);
            return ListHelper.Compare(val, key, null);
        }

        public virtual int Compare(object x, object y)
        {
            var xValue = x == null ? null : Invoker.GetValue(x);
            var yValue = y == null ? null : Invoker.GetValue(y);
            var rez = ListHelper.Compare(xValue, yValue, null);
            //if (hash && rez == 0 && x != null && y != null)
            //    rez = x.GetHashCode().CompareTo(y.GetHashCode());
            return Direction == ListSortDirection.Ascending ? rez : -rez;
        }

        public virtual bool EqualsObjects(object x, object y)
        {
            var xValue = x == null ? null : Invoker.GetValue(x);
            var yValue = y == null ? null : Invoker.GetValue(y);
            var rez = ListHelper.Equal(xValue, yValue);
            //if (hash && rez == 0 && x != null && y != null)
            //    rez = x.GetHashCode().Equals(y.GetHashCode());
            return rez;
        }

        bool IEqualityComparer.Equals(object x, object y)
        {
            return EqualsObjects(x, y);
        }

        public int GetHashCode(object obj)
        {
            var objValue = obj == null ? null : Invoker.GetValue(obj);
            return objValue?.GetHashCode() ?? obj.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is InvokerComparer comp)
            {
                return Equals(comp);
            }
            return object.ReferenceEquals(this, obj);
        }

        private bool Equals(InvokerComparer comp)
        {
            bool byasc = Direction == comp.Direction;
            bool byacc = Invoker == comp.Invoker;
            return byasc && byacc;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public class InvokerComparer<T> : InvokerComparer, IComparer<T>, IEqualityComparer<T>
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

        public virtual int CompareVal(T x, object key)
        {
            return base.CompareVal(x, key);
        }

        public virtual int Compare(T x, T y)
        {
            return base.Compare(x, y);
        }

        public virtual bool Equals(T x, T y)
        {
            return ((IEqualityComparer)this).Equals(x, y);
        }

        public virtual int GetHashCode(T obj)
        {
            return base.GetHashCode(obj);
        }
    }


    public class InvokerComparer<T, V> : InvokerComparer<T>
    {
        public InvokerComparer()
        { }

        public InvokerComparer(PropertyInfo info, ListSortDirection direction = ListSortDirection.Ascending)
            : base(info, direction)
        {
        }

        public InvokerComparer(IInvoker<T, V> accessor, ListSortDirection direction = ListSortDirection.Ascending)
            : base(accessor, direction)
        {
        }

        public InvokerComparer(string property, ListSortDirection direction = ListSortDirection.Ascending)
            : base(property, direction)
        {
        }

        public IInvoker<T, V> ValueInvoker
        {
            get => (IInvoker<T, V>)Invoker;
            set => Invoker = value;
        }

        public override int CompareVal(object x, object key)
        {
            return CompareVal((T)x, (V)key);
        }

        public override int CompareVal(T x, object key)
        {
            return CompareVal(x, (V)key);
        }

        public int CompareVal(T x, V key)
        {
            var val = ValueInvoker.GetValue(x);
            var result = ListHelper.CompareT(val, key, null);
            return Direction == ListSortDirection.Ascending ? result : -result;
        }

        //TODO nullable null compare
        //public override int Compare(object x, object y)
        //{
        //    return Compare((T)x, (T)y);
        //}

        //public override int Compare(T x, T y)
        //{
        //    var xValue = x == null ? default(V) : ValueInvoker.GetValue(x);
        //    var yValue = y == null ? default(V) : ValueInvoker.GetValue(y);
        //    var result = ListHelper.CompareT(xValue, yValue, null);
        //    return Direction == ListSortDirection.Ascending ? result : -result;
        //}

        //public override bool EqualsObjects(object x, object y)
        //{
        //    return Equals((T)x, (T)y);
        //}

        //public override bool Equals(T x, T y)
        //{
        //    var xValue = x == null ? default(V) : ValueInvoker.GetValue(x);
        //    var yValue = y == null ? default(V) : ValueInvoker.GetValue(y);
        //    return ListHelper.EqualT(xValue, yValue);
        //}

        //public override int GetHashCode(T obj)
        //{
        //    var objValue = obj == null ? default(V) : ValueInvoker.GetValue(obj);
        //    return objValue?.GetHashCode() ?? obj.GetHashCode();
        //}
    }
}
