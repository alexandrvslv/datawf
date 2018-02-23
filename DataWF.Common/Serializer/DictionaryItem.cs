using System;
using System.Collections;

namespace DataWF.Common
{
    internal class DictionaryItem : IDictionaryItem
    {
        public static IDictionaryItem Create(Type type)
        {
            return !type.IsGenericType
               ? new DictionaryItem()
               : (IDictionaryItem)TypeHelper.CreateObject(typeof(DictionaryItem<,>).MakeGenericType(type.GetGenericArguments()));
        }

        public DictionaryItem()
        { }

        public DictionaryItem(DictionaryEntry entry)
        {
            Key = entry.Key;
            Value = entry.Value;
        }

        public object Key { get; set; }

        public object Value { get; set; }

        public void Fill(object value)
        {
            Fill((DictionaryEntry)value);
        }

        public void Fill(DictionaryEntry value)
        {
            Key = value.Key;
            Value = value.Value;
        }

        public void Reset()
        {
            Key = Value = null;
        }
    }

}
