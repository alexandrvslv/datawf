using NUnit.Framework;
using System;
using System.Collections.Generic;
using DataWF.Common;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Xml.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace DataWF.Test.Common
{
    public enum Serializer
    {
        Xml,
        Binary,
        SystemXml,
        SystemJson
    }

    public enum SerializeDirection
    {
        Write,
        Read
    }

    [TestFixture()]
    public partial class TestSerialize
    {
        public ArraySegment<byte> TestWrite<T>(BaseSerializer serializer, T list)
        {
            return serializer.Serialize(list);
        }

        public ArraySegment<byte> TestWrite<T>(XmlSerializer serializer, T list)
        {
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(stream, list);
                return stream.TryGetBuffer(out var buffer) ? buffer : new ArraySegment<byte>(stream.ToArray());
            }
        }

        public ArraySegment<byte> TestJsonWrite<T>(T list)
        {
            using (var stream = new MemoryStream())
            using (var jsonWriter = new Utf8JsonWriter(stream))
            {
                JsonSerializer.Serialize(jsonWriter, list);
                return stream.ToArray();
            }
        }

        public T TestRead<T>(BaseSerializer serializer, ArraySegment<byte> buffer)
        {
            return serializer.Deserialize(buffer, default(T));
        }

        public T TestRead<T>(XmlSerializer serializer, ArraySegment<byte> buffer)
        {
            using (var stream = new MemoryStream(buffer.Array, buffer.Offset, buffer.Count))
            {
                return (T)serializer.Deserialize(stream);
            }
        }

        public T TestJsonRead<T>(ArraySegment<byte> buffer)
        {
            var reader = new System.Text.Json.Utf8JsonReader(buffer);
            return JsonSerializer.Deserialize<T>(ref reader);
        }

        [Test, Combinatorial]
        public void Benchmark([Values(10)] int listLength,
            [Values(100, 10000)] int iteration,
            [Values(SerializeDirection.Write, SerializeDirection.Read)] SerializeDirection direction,
            [Values(Serializer.SystemXml, Serializer.SystemJson, Serializer.Xml, Serializer.Binary)] Serializer serializerType)
        {
            switch (serializerType)
            {
                case Serializer.Xml:
                    {
                        var list = GenerateList(listLength);
                        var serializer = new XmlTextSerializer(typeof(List<TestSerializeClass>));
                        if (direction == SerializeDirection.Write)
                        {
                            for (var i = 0; i < iteration; i++)
                                TestWrite(serializer, list);
                        }
                        else
                        {
                            var buffer = TestWrite(serializer, list);
                            for (var i = 0; i < iteration; i++)
                                TestRead<List<TestSerializeClass>>(serializer, buffer);
                        }
                    }
                    break;
                case Serializer.Binary:
                    {
                        var list = GenerateList(listLength);
                        var serializer = new BinarySerializer(typeof(List<TestSerializeClass>));
                        if (direction == SerializeDirection.Write)
                        {
                            for (var i = 0; i < iteration; i++)
                                TestWrite(serializer, list);
                        }
                        else
                        {
                            var buffer = TestWrite(serializer, list);
                            for (var i = 0; i < iteration; i++)
                                TestRead<List<TestSerializeClass>>(serializer, buffer);
                        }
                    }
                    break;
                case Serializer.SystemXml:
                    {
                        var list = GenerateList(listLength);
                        var serializer = new XmlSerializer(typeof(List<TestSerializeClass>));

                        if (direction == SerializeDirection.Write)
                        {
                            for (var i = 0; i < iteration; i++)
                                TestWrite(serializer, list);
                        }
                        else
                        {
                            var buffer = TestWrite(serializer, list);
                            for (var i = 0; i < iteration; i++)
                                TestRead<List<TestSerializeClass>>(serializer, buffer);
                        }
                    }
                    break;
                case Serializer.SystemJson:
                    {
                        var list = GenerateList(listLength);
                        if (direction == SerializeDirection.Write)
                        {
                            for (var i = 0; i < iteration; i++)
                                TestJsonWrite(list);
                        }
                        else
                        {
                            var buffer = TestJsonWrite(list);
                            for (var i = 0; i < iteration; i++)
                                TestJsonRead<List<TestSerializeClass>>(buffer);
                        }
                    }
                    break;
            }

        }

        [Test]
        public void TestXMLGenericList()
        {
            TestGenericList(new XmlTextSerializer(typeof(List<string>)));
        }

        [Test]
        public void TestBinaryGenericList()
        {
            TestGenericList(new BinarySerializer(typeof(List<string>)));
        }

        public void TestGenericList(BaseSerializer serializer)
        {
            var list = new List<string> { "one", "two", "three" };
            var buffer = TestWrite(serializer, list);
#if DEBUG
            PrintBuffer(buffer);
#endif
            var newList = TestRead<List<string>>(serializer, buffer);
            Assert.AreEqual(list.Count, newList.Count, "Deserialization Fail");
            Assert.AreEqual(list[0], newList[0], "Deserialization Fail");
        }

        [Test]
        public void TestXMLNativeGenericList()
        {
            var list = new List<string> { "one", "two", "three" };
            var serializer = new XmlSerializer(typeof(List<string>));
            TestNativeGenericList(serializer, list);
        }

        public void TestNativeGenericList<T>(XmlSerializer serializer, T list) where T : List<string>
        {
            var buffer = TestWrite(serializer, list);
#if DEBUG
            PrintBuffer(buffer);
#endif
            var newList = TestRead<T>(serializer, buffer);
            Assert.AreEqual(list.Count, newList.Count, "Deserialization Fail");
            Assert.AreEqual(list[0], newList[0], "Deserialization Fail");
        }

        [Test]
        public void TestXMLStringArray()
        {
            TestStringArray(new XmlTextSerializer(typeof(string[])));
        }

        [Test]
        public void TestBinaryStringArray()
        {
            TestStringArray(new BinarySerializer(typeof(string[])));
        }

        public void TestStringArray(BaseSerializer serializer)
        {
            var list = new string[] { "one", "two", "three", "four", "five" };
            var buffer = TestWrite(serializer, list);
#if DEBUG
            PrintBuffer(buffer);
#endif
            var newList = TestRead<string[]>(serializer, buffer);
            Assert.AreEqual(list.Length, newList.Length, "Deserialization Fail");
            Assert.AreEqual(list[0], newList[0], "Deserialization Fail");
        }

        [Test]
        public void TestXMLArrayList()
        {
            TestArrayList(new XmlTextSerializer());
        }

        [Test]
        public void TestBinaryArrayList()
        {
            TestArrayList(new BinarySerializer());
        }

        public void TestArrayList(BaseSerializer serializer)
        {
            var list = new ArrayList { "one", 2, 3.0M, new TestSerializeClass { IntValue = 1, DecimalValue = 2.2M, StringValue = "bla" } };
            var buffer = TestWrite(serializer, list);
#if DEBUG
            PrintBuffer(buffer);
#endif
            var newList = TestRead<ArrayList>(serializer, buffer);
            Assert.AreEqual(list.Count, newList.Count, "Deserialization Fail");
            Assert.AreEqual(list[0], newList[0], "Deserialization Fail");
            Assert.IsInstanceOf<TestSerializeClass>(newList[3], "Deserialization Fail");
            Assert.AreEqual(((TestSerializeClass)list[3]).StringValue, ((TestSerializeClass)newList[3]).StringValue, "Deserialization Fail");
        }

        [Test]
        public void TestXMLDictionary()
        {
            TestDictionary(new XmlTextSerializer());
        }

        [Test]
        public void TestBinaryDictionary()
        {
            TestDictionary(new BinarySerializer());
        }

        public void TestDictionary(BaseSerializer serializer)
        {
            var dict = new Dictionary<string, int> { { "one", 1 }, { "two", 2 }, { "three", 3 }, { "four", 4 }, { "five", 5 } };

            var buffer = TestWrite(serializer, dict);
#if DEBUG
            PrintBuffer(buffer);
#endif
            var newDict = TestRead<Dictionary<string, int>>(serializer, buffer);
            Assert.AreEqual(dict.Count, newDict.Count, "Deserialization Fail");
            Assert.AreEqual(dict["one"], newDict["one"], "Deserialization Fail");
        }

        [Test]
        public void TestXMLHashtable()
        {
            TestHashtable(new XmlTextSerializer());
        }

        [Test]
        public void TestBinaryHashtable()
        {
            TestHashtable(new BinarySerializer());
        }

        public void TestHashtable(BaseSerializer serialiser)
        {
            var dict = new Hashtable { { "one", 1 }, { "two", 2 }, { "three", 3 }, { "four", 4 }, { "five", 5 } };
            var buffer = TestWrite(serialiser, dict);
#if DEBUG
            PrintBuffer(buffer);
#endif
            var newDict = TestRead<Hashtable>(serialiser, buffer);
            Assert.AreEqual(dict.Count, newDict.Count, "Deserialization Fail");
            Assert.AreEqual(dict["one"], newDict["one"], "Deserialization Fail");
        }

        [Test]
        public void TestXMLClass()
        {
            TestClass(new XmlTextSerializer());
        }

        [Test]
        public void TestBinaryClass()
        {
            TestClass(new BinarySerializer());
        }

        public void TestClass(BaseSerializer serializer)
        {
            TestSerializeClass item = GenerateClass();
            var buffer = TestWrite(serializer, item);
#if DEBUG
            PrintBuffer(buffer);
#endif
            var newItem = TestRead<TestSerializeClass>(serializer, buffer);
            Assert.AreEqual(item.IntValue, newItem.IntValue, "Deserialization Int Value Fail");
            Assert.AreEqual(item.NullableIntValue, newItem.NullableIntValue, "Deserialization Nullable Int Value Fail");
            Assert.AreEqual(item.DecimalValue, newItem.DecimalValue, "Deserialization Decimal Value Fail");
            Assert.AreEqual(item.NullableDecimalValue, newItem.NullableDecimalValue, "Deserialization Nullable Decimal Value Fail");
            Assert.AreEqual(item.StringValue, newItem.StringValue, "Deserialization String Value Fail");
            Assert.AreEqual(item.ClassValue.StringValue, newItem.ClassValue.StringValue, "Deserialization Class Value Fail");
        }

        [Test]
        public void TestXMLClassList()
        {
            TestClassList(new XmlTextSerializer());
        }

        [Test]
        public void TestBinaryClassList()
        {
            TestClassList(new BinarySerializer());
        }

        public void TestClassList(BaseSerializer serializer)
        {
            List<TestSerializeClass> items = GenerateList();
            var buffer = TestWrite(serializer, items);
#if DEBUG
            PrintBuffer(buffer);
#endif
            var newItems = TestRead<List<TestSerializeClass>>(serializer, buffer);
            Assert.AreEqual(items.Count, newItems.Count, "Deserialization Fail");
            Assert.AreEqual(items[1].GuidValue, newItems[1].GuidValue, "Deserialization Fail");
            Assert.AreEqual(items[1].IntValue, newItems[1].IntValue, "Deserialization Fail");
            Assert.AreEqual(items[1].NullableIntValue, newItems[1].NullableIntValue, "Deserialization Nullable Int Value Fail");
            Assert.AreEqual(items[1].DecimalValue, newItems[1].DecimalValue, "Deserialization Fail");
            Assert.AreEqual(items[1].StringValue, newItems[1].StringValue, "Deserialization Fail");
            Assert.AreEqual(items[1].ClassValue.StringValue, newItems[1].ClassValue.StringValue, "Deserialization Fail");
        }

        [Test]
        public void TestXMLSerializeableElementList()
        {
            List<TestSerializableElement> items = GenerateSEList();
            var serialiser = new XmlTextSerializer();
            var buffer = TestWrite(serialiser, items);
#if DEBUG
            PrintBuffer(buffer);
#endif
            var newItems = TestRead<List<TestSerializableElement>>(serialiser, buffer);
            Assert.AreEqual(items.Count, newItems.Count, "Deserialization Fail");
            Assert.AreEqual(items[1].ToSerialize.IntValue, newItems[1].ToSerialize.IntValue, "Deserialization Fail");
            Assert.AreEqual(items[1].ToSerialize.DecimalValue, newItems[1].ToSerialize.DecimalValue, "Deserialization Fail");
            Assert.AreEqual(items[1].ToSerialize.StringValue, newItems[1].ToSerialize.StringValue, "Deserialization Fail");
            Assert.AreEqual(items[1].ToSerialize.ClassValue.StringValue, newItems[1].ToSerialize.ClassValue.StringValue, "Deserialization Fail");
        }

        [Test]
        public void BinaryStreamHolder()
        {
            var streamData = new byte[] { 1, 0, 1, 0, 1, 0, 1, 0 };
            var streamHolder = new StreamHolder
            {
                FileName = "some_name.ext",
                Stream = new MemoryStream(streamData)
            };

            var serializer = new BinarySerializer();

            var buffer = serializer.Serialize(streamHolder);

            var newStreamHolder = serializer.Deserialize<StreamHolder>(buffer, null);

            Assert.AreEqual(streamHolder.FileName, newStreamHolder.FileName, "Deserialization Fail");

            var newStreamData = Helper.GetBytes(newStreamHolder.Stream, 8);

            Assert.IsTrue(streamData.AsSpan().SequenceEqual(newStreamData), "Deserialization Fail");
        }



        private void PrintBuffer(ArraySegment<byte> buffer)
        {
            var text = System.Text.Encoding.UTF8.GetString(buffer);
            using (var reader = new StringReader(text))
            {
                for (; ; )
                {
                    var line = reader.ReadLine();
                    if (line != null)
                        Console.WriteLine(line);
                    else
                        break;
                }
            }
        }

        public static List<TestSerializableElement> GenerateSEList()
        {
            var item = new List<TestSerializableElement>();
            item.AddRange(new[] {
                new TestSerializableElement{
                    ToSerialize =
                    new TestSerializeClass
                    {
                        IntValue = 5,
                        NullableIntValue = null,
                        DecimalValue = 5.5M,
                        StringValue = "bla bla",
                        ClassValue = new TestSerializeClass
                        {
                            IntValue = 25,
                            DecimalValue = 25.25M,
                            StringValue = "bla bla bla"
                        }
                    } },
                new TestSerializableElement{
                    ToSerialize =
                    new TestSerializeClass
                    {
                        IntValue = 10,
                        NullableIntValue = 1,
                        DecimalValue = 10.5M,
                        StringValue = "bla bla",
                        ClassValue = new TestSerializeClass
                        {
                            IntValue = 35,
                            DecimalValue = 35.25M,
                            StringValue = "bla bla bla"
                        }
                    } },
                new TestSerializableElement{
                    ToSerialize =
                    new TestSerializeClass
                    {
                        IntValue = 15,
                        DecimalValue = 15.5M,
                        StringValue = "bla bla",
                        ClassValue = new TestSerializeClass
                        {
                            IntValue = 45,
                            DecimalValue = 45.25M,
                            StringValue = "bla bla bla"
                        }
                    }
                }
            });
            return item;
        }

        public static TestSerializeClass GenerateClass()
        {
            return new TestSerializeClass
            {
                IntValue = 5,
                GuidValue = Guid.NewGuid(),
                NullableIntValue = null,
                DecimalValue = 5.5M,
                NullableDecimalValue = 1.1M,
                StringValue = "bla bla",
                ClassValue = new TestSerializeClass
                {
                    IntValue = 25,
                    DecimalValue = 25.25M,
                    StringValue = "bla bla bla"
                }
            };
        }

        public static List<TestSerializeClass> GenerateList(int cout = 5)
        {
            var items = new List<TestSerializeClass>();
            for (int i = 0; i < cout; i++)
            {
                items.Add(new TestSerializeClass
                {
                    IntValue = i,
                    GuidValue = Guid.NewGuid(),
                    NullableIntValue = null,
                    DecimalValue = i / 3.3M,
                    StringValue = "bla bla " + i,
                    ClassValue = new TestSerializeClass
                    {
                        IntValue = i * 5,
                        DecimalValue = i / 25.25M,
                        StringValue = "bla bla bla" + i
                    }
                });
            }
            return items;
        }

        public class TestSerializableElement : IXMLSerializable
        {
            public TestSerializeClass ToSerialize { get; set; }

            public void Deserialize(XmlInvokerReader reader)
            {
                reader.ReadObject(this, reader.Serializer.GetTypeInfo(typeof(TestSerializableElement)));
            }

            public void Serialize(XmlInvokerWriter writer)
            {
                writer.Write(ToSerialize, nameof(ToSerialize), false);
            }
        }
    }
}
