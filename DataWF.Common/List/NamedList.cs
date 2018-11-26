
using System.Collections.Generic;

namespace DataWF.Common
{
    public class NamedList<T> : SelectableList<T>, INamedList where T : INamed, new()
    {
        static readonly Invoker<T, string> nameInvoker = new Invoker<T, string>(nameof(INamed.Name), (item) => item.Name);

        public NamedList(int capacity) : base(capacity)
        {
            Indexes.Add(nameInvoker);
        }

        public NamedList()
        {
            Indexes.Add(nameInvoker);
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
