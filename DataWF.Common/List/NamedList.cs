
using System.Collections.Generic;

namespace DataWF.Common
{
    public class NamedList<T> : SelectableList<T>, INamedList where T : INamed, new()
    {
        public NamedList(int capacity) : base(capacity)
        {
            Indexes.Add(NamedNameInvoker<T>.Instance);
        }

        public NamedList()
        {
            Indexes.Add(NamedNameInvoker<T>.Instance);
        }

        public NamedList(IEnumerable<T> items) : this()
        {
            AddRangeInternal(items);
        }

        public T this[string param]
        {
            get { return SelectOne(nameof(INamed.Name), param); }
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

        public bool Remove(string name)
        {
            var item = this[name];
            return item == null ? false : Remove(item);
        }

        public INamed Get(string name)
        {
            return this[name];
        }

        public void Set(INamed value)
        {
            if (value != null)
            {
                this[value.Name] = (T)value;
            }
        }

        public void Set(INamed value, int index)
        {
            if (value != null)
            {
                Set(value);
                this[index] = (T)value;
            }
        }
    }


}
