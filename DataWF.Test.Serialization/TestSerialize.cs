using NUnit.Framework;
using System;
using System.Collections.Generic;
using DataWF.Serialization;
using System.IO;
using System.Collections;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reflection;
using System.Diagnostics;

namespace DataWF.Test.Serialization;

[TestFixture()]
public partial class TestSerialize
{
    public ArraySegment<byte> Write<T>(ISerializer serializer, T value)
        => serializer.Serialize(value!);

    public T Read<T>(ISerializer serializer, ArraySegment<byte> buffer)
        => serializer.Deserialize<T>(buffer)!;

    [Test]
    public void TestJsonCaseInsensitiveSequentialProperties()
    {
        var json = "{\"field\":7,\"stringproperty\":\"value\",\"intproperty\":42}"u8;
        var serializer = new JsonInvokerSerializer();
        var value = serializer.Deserialize<DataWF.Test.TestClass>(json);

        Assert.That(value, Is.Not.Null);
        Assert.That(value!.Field, Is.EqualTo(7));
        Assert.That(value.StringProperty, Is.EqualTo("value"));
        Assert.That(value.IntProperty, Is.EqualTo(42));

        var reorderedJson = "{\"intproperty\":13,\"unknown\":0,\"stringproperty\":\"reordered\",\"field\":5}"u8;
        var reorderedValue = serializer.Deserialize<DataWF.Test.TestClass>(reorderedJson);

        Assert.That(reorderedValue, Is.Not.Null);
        Assert.That(reorderedValue!.Field, Is.EqualTo(5));
        Assert.That(reorderedValue.StringProperty, Is.EqualTo("reordered"));
        Assert.That(reorderedValue.IntProperty, Is.EqualTo(13));

        var escapedNameValue = serializer.Deserialize<DataWF.Test.TestClass>(
            "{\"\\u0069ntproperty\":24}"u8);
        Assert.That(escapedNameValue, Is.Not.Null);
        Assert.That(escapedNameValue!.IntProperty, Is.EqualTo(24));
    }

    [Test]
    public void TestXMLAllCollection() => TestAllCollection(new XmlInvokerSerializer());

    [Test]
    public void TestJsonAllCollection() => TestAllCollection(new JsonInvokerSerializer());

    [Test]
    public void TestMessagePackAllCollection() => TestAllCollection(new MessagePackInvokerSerializer());

    [Test]
    public void TestMemoryPackAllCollection() => TestAllCollection(new MemoryPackInvokerSerializer());

    public void TestAllCollection(ISerializer serializer)
    {
        string[] stringData = ["one", "two", "three", "four", "five"];
        TestEnumerable(serializer, stringData);

        var nStringArray = new string?[] { "one", "two", "three", "four", "five", null };
        TestEnumerable(serializer, nStringArray);

        int[] intData = [1, 2, 3, 4, 5];
        TestEnumerable(serializer, intData);

        int?[] nIntData = [1, 2, 3, 4, 5, null];
        TestEnumerable(serializer, nIntData);

        var items = TestClass.GenerateList().ToArray();
        TestEnumerable(serializer, items);

        TestDictionary(serializer);

        TestArrayList(serializer);
    }

    public void TestEnumerable<T>(ISerializer serializer, T[] data)
    {
        TestArray(serializer, data);
        TestEnumerable<List<T>, T>(serializer, [.. data]);
        TestEnumerable<LinkedList<T>, T>(serializer, new LinkedList<T>(data));
        TestEnumerable<HashSet<T>, T>(serializer, [.. data]);
        TestEnumerable<SortedSet<T>, T>(serializer, [.. data]);
        TestEnumerable<Queue<T>, T>(serializer, new Queue<T>(data));
        TestEnumerable<Stack<T>, T>(serializer, new Stack<T>(data));
        TestEnumerable<ConcurrentBag<T>, T>(serializer, [.. data]);
        TestEnumerable<ConcurrentQueue<T>, T>(serializer, new ConcurrentQueue<T>(data));
        TestEnumerable<ConcurrentStack<T>, T>(serializer, new ConcurrentStack<T>(data));
        TestEnumerable<ImmutableArray<T>, T>(serializer, [.. data]);
        TestEnumerable<ImmutableList<T>, T>(serializer, [.. data]);
        TestEnumerable<ImmutableHashSet<T>, T>(serializer, [.. data]);
        // TODO TestEnumerable<ImmutableQueue<T>, T>(serializer, [.. data]);
        // TODO TestEnumerable<ImmutableStack<T>, T>(serializer, [.. data]);
        // TODO TestEnumerable<IImmutableQueue<T>, T>(serializer, [.. data]);
        // TODO TestEnumerable<IImmutableStack<T>, T>(serializer, [.. data]);
        TestEnumerable<IImmutableList<T>, T>(serializer, data.ToImmutableList());
        TestEnumerable<IImmutableSet<T>, T>(serializer, data.ToImmutableHashSet());
        TestEnumerable<ISet<T>, T>(serializer, new HashSet<T>(data));
        TestEnumerable<IReadOnlySet<T>, T>(serializer, new HashSet<T>(data));
        TestEnumerable<IList<T>, T>(serializer, data);
        TestEnumerable<IReadOnlyList<T>, T>(serializer, data);
        TestEnumerable<ICollection<T>, T>(serializer, data);
        TestEnumerable<IReadOnlyCollection<T>, T>(serializer, data);
        TestEnumerable<IEnumerable<T>, T>(serializer, data);
    }

    public void TestEnumerable<T, E>(ISerializer serializer, T list)
        where T : IEnumerable<E>
    {
        var buffer = Write(serializer, list);
#if DEBUG
        PrintBuffer(buffer);
#endif
        var newList = Read<T>(serializer, buffer);
        AssertEnumerable(list, newList);
    }

    public void TestArray<T>(ISerializer serializer, T[] list)
    {
        var buffer = Write(serializer, list);
#if DEBUG
        PrintBuffer(buffer);
#endif
        var newList = Read<T[]>(serializer, buffer);
        AssertEnumerable(list, newList);//, EqualityComparer<T>.Default
    }

    public void TestArrayList(ISerializer serializer)
    {
        var list = new ArrayList { "one", 2, 3.0M, new TestClass { IntProperty = 1, DecimalProperty = 2.2M, StringProperty = "bla" } };
        var buffer = Write(serializer, list);
#if DEBUG
        PrintBuffer(buffer);
#endif
        var newList = Read<ArrayList>(serializer, buffer);
        Assert.AreEqual(list.Count, newList.Count);
        Assert.AreEqual(list[0], newList[0]);
        Assert.IsInstanceOf<TestClass>(newList[3]);
        AssertTestClass((TestClass)list[3]!, (TestClass)newList[3]!);
    }

    public void TestDictionary(ISerializer serializer)
    {
        TestDictionary(serializer, new Dictionary<string, int> { { "one", 1 }, { "two", 2 }, { "three", 3 }, { "four", 4 }, { "five", 5 } });
        TestDictionary(serializer, new Dictionary<string, int?> { { "one", 1 }, { "two", 2 }, { "three", 3 }, { "four", 4 }, { "five", 5 }, { "null", null } });
        TestDictionary(serializer, new Dictionary<int, string> { { 1, "one" }, { 2, "two" }, { 3, "three" }, { 4, "four" }, { 5, "five" } });
        TestDictionary(serializer, new Dictionary<int, string?> { { 1, "one" }, { 2, "two" }, { 3, "three" }, { 4, "four" }, { 5, "five" }, { 6, null } });
        TestDictionary(serializer, TestClass.GenerateList().Select((x, i) => (i, x)).ToDictionary(x => x.i, x => x.x));
        TestDictionary(serializer, new Dictionary<int, TestClass?> {
            { 1, TestClass.GenerateTestClass(1) },
            { 2, TestClass.GenerateTestClass(2) },
            { 3, TestClass.GenerateTestClass(3) },
            { 4, TestClass.GenerateTestClass(4) },
            { 5, TestClass.GenerateTestClass(5) },
            { 6, null }
        });
    }

    public void TestDictionary<K, V>(ISerializer serializer, Dictionary<K, V> data)
        where K : notnull
    {
        TestDictionary<Dictionary<K, V>, K, V>(serializer, data);
        TestDictionary<SortedList<K, V>, K, V>(serializer, new SortedList<K, V>(data));
        TestDictionary<SortedDictionary<K, V>, K, V>(serializer, new SortedDictionary<K, V>(data));
        TestDictionary<ConcurrentDictionary<K, V>, K, V>(serializer, new ConcurrentDictionary<K, V>(data));
        TestDictionary<ImmutableDictionary<K, V>, K, V>(serializer, data.ToImmutableDictionary());
        TestDictionary<ImmutableSortedDictionary<K, V>, K, V>(serializer, data.ToImmutableSortedDictionary());
        TestDictionary<IDictionary<K, V>, K, V>(serializer, data);
        TestReadOnlyDictionary<IReadOnlyDictionary<K, V>, K, V>(serializer, data);
        TestReadOnlyDictionary<IImmutableDictionary<K, V>, K, V>(serializer, data.ToImmutableDictionary());
    }

    public void TestDictionary<T, K, V>(ISerializer serializer, T dict)
        where T : IDictionary<K, V>
        where K : notnull
    {
        var buffer = Write(serializer, dict);
#if DEBUG
        PrintBuffer(buffer);
#endif
        var newDict = Read<T>(serializer, buffer);
        Assert.AreEqual(dict.Count, newDict.Count);
        foreach (var key in dict.Keys)
        {
            AssertGeneric(dict[key], newDict[key]);
        }
    }

    public void TestReadOnlyDictionary<T, K, V>(ISerializer serializer, T dict)
        where T : IReadOnlyDictionary<K, V>
        where K : notnull
    {
        SerializeProvider.RegisterDictionary<T, K, V>();

        var buffer = Write(serializer, dict);
#if DEBUG
        PrintBuffer(buffer);
#endif
        var newDict = Read<T>(serializer, buffer);
        Assert.AreEqual(dict.Count, newDict.Count);
        foreach (var key in dict.Keys)
        {
            AssertGeneric(dict[key], newDict[key]);
        }
    }

    [Test]
    public void TestXMLClass() => TestClassSerializer(new XmlInvokerSerializer()
#if DEBUG
    { Indented = true }
#endif
    );

    [Test]
    public void TestJsonClass() => TestClassSerializer(new JsonInvokerSerializer());

    [Test]
    public void TestMemoryPackClass() => TestClassSerializer(new MemoryPackInvokerSerializer());

    [Test]
    public void TestMessagePackClass() => TestClassSerializer(new MessagePackInvokerSerializer());

    public void TestClassSerializer(BaseSerializer serializer)
    {
        TestClass item = TestClass.GenerateTestClass(5);
        var buffer = Write(serializer, item);
#if DEBUG
        PrintBuffer(buffer);
#endif
        var newItem = Read<TestClass>(serializer, buffer);
        AssertTestClass(item, newItem);
    }

    [Test]
    public Task TestXMLStream() => TestStream(new XmlInvokerSerializer() { Indented = true });

    [Test]
    public Task TestJsonStream() => TestStream(new JsonInvokerSerializer());

    [Test]
    public Task TestMemoryPackStream() => TestStream(new MemoryPackInvokerSerializer());

    [Test]
    public Task TestMessagePackStream() => TestStream(new MessagePackInvokerSerializer());

    public async Task TestStream(ISerializer serializer)
    {
        List<TestClass> items = TestClass.GenerateList();
        var file = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "Test-" + serializer.GetType().Name);
        await serializer.SerializeAsync(file, items);
        var newItems = await serializer.DeserializeAsync<List<TestClass>>(file, false);
        AssertEnumerable(items, newItems!);
    }

    private static void AssertEnumerable<T>(IEnumerable<T> items, IEnumerable<T> newItems)
    {
        Assert.NotNull(newItems);
        Assert.AreEqual(items.Count(), newItems!.Count());
        var itemsEnumer = items.GetEnumerator();
        var newItemsEnumer = newItems.GetEnumerator();
        while (itemsEnumer.MoveNext() & newItemsEnumer.MoveNext())
        {
            AssertGeneric(itemsEnumer.Current, newItemsEnumer.Current);
        }
    }

    private static void AssertGeneric<T>(T item, T newItem)
    {
        switch (item)
        {
            case TestClass itemClass:
                AssertTestClass(itemClass, (TestClass)(object)newItem!);
                break;
            default:
                Assert.AreEqual(item, newItem);
                break;
        }
    }

    private static void AssertTestClass(TestClass item, TestClass newItem)
    {
        Assert.NotNull(newItem);
        Assert.AreEqual(item.Field, newItem.Field);
        Assert.AreEqual(item.VirtualProperty, newItem.VirtualProperty);
        Assert.AreEqual(item.NStringProperty, newItem.NStringProperty);
        Assert.AreEqual(item.CharProperty, newItem.CharProperty);
        Assert.AreEqual(item.NCharProperty, newItem.NCharProperty);
        Assert.AreEqual(item.GuidProperty, newItem.GuidProperty);
        Assert.AreEqual(item.NGuidProperty, newItem.NGuidProperty);
        Assert.AreEqual(item.IntProperty, newItem.IntProperty);
        Assert.AreEqual(item.NIntProperty, newItem.NIntProperty);
        Assert.AreEqual(item.UIntProperty, newItem.UIntProperty);
        Assert.AreEqual(item.NUIntProperty, newItem.NUIntProperty);
        Assert.AreEqual(item.ShortProperty, newItem.ShortProperty);
        Assert.AreEqual(item.NShortProperty, newItem.NShortProperty);
        Assert.AreEqual(item.UShortProperty, newItem.UShortProperty);
        Assert.AreEqual(item.NUShortProperty, newItem.NUShortProperty);
        Assert.AreEqual(item.LongProperty, newItem.LongProperty);
        Assert.AreEqual(item.NLongProperty, newItem.NLongProperty);
        Assert.AreEqual(item.ULongProperty, newItem.ULongProperty);
        Assert.AreEqual(item.NULongProperty, newItem.NULongProperty);
        Assert.AreEqual(item.SByteProperty, newItem.SByteProperty);
        Assert.AreEqual(item.NSByteProperty, newItem.NSByteProperty);
        Assert.AreEqual(item.ByteProperty, newItem.ByteProperty);
        Assert.AreEqual(item.NByteProperty, newItem.NByteProperty);
        Assert.AreEqual(item.DecimalProperty, newItem.DecimalProperty);
        Assert.AreEqual(item.NDecimalProperty, newItem.NDecimalProperty);
        Assert.AreEqual(item.DoubleProperty, newItem.DoubleProperty);
        Assert.AreEqual(item.NDoubleProperty, newItem.NDoubleProperty);
        Assert.AreEqual(item.FloatProperty, newItem.FloatProperty);
        Assert.AreEqual(item.NFloatProperty, newItem.NFloatProperty);
        Assert.AreEqual(item.StringProperty, newItem.StringProperty);
        Assert.AreEqual(item.DateOnlyProperty, newItem.DateOnlyProperty);
        Assert.AreEqual(item.NDateOnlyProperty, newItem.NDateOnlyProperty);
        Assert.AreEqual(item.TimeOnlyProperty, newItem.TimeOnlyProperty);
        Assert.AreEqual(item.NTimeOnlyProperty, newItem.NTimeOnlyProperty);
        Assert.AreEqual(item.DateTimeProperty, newItem.DateTimeProperty);
        Assert.AreEqual(item.NDateTimeProperty, newItem.NDateTimeProperty);
        Assert.AreEqual(item.DateTimeOffsetProperty, newItem.DateTimeOffsetProperty);
        Assert.AreEqual(item.NDateTimeOffsetProperty, newItem.NDateTimeOffsetProperty);
        Assert.AreEqual(item.TimeSpanProperty, newItem.TimeSpanProperty);
        Assert.AreEqual(item.NTimeSpanProperty, newItem.NTimeSpanProperty);
        Assert.AreEqual(item.Struct.Field, newItem.Struct.Field);
        Assert.AreEqual(item.Struct.IntProperty, newItem.Struct.IntProperty);
        Assert.AreEqual(item.Struct.NIntProperty, newItem.Struct.NIntProperty);
        Assert.AreEqual(item.NStruct.HasValue, newItem.NStruct.HasValue);
        if (item.NStruct.HasValue)
        {
            Assert.AreEqual(item.NStruct.Value.Field, newItem.NStruct!.Value.Field);
            Assert.AreEqual(item.NStruct.Value.IntProperty, newItem.NStruct.Value.IntProperty);
            Assert.AreEqual(item.NStruct.Value.NIntProperty, newItem.NStruct.Value.NIntProperty);
        }
        Assert.IsTrue(item.IntListProperty.SequenceEqual(newItem.IntListProperty));
        Assert.That(newItem.NIntListProperty, Is.EqualTo(item.NIntListProperty));
        Assert.IsTrue(item.IntArray.SequenceEqual(newItem.IntArray));
        Assert.IsTrue(item.CharArray.SequenceEqual(newItem.CharArray));
        Assert.IsTrue(item.ByteArray.SequenceEqual(newItem.ByteArray));
        Assert.That(newItem.NIntArray, Is.EqualTo(item.NIntArray));
        Assert.AreEqual(item.Int8BaseEnumProperty, newItem.Int8BaseEnumProperty);
        Assert.AreEqual(item.UInt8BaseEnumProperty, newItem.UInt8BaseEnumProperty);
        Assert.AreEqual(item.Int16BaseEnumProperty, newItem.Int16BaseEnumProperty);
        Assert.AreEqual(item.UInt16BaseEnumProperty, newItem.UInt16BaseEnumProperty);
        Assert.AreEqual(item.Int32BaseEnumProperty, newItem.Int32BaseEnumProperty);
        Assert.AreEqual(item.UInt32BaseEnumProperty, newItem.UInt32BaseEnumProperty);
        Assert.AreEqual(item.Int64BaseEnumProperty, newItem.Int64BaseEnumProperty);
        Assert.AreEqual(item.UInt64BaseEnumProperty, newItem.UInt64BaseEnumProperty);
        Assert.AreEqual(item.NInt8BaseEnumProperty, newItem.NInt8BaseEnumProperty);
        Assert.AreEqual(item.NUInt8BaseEnumProperty, newItem.NUInt8BaseEnumProperty);
        Assert.AreEqual(item.NInt16BaseEnumProperty, newItem.NInt16BaseEnumProperty);
        Assert.AreEqual(item.NUInt16BaseEnumProperty, newItem.NUInt16BaseEnumProperty);
        Assert.AreEqual(item.NInt32BaseEnumProperty, newItem.NInt32BaseEnumProperty);
        Assert.AreEqual(item.NUInt32BaseEnumProperty, newItem.NUInt32BaseEnumProperty);
        Assert.AreEqual(item.NInt64BaseEnumProperty, newItem.NInt64BaseEnumProperty);
        Assert.AreEqual(item.NUInt64BaseEnumProperty, newItem.NUInt64BaseEnumProperty);
        Assert.AreEqual(item.Group is null, newItem.Group is null);
        if (item.Group is not null)
        {
            AssertTestClass(item.Group, newItem.Group!);
        }
        Assert.AreEqual(item.Record is null, newItem.Record is null);
        if (item.Record is not null)
        {
            Assert.AreEqual(item.Record.IntProperty, newItem.Record!.IntProperty);
            Assert.AreEqual(item.Record.NIntProperty, newItem.Record.NIntProperty);
            Assert.AreEqual(item.Record.StringProperty, newItem.Record.StringProperty);
            Assert.IsTrue(item.Record.IntArrayProperty.SequenceEqual(newItem.Record.IntArrayProperty));
        }
        Assert.AreEqual(item.RecordStruct.HasValue, newItem.RecordStruct.HasValue);
        if (item.RecordStruct.HasValue)
        {
            Assert.AreEqual(item.RecordStruct.Value.IntProperty, newItem.RecordStruct!.Value.IntProperty);
            Assert.AreEqual(item.RecordStruct.Value.NIntProperty, newItem.RecordStruct.Value.NIntProperty);
            Assert.AreEqual(item.RecordStruct.Value.StringProperty, newItem.RecordStruct.Value.StringProperty);
            Assert.IsTrue(item.RecordStruct.Value.IntArrayProperty.SequenceEqual(newItem.RecordStruct.Value.IntArrayProperty));
        }
    }

    //     [Test]
    //     public void TestXMLSerializeableElementList()
    //     {
    //         List<TestSerializableElement> items = GenerateSEList();
    //         var serialiser = new XmlInvokerSerializer();
    //         var buffer = Write(serialiser, items);
    // #if DEBUG
    //         PrintBuffer(buffer);
    // #endif
    //         var newItems = Read<List<TestSerializableElement>>(serialiser, buffer);
    //         Assert.AreEqual(items.Count, newItems.Count);
    //         Assert.AreEqual(items[1].ToSerialize.IntProperty, newItems[1].ToSerialize.IntProperty);
    //         Assert.AreEqual(items[1].ToSerialize.DecimalProperty, newItems[1].ToSerialize.DecimalProperty);
    //         Assert.AreEqual(items[1].ToSerialize.StringProperty, newItems[1].ToSerialize.StringProperty);
    //         Assert.AreEqual(items[1].ToSerialize.Group.StringProperty, newItems[1].ToSerialize.Group.StringProperty);
    //     }

    // [Test]
    // public void BinaryStreamHolder()
    // {
    //     var streamData = new byte[] { 1, 0, 1, 0, 1, 0, 1, 0 };
    //     var streamHolder = new StreamHolder
    //     {
    //         FileName = "some_name.ext",
    //         Stream = new MemoryStream(streamData)
    //     };
    //     var serializer = new BinaryInvokerSerializer();
    //     var buffer = serializer.Serialize(streamHolder);
    //     var newStreamHolder = serializer.Deserialize<StreamHolder>(buffer, null);
    //     Assert.AreEqual(streamHolder.FileName, newStreamHolder.FileName);
    //     var newStreamData = newStreamHolder.Stream.GetBytes(8);
    //     Assert.IsTrue(streamData.AsSpan().SequenceEqual(newStreamData));
    // }

    private void PrintBuffer(ArraySegment<byte> buffer)
    {
        var lineLenth = 32;
        var bytes = buffer.AsSpan();
        WriteLine($"Buffer: {bytes.Length} bytes");
        Span<char> hex = stackalloc char[lineLenth * 3];
        Span<char> ascii = stackalloc char[lineLenth];
        for (var offset = 0; offset < bytes.Length; offset += lineLenth)
        {
            var line = bytes.Slice(offset, Math.Min(lineLenth, bytes.Length - offset));
            hex.Fill(' ');

            for (var i = 0; i < line.Length; i++)
            {
                var value = line[i];
                var hexOffset = i * 3;
                hex[hexOffset] = GetHexDigit(value >> 4);
                hex[hexOffset + 1] = GetHexDigit(value & 0x0F);
                ascii[i] = value is >= 0x20 and <= 0x7E ? (char)value : '.';
            }

            WriteLine(
                $"{offset:X8}  {new string(hex)}  |{new string(ascii[..line.Length])}|");
        }

        static char GetHexDigit(int value) => (char)(value < 10 ? '0' + value : 'A' + value - 10);
    }

    private void WriteLine(string value)
    {
        //TestContext.Progress.WriteLine(value);
        //Console.WriteLine(value);
        Debug.WriteLine(value);
    }
}
