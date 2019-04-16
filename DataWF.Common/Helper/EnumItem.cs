using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace DataWF.Common
{
    public class EnumItem : ICheck, INamed, INotifyPropertyChanged
    {
        public static IEnumerable<EnumItem> GetEnumItems(Type type)
        {
            int index = 0;
            foreach (var enumItem in Enum.GetValues(type))
            {
                yield return new EnumItem(enumItem) { Index = index };
                index++;
            }
        }

        public static IEnumerable<EnumItem<T>> GetEnumItems<T>() where T : struct
        {
            int index = 0;
            foreach (T enumItem in Enum.GetValues(typeof(T)))
            {
                yield return new EnumItem<T>(enumItem) { Index = index };
                index++;
            }
        }

        public static string FormatUI(object item)
        {
            return Locale.Get(item.GetType(), Format(item));
        }

        public static string Format(object item)
        {
            var name = item.ToString();
            var type = item.GetType();
            var field = type.GetRuntimeField(name);
            if (field != null)
            {
                return field.GetCustomAttribute<EnumMemberAttribute>(false)?.Value ?? name;
            }
            else if (type.GetCustomAttribute<FlagsAttribute>() != null)
            {
                return string.Join(", ", Enum.GetValues(type)
                    .TypeOf<object>()
                    .Where(p => ((int)p & (int)item) != 0)
                    .Select(p => FormatUI(p)));
            }

            return name;
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

        //public static bool operator ==(object item, EnumItem enumItem) { return item.Equals(enumItem.Value); }

        //public static bool operator !=(object item, EnumItem enumItem) { return !item.Equals(enumItem.Value); }


        private bool check;

        public EnumItem()
        { }

        public EnumItem(object item)
            : this(item, item.ToString(), FormatUI(item))
        { }

        public EnumItem(object item, string name, string text)
        {
            Value = item;
            Name = name;
            Text = text;
        }

        public int Index { get; set; }

        public object Value { get; }

        public string Name { get; set; }

        public string Text { get; set; }

        public bool Check
        {
            get { return check; }
            set
            {
                if (check != value)
                {
                    check = value;
                    OnPropertyChanged(nameof(Check));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is EnumItem item)
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

    public class EnumItem<T> : EnumItem where T : struct
    {
        public static string Format(T item)
        {
            return ((EnumItem<T>)item).Text;
        }

        public static implicit operator T(EnumItem<T> item) { return item.TypedValue; }

        public static implicit operator EnumItem<T>(T item) { return new EnumItem<T>(item); }

        public EnumItem()
        { }

        public EnumItem(T item)
            : base(item, item.ToString(), EnumItem.FormatUI(item))
        { }

        public T TypedValue { get => (T)Value; }


    }
}

