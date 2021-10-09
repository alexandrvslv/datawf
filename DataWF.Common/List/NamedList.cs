
using System;
using System.Collections.Generic;

namespace DataWF.Common
{
    public class NamedList<T> : SelectableList<T>, INamedList<T> where T : INamed
    {
        private ListIndex<T, string> nameIndex;

        public NamedList(int capacity, ListIndex<T, string> nameIndex) : base(capacity)
        {
            this.nameIndex = nameIndex;
            Indexes.Add(nameof(INamed.Name), nameIndex);
        }

        public NamedList(int capacity) : this(capacity, new ListIndex<T, string>(
                NamedNameInvoker<T>.Instance,
                ListIndexFactory.GetNullKey<string>(),
                StringComparer.Ordinal,
                false))
        {
        }

        public NamedList() : this(4)
        {
        }

        public NamedList(IEnumerable<T> items) : this()
        {
            AddRangeInternal(items, false);
        }

        public virtual T this[string param]
        {
            get { return nameIndex.SelectOne(param); }
            set
            {
                var exists = this[param];
                if (!value?.Equals(exists) ?? false)
                {
                    if (exists != null)
                    {
                        Remove(exists);
                    }
                    Add(value);
                }
            }
        }

        public virtual bool Contains(string name)
        {
            return this[name] != null;
        }

        public bool Remove(string name)
        {
            var item = this[name];
            return item == null ? false : Remove(item);
        }

        public T Get(string name)
        {
            return this[name];
        }

        public void Set(T value)
        {
            if (value != null)
            {
                this[value.Name] = (T)value;
            }
        }

        public void Set(T value, int index)
        {
            if (value != null)
            {
                Set(value);
                if (index >= Count)
                {
                    index = Count - 1;
                }
                this[index] = (T)value;
            }
        }

        INamed INamedList.Get(string name) => Get(name);

        public void Set(INamed value) => Set((T)value);

        public void Set(INamed value, int index) => Set((T)value, index);
    }


}
