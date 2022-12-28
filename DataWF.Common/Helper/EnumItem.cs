using DataWF.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

[assembly: Invoker(typeof(EnumItem), nameof(EnumItem.Value), typeof(EnumItem.ValueInvoker))]
[assembly: Invoker(typeof(EnumItem), nameof(EnumItem.Name), typeof(EnumItem.NameInvoker))]
[assembly: Invoker(typeof(EnumItem), nameof(EnumItem.Text), typeof(EnumItem.TextInvoker))]
[assembly: Invoker(typeof(EnumItem), nameof(EnumItem.TextUI), typeof(EnumItem.TextUIInvoker))]
[assembly: Invoker(typeof(EnumItem), nameof(EnumItem.Index), typeof(EnumItem.IndexInvoker))]
namespace DataWF.Common
{
    public class EnumItem : ICheck, INamed, INotifyPropertyChanged
    {
        public static Dictionary<Type, EnumItemList> Cache = new Dictionary<Type, EnumItemList>();

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
            var type = item.GetType();
            if (!Cache.TryGetValue(type, out var list))
            {
                Cache[type] = list = new EnumItemList(type);
            }
            var enumItem = list.GetItem(item);

            return enumItem?.TextUI;
        }

        public static string Format(object item)
        {
            var type = item.GetType();
            if (!Cache.TryGetValue(type, out var list))
            {
                Cache[type] = list = new EnumItemList(type);
            }
            var enumItem = list.GetItem(item);

            return enumItem?.Text;
        }

        internal static string FormatInternal(object item, Type type)
        {
            var name = item.ToString();
            var field = type.GetRuntimeField(name);
            if (field != null)
            {
                return field.GetCustomAttribute<EnumMemberAttribute>(false)?.Value ?? name;
            }
            else if (type.GetCustomAttribute<FlagsAttribute>() != null)
            {
                var itemValue = (int)item;
                return string.Join(", ", Enum.GetValues(type)
                    .TypeOf<object>()
                    .Where(p => (int)p != 0 && ((int)p & itemValue) == (int)p)
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
        private LocaleItem localeItem;

        public EnumItem()
        { }

        public EnumItem(object item)
            : this(item, item.ToString(), FormatInternal(item, item.GetType()))
        { }

        public EnumItem(object item, string name, string text)
        {
            Value = item;
            Name = name;
            Text = text;
            localeItem = Locale.GetItem(item.GetType(), text);
            localeItem.PropertyChanged += LocaleItem_PropertyChanged;
            TextUI = localeItem.Value;
        }

        private void LocaleItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            TextUI = localeItem.Value;
            OnPropertyChanged(nameof(TextUI));
        }

        public int Index { get; set; }

        public object Value { get; }

        public string Name { get; set; }

        public string Text { get; set; }

        public string TextUI { get; set; }

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

        public class ValueInvoker : Invoker<EnumItem, object>
        {
            public static readonly ValueInvoker Instance = new ValueInvoker();

            public override string Name => nameof(EnumItem.Value);

            public override bool CanWrite => false;

            public override object GetValue(EnumItem target) => target.Value;

            public override void SetValue(EnumItem target, object value) { }
        }

        public class NameInvoker : Invoker<EnumItem, string>
        {
            public static readonly NameInvoker Instance = new NameInvoker();
            public override string Name => nameof(EnumItem.Name);

            public override bool CanWrite => true;

            public override string GetValue(EnumItem target) => target.Name;

            public override void SetValue(EnumItem target, string value) => target.Name = value;
        }

        public class TextInvoker : Invoker<EnumItem, string>
        {
            public static readonly TextInvoker Instance = new TextInvoker();
            public override string Name => nameof(EnumItem.Text);

            public override bool CanWrite => true;

            public override string GetValue(EnumItem target) => target.Text;

            public override void SetValue(EnumItem target, string value) => target.Text = value;
        }

        public class TextUIInvoker : Invoker<EnumItem, string>
        {
            public static readonly TextInvoker Instance = new TextInvoker();
            public override string Name => nameof(EnumItem.TextUI);

            public override bool CanWrite => true;

            public override string GetValue(EnumItem target) => target.TextUI;

            public override void SetValue(EnumItem target, string value) => target.TextUI = value;
        }

        public class IndexInvoker : Invoker<EnumItem, int>
        {
            public static readonly IndexInvoker Instance = new IndexInvoker();
            public override string Name => nameof(EnumItem.Index);

            public override bool CanWrite => true;

            public override int GetValue(EnumItem target) => target.Index;

            public override void SetValue(EnumItem target, int value) => target.Index = value;
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

    //public class EnumConvert
    //{
    //    [StructLayout(LayoutKind.Explicit)]
    //    struct EnumUnion32<T> where T : enum
    //    {
    //        [FieldOffset(0)]
    //        public T Enum;

    //        [FieldOffset(0)]
    //        public int Int;
    //    }

    //    public static int Enum32ToInt<T>(T e) where T : enum
    //    {
    //        var u = default(EnumUnion32<T>);
    //        u.Enum = e;
    //        return u.Int;
    //    }

    //    public static T IntToEnum32<T>(int value) where T : enum
    //    {
    //        var u = default(EnumUnion32<T>);
    //        u.Int = value;
    //        return u.Enum;
    //    }
    //}
}

