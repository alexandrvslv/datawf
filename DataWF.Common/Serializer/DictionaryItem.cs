using System;
using System.Collections;

namespace DataWF.Common
{
    internal class DictionaryItem : IDictionaryItem
    {
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

        

        public void Reset()
        {
            Key = Value = null;
        }
    }

}
