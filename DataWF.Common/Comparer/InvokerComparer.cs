using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace DataWF.Common
{
    /// <summary>
    /// Comparer used ReflectionAccessor to get property of item.
    /// </summary>
    public abstract class InvokerComparer : IComparer, IEqualityComparer, INotifyPropertyChanged, INamed
    {
        private ListSortDirection direction;        
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
        public abstract IInvoker Invoker { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public abstract int CompareVal(object x, object key);

        public abstract int Compare(object x, object y);

        public new abstract bool Equals(object x, object y);

        public abstract int GetHashCode(object obj);

        public override bool Equals(object obj)
        {
            return obj is InvokerComparer comp ? Equals(comp) : ReferenceEquals(this, obj);
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
