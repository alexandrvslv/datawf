using DataWF.Common;
using System;
using System.Collections;
using System.Collections.Generic;

[assembly: Invoker(typeof(Dictionary<,>), nameof(Dictionary<object, object>.Comparer), typeof(DictionaryComparerInvoker<,>))]
[assembly: Invoker(typeof(Dictionary<,>), nameof(Dictionary<object, object>.Count), typeof(DictionaryCountInvoker<,>))]
[assembly: Invoker(typeof(Dictionary<,>), nameof(Dictionary<object, object>.Keys), typeof(DictionaryKeysInvoker<,>))]
[assembly: Invoker(typeof(Dictionary<,>), nameof(Dictionary<object, object>.Values), typeof(DictionaryValuesInvoker<,>))]
[assembly: Invoker(typeof(DictionaryItem<,>), nameof(DictionaryItem<object, object>.Key), typeof(DictionaryItem<,>.KeyInvoker))]
[assembly: Invoker(typeof(DictionaryItem<,>), nameof(DictionaryItem<object, object>.Value), typeof(DictionaryItem<,>.ValueInvoker))]
[assembly: Invoker(typeof(List<>), nameof(List<object>.Count), typeof(ListCountInvoker<>))]
[assembly: Invoker(typeof(List<>), nameof(List<object>.Capacity), typeof(ListCapacityInvoker<>))]
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

        public class KeyInvoker : Invoker<DictionaryItem<K, V>, K>
        {
            public override string Name => nameof(DictionaryItem<K, V>.Key);

            public override bool CanWrite => true;

            public override K GetValue(DictionaryItem<K, V> target) => target.Key;

            public override void SetValue(DictionaryItem<K, V> target, K value) => target.Key = value;
        }


        public class ValueInvoker : Invoker<DictionaryItem<K, V>, V>
        {
            public override string Name => nameof(DictionaryItem<K, V>.Value);

            public override bool CanWrite => true;

            public override V GetValue(DictionaryItem<K, V> target) => target.Value;

            public override void SetValue(DictionaryItem<K, V> target, V value) => target.Value = value;
        }
    }

    public class DictionaryComparerInvoker<K, V> : Invoker<Dictionary<K, V>, IEqualityComparer<K>>
    {
        public override string Name => nameof(Dictionary<K, V>.Comparer);

        public override bool CanWrite => false;

        public override IEqualityComparer<K> GetValue(Dictionary<K, V> target) => target.Comparer;

        public override void SetValue(Dictionary<K, V> target, IEqualityComparer<K> value) { }
    }

    public class DictionaryCountInvoker<K, V> : Invoker<Dictionary<K, V>, int>
    {
        public override string Name => nameof(Dictionary<K, V>.Count);

        public override bool CanWrite => false;

        public override int GetValue(Dictionary<K, V> target) => target.Count;

        public override void SetValue(Dictionary<K, V> target, int value) { }
    }

    public class DictionaryKeysInvoker<K, V> : Invoker<Dictionary<K, V>, Dictionary<K, V>.KeyCollection>
    {
        public override string Name => nameof(Dictionary<K, V>.Keys);

        public override bool CanWrite => false;

        public override Dictionary<K, V>.KeyCollection GetValue(Dictionary<K, V> target) => target.Keys;

        public override void SetValue(Dictionary<K, V> target, Dictionary<K, V>.KeyCollection value) { }
    }

    public class DictionaryValuesInvoker<K, V> : Invoker<Dictionary<K, V>, Dictionary<K, V>.ValueCollection>
    {
        public override string Name => nameof(Dictionary<K, V>.Values);

        public override bool CanWrite => false;

        public override Dictionary<K, V>.ValueCollection GetValue(Dictionary<K, V> target) => target.Values;

        public override void SetValue(Dictionary<K, V> target, Dictionary<K, V>.ValueCollection value) { }
    }

    public class ListCountInvoker<T> : Invoker<List<T>, int>
    {
        public override string Name => nameof(List<T>.Count);

        public override bool CanWrite => false;

        public override int GetValue(List<T> target) => target.Count;

        public override void SetValue(List<T> target, int value) { }
    }

    public class ListCapacityInvoker<T> : Invoker<List<T>, int>
    {
        public override string Name => nameof(List<T>.Capacity);

        public override bool CanWrite => true;

        public override int GetValue(List<T> target) => target.Capacity;

        public override void SetValue(List<T> target, int value) => target.Capacity = value;
    }

}
