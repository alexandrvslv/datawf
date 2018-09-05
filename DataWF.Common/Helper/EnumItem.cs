using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace DataWF.Common
{
    public class EnumItem : ICheck, INotifyPropertyChanged
    {
        private bool check;

        public override string ToString()
        {
            if (Name == null)
            {
                var type = Value.GetType();
                var name = Value.ToString();
                var memeberName = type.GetMember(name)?.FirstOrDefault()?.GetCustomAttribute<EnumMemberAttribute>()?.Value;
                Name = Locale.Get(Locale.GetTypeCategory(type), memeberName ?? name);
            }
            return Name;
        }

        public int Index { get; set; }

        public string Name { get; set; }

        public object Value { get; set; }

        public bool Check
        {
            get { return check; }
            set
            {
                if (this.check != value)
                {
                    this.check = value;
                    OnPropertyChanged(nameof(Check));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }


    public class EnumItemList<T> : SelectableList<EnumItem<T>> where T : struct
    {
        public EnumItemList() : base()
        { }

        public EnumItemList(IEnumerable<EnumItem<T>> items) : base(items)
        { }

        public override int IndexOf(object item)
        {
            if (item == null)
                return -1;
            if (item is T)
            {
                return IndexOf((EnumItem<T>)(T)item);
            }
            return IndexOf((EnumItem<T>)item);
        }

        public override object GetItem(int index)
        {
            return this[index].Value;
        }
    }

    public struct EnumItem<T> where T : struct
    {
        static EnumItem()
        {
            foreach (T enumItem in Enum.GetValues(typeof(T)))
            {
                Cache[enumItem] = new EnumItem<T>(enumItem);
            }
        }

        public static Dictionary<T, EnumItem<T>> Cache = new Dictionary<T, EnumItem<T>>();

        public static bool operator ==(T item, EnumItem<T> enumItem) { return item.Equals(enumItem.Value); }

        public static bool operator !=(T item, EnumItem<T> enumItem) { return !item.Equals(enumItem.Value); }

        public static implicit operator T(EnumItem<T> item) { return item.Value; }

        public static implicit operator EnumItem<T>(T item) { return Cache[item]; }

        public EnumItem(T item)
        {
            Value = item;
            Text = item.ToString();
            var attribute = typeof(T).GetRuntimeField(Text).GetCustomAttribute<EnumMemberAttribute>(false);
            if (attribute != null)
                Text = attribute.Value;
        }

        public T Value { get; set; }

        public string Text { get; set; }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is EnumItem<T> item)
            {
                obj = item.Value;
            }
            return Value.Equals(obj);
        }

        public override string ToString()
        {
            return Text;
        }
    }
}

