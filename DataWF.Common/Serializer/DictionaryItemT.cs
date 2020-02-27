using System;
using System.Collections;
using System.Collections.Generic;

namespace DataWF.Common
{
    public class DictionaryItem<K, V> : IDictionaryItem
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

        [Invoker(typeof(DictionaryItem<,>), nameof(Key))]
        public class KeyInvoker : Invoker<DictionaryItem<K, V>, K>
        {
            public static readonly KeyInvoker Instance = new KeyInvoker();
            public override string Name => nameof(DictionaryItem<K, V>.Key);

            public override bool CanWrite => true;

            public override K GetValue(DictionaryItem<K, V> target) => target.Key;

            public override void SetValue(DictionaryItem<K, V> target, K value) => target.Key = value;
        }

        [Invoker(typeof(DictionaryItem<,>), nameof(Value))]
        public class ValueInvoker : Invoker<DictionaryItem<K, V>, V>
        {
            public static readonly ValueInvoker Instance = new ValueInvoker();
            public override string Name => nameof(DictionaryItem<K, V>.Value);

            public override bool CanWrite => true;

            public override V GetValue(DictionaryItem<K, V> target) => target.Value;

            public override void SetValue(DictionaryItem<K, V> target, V value) => target.Value = value;
        }
    }

}
