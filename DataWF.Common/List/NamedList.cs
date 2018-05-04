using DataWF.Common;

namespace DataWF.Gui
{
    public class NamedList<T> : SelectableList<T>, INamedList where T : INamed, new()
    {
        static readonly Invoker<T, string> nameInvoker = new Invoker<T, string>(nameof(INamed.Name), (item) => item.Name);

        public NamedList(int capacity) : base(capacity)
        { }

        public NamedList()
        {
            Indexes.Add(nameInvoker);
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
    }


}
