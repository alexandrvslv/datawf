using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.ComponentModel;
using System.Collections;

namespace DataWF.Common
{
    /// <summary>
    /// Comparer used ReflectionAccessor to get property of item.
    /// </summary>
    public class InvokerComparer : IComparer
    {
        public IInvoker invoker;
        public ListSortDirection _direction;
        public IComparer _comparer;
        //bool hash = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="Dwf.Tool.InvokerComparer"/> class.
        /// </summary>
        /// <param name='comparer'>
        /// Comparer.
        /// </param>
        public InvokerComparer(IComparer comparer)
        {
            _comparer = comparer;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Dwf.Tool.InvokerComparer"/> class.
        /// </summary>
        /// <param name='type'>
        /// type.
        /// </param>
        /// <param name='property'>
        /// property name.
        /// </param>
        /// <param name='direction'>
        /// direction.
        /// </param>
        public InvokerComparer(Type type, string property, ListSortDirection direction = ListSortDirection.Ascending)
            : this(EmitInvoker.Initialize(type, property), direction)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Dwf.Tool.InvokerComparer"/> class.
        /// </summary>
        /// <param name='info'>
        /// property Info.
        /// </param>
        /// <param name='direction'>
        /// direction.
        /// </param>
        public InvokerComparer(PropertyInfo info, ListSortDirection direction = ListSortDirection.Ascending)
            : this(info, null, direction)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Dwf.Tool.InvokerComparer"/> class.
        /// </summary>
        /// <param name='info'>
        /// Property Info.
        /// </param>
        /// <param name='index'>
        /// Index.
        /// </param>
        /// <param name='direction'>
        /// Direction.
        /// </param>
        public InvokerComparer(PropertyInfo info, object index, ListSortDirection direction = ListSortDirection.Ascending)
            : this(EmitInvoker.Initialize(info, index), direction)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Dwf.Tool.InvokerComparer"/> class.
        /// </summary>
        /// <param name='accesor'>
        /// Accesor.
        /// </param>
        /// <param name='direction'>
        /// Direction.
        /// </param>
        public InvokerComparer(IInvoker accesor, ListSortDirection direction)
        {
            invoker = accesor;
            _direction = direction;
        }

        //public bool Hash
        //{
        //    get { return hash; }
        //    set { hash = value; }
        //}
        #region IComparer

        /// <summary>
        /// Compare the specified xWord and yWord by class Accesor.
        /// </summary>
        /// <param name='x'>
        /// X word.
        /// </param>
        /// <param name='y'>
        /// Y word.
        /// </param>
        public int Compare(object x, object y)
        {
            if (_comparer != null)
                return _comparer.Compare(x, y);
            object xValue = x == null ? null : invoker.GetValue(x);
            object yValue = y == null ? null : invoker.GetValue(y);
            int rez = ListHelper.Compare(xValue, yValue, null, _direction, false);
            //if (hash && rez == 0 && xWord != null && yWord != null)
            //    rez = xWord.GetHashCode().CompareTo(yWord.GetHashCode());
            return rez;
        }

        public int CompareVal(object x, object key)
        {
            object val = invoker.GetValue(x);
            return ListHelper.Compare(val, key, _comparer, false);
        }

        /// <summary>
        /// Equals the specified xWord and yWord.
        /// </summary>
        /// <param name='xWord'>
        /// If set to <c>true</c> x word.
        /// </param>
        /// <param name='yWord'>
        /// If set to <c>true</c> y word.
        /// </param>
        public new bool Equals(object xWord, object yWord)
        {
            return xWord.Equals(yWord);
        }

        /// <summary>
        /// Gets the hash code.
        /// </summary>
        /// <returns>
        /// The hash code.
        /// </returns>
        /// <param name='obj'>
        /// Object.
        /// </param>
        public int GetHashCode(object obj)
        {
            return obj.GetHashCode();
        }

        #endregion

        public override bool Equals(object obj)
        {
            if (obj is InvokerComparer)
            {
                bool bycom = true;
                if (_comparer != null)
                    bycom = _comparer.Equals(((InvokerComparer)obj)._comparer);
                bool byasc = _direction == ((InvokerComparer)obj)._direction;
                bool byacc = invoker == ((InvokerComparer)obj).invoker;
                if (invoker != null)
                    byacc = invoker.Equals(((InvokerComparer)obj).invoker);
                return bycom && byasc && byacc;
            }
            return object.ReferenceEquals(this, obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    /// <summary>
    /// Comparer access generic.
    /// </summary>
    public class InvokerComparer<T> : InvokerComparer, IComparer<T>
    {
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

        public InvokerComparer(IComparer comparer)
            : base(comparer)
        {
        }

        #region IComparer<T>
        public int CompareVal(T x, object key)
        {
            object val = invoker.GetValue(x);
            return ListHelper.Compare(val, key, _comparer, false);
        }

        public int Compare(T x, T y)
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

        #endregion
    }
}
