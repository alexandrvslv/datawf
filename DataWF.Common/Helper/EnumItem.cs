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
        public static string Format(object item)
        {
            var text = item.ToString();
            var field = item.GetType().GetRuntimeField(text);
            return field?.GetCustomAttribute<EnumMemberAttribute>(false)?.Value ?? text;
        }

        public static object Parse(Type type, string value)
        {
            try
            {
                return Enum.Parse(type, value);
            }
            catch
            {
                var itemType = (typeof(EnumItem<>)).MakeGenericType(type);
                return EmitInvoker.Invoke(itemType, nameof(Parse), null, value);
            }
        }

        private bool check;

        public override string ToString()
        {
            if (Name == null)
            {
                Name = Format(Value);
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

        public static object Parse(string value)
        {
            bool checkInt = int.TryParse(value, out var intValue);
            return Cache.Values.FirstOrDefault(p => p.Name.Equals(value, StringComparison.OrdinalIgnoreCase)
            || p.Text.Equals(value, StringComparison.OrdinalIgnoreCase)
            || (checkInt && value == p.Value.ToString())).Value;
        }

        public static Dictionary<T, EnumItem<T>> Cache = new Dictionary<T, EnumItem<T>>();

        public static string Format(T item)
        {
            return ((EnumItem<T>)item).Text;
        }

        public static bool operator ==(T item, EnumItem<T> enumItem) { return item.Equals(enumItem.Value); }

        public static bool operator !=(T item, EnumItem<T> enumItem) { return !item.Equals(enumItem.Value); }

        public static implicit operator T(EnumItem<T> item) { return item.Value; }

        public static implicit operator EnumItem<T>(T item) { return Cache.TryGetValue(item, out var value) ? value : (Cache[item] = new EnumItem<T>(item)); }

        public EnumItem(T item):this(item, 
            item.ToString(), 
            typeof(T).GetRuntimeField(item.ToString())?
            .GetCustomAttribute<EnumMemberAttribute>(false)?
            .Value ?? item.ToString())
        {
        }

        public EnumItem(T item, string name, string text)
        {
            Value = item;
            Name = name;
            Text = text;
        }

        public T Value { get; }
        public string Name { get; }
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

