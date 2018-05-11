using System;
using System.Collections;
using System.Collections.Generic;

namespace DataWF.Common
{
    internal class DictionaryItem<K, V> : IDictionaryItem
    {
        

        public K Key { get; set; }

        public V Value { get; set; }

        object IDictionaryItem.Key { get => Key; set => Key = (K)value; }

        object IDictionaryItem.Value { get => Value; set => Value = (V)value; }

        public void Fill(object value)
        {
            if (value is DictionaryEntry)
                Fill((DictionaryEntry)value);
            else
                Fill((KeyValuePair<K, V>)value);
        }

        public void Fill(KeyValuePair<K, V> value)
        {
            Key = value.Key;
            Value = value.Value;
        }

        public void Fill(DictionaryEntry value)
        {
            Key = (K)value.Key;
            Value = (V)value.Value;
        }

        public void Reset()
        {
            Key = default(K);
            Value = default(V);
        }
    }

}
