using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
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
            return ListHelper.Compare(val, key, (IComparer)null);
        }

        public virtual int Compare(object x, object y)
        {
            var xValue = x == null ? null : Invoker.GetValue(x);
            var yValue = y == null ? null : Invoker.GetValue(y);
            var rez = ListHelper.Compare(xValue, yValue, (IComparer)null);
            //if (hash && rez == 0 && x != null && y != null)
            //    rez = x.GetHashCode().CompareTo(y.GetHashCode());
            return Direction == ListSortDirection.Ascending ? rez : -rez;
        }

        public new virtual bool Equals(object x, object y)
        {
            var xValue = x == null ? null : Invoker.GetValue(x);
            var yValue = y == null ? null : Invoker.GetValue(y);
            var rez = ListHelper.Equal(xValue, yValue);
            //if (hash && rez == 0 && x != null && y != null)
            //    rez = x.GetHashCode().Equals(y.GetHashCode());
            return rez;
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
}
