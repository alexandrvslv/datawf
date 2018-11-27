using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace DataWF.Common
{
    public class EnumItemList : NamedList<EnumItem>
    {
        public static readonly Invoker<EnumItem, object> ValueInvoker = new Invoker<EnumItem, object>(nameof(EnumItem.Value),
            p => p.Value);

        private object itemValue;

        public EnumItemList()
        {
            Indexes.Add(ValueInvoker);
        }

        public EnumItemList(Type type) : this(EnumItem.GetEnumItems(type))
        {
            Type = type;
        }

        public EnumItemList(IEnumerable<EnumItem> items) : this()
        {
            AddRangeInternal(items);
        }

        public object Value
        {
            get { return itemValue; }
            set
            {
                if (itemValue == value)
                    return;
                foreach (var item in this)
                {
                    item.Check = value == null
                        ? false
                        : ((int)value & (int)item.Value) != 0;
                }
                itemValue = value;
                OnPropertyChanged();
            }
        }

        public IEnumerable<object> Values
        {
            get
            {
                foreach (var item in this)
                {
                    if (item.Check)
                    {
                        yield return item.Value;
                    }
                }
            }
        }

        public Type Type { get; }

        public override void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnItemPropertyChanged(sender, e);
            if (e.PropertyName == nameof(EnumItem.Check))
            {
                int temp = 0;
                foreach (var item in this)
                {
                    if (item.Check)
                    {
                        temp |= (int)item.Value;
                    }
                }
                itemValue = Enum.ToObject(Type, temp);
                OnPropertyChanged(nameof(Value));
            }
        }

        public override int IndexOf(object item)
        {
            if (item == null)
                return -1;
            if (item.GetType() == Type)
            {
                item = SelectOne(ValueInvoker.Name, item);
            }
            return IndexOf((EnumItem)item);
        }

    }


    public class TypedEnumItemList : EnumItemList
    {
        public TypedEnumItemList(Type type) : base(type)
        { }

        public override object GetItem(int index)
        {
            return this[index].Value;
        }
    }
}

