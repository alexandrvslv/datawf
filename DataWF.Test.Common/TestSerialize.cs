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
    [TestFixture()]
    public partial class TestSerialize
    {
        public byte[] TestWrite<T>(BaseSerializer serializer, T list)
        {
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(stream, list);
                return stream.ToArray();
            }
        }

        public byte[] TestWrite<T>(XmlSerializer serializer, T list)
        {
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(stream, list);
                return stream.ToArray();
            }
        }

        public byte[] TestJsonWrite<T>(T list)
        {
            using (var stream = new MemoryStream())
            using (var jsonWriter = new Utf8JsonWriter(stream))
            {
                JsonSerializer.Serialize(jsonWriter, list);
                return stream.ToArray();
            }
        }

        public T TestRead<T>(BaseSerializer serializer, byte[] buffer)
        {
            using (var stream = new MemoryStream(buffer))
            {
                return serializer.Deserialize(stream, default(T));
            }
        }

        public T TestRead<T>(XmlSerializer serializer, byte[] buffer)
        {
            using (var stream = new MemoryStream(buffer))
            {
                return (T)serializer.Deserialize(stream);
            }
        }

        public async Task<T> TestJsonRead<T>(byte[] buffer)
        {
            using (var stream = new MemoryStream(buffer))
            {
                return await JsonSerializer.DeserializeAsync<T>(stream);
            }
        }

        [Test()]
        public void BenchmarkBinaryWrite()
        {
            var list = GenerateList();
            var serializer = new BinarySerializer(typeof(List<TestSerializeClass>));
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            for (var i = 0; i < 100000; i++)
                TestWrite(serializer, list);
            stopwatch.Stop();
            Console.WriteLine($"Benchmark Binary in:{stopwatch.ElapsedMilliseconds}");
        }

        [Test()]
        public void BenchmarkBinaryRead()
        {
            var list = GenerateList();
            var serializer = new BinarySerializer(typeof(List<TestSerializeClass>));
            var buffer = TestWrite(serializer, list);
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            for (var i = 0; i < 100000; i++)
                TestRead<List<TestSerializeClass>>(serializer, buffer);
            stopwatch.Stop();
            Console.WriteLine($"Benchmark Binary size:{buffer.Length} in:{stopwatch.ElapsedMilliseconds}");
        }

        [Test()]
        public void BenchmarkXMLTextWrite()
        {
            var list = GenerateList();
            var serializer = new XMLTextSerializer(typeof(List<TestSerializeClass>));
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            for (var i = 0; i < 100000; i++)
                TestWrite(serializer, list);
            stopwatch.Stop();
            Console.WriteLine($"Benchmark XML in:{stopwatch.ElapsedMilliseconds}");
        }

        [Test()]
        public void BenchmarkXMLTextRead()
        {
            var list = GenerateList();
            var serializer = new XMLTextSerializer(typeof(List<TestSerializeClass>));
            var buffer = TestWrite(serializer, list);
#if DEBUG
            PrintBuffer(buffer);
#endif
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            for (var i = 0; i < 100000; i++)
                TestRead<List<TestSerializeClass>>(serializer, buffer);
            stopwatch.Stop();
            Console.WriteLine($"Benchmark XML size:{buffer.Length} in:{stopwatch.ElapsedMilliseconds}");
        }

        [Test()]
        public void BenchmarkXMLSystemWrite()
        {
            var list = GenerateList();
            var serializer = new XmlSerializer(typeof(List<TestSerializeClass>));
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            for (var i = 0; i < 100000; i++)
                TestWrite(serializer, list);
            stopwatch.Stop();
            Console.WriteLine($"Benchmark XML Native in:{stopwatch.ElapsedMilliseconds}");
        }

        [Test()]
        public void BenchmarkXMLSystemRead()
        {
            var list = GenerateList();
            var serializer = new XmlSerializer(typeof(List<TestSerializeClass>));
            var buffer = TestWrite(serializer, list);
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            for (var i = 0; i < 100000; i++)
                TestRead<List<TestSerializeClass>>(serializer, buffer);
            stopwatch.Stop();
            Console.WriteLine($"Benchmark XML Native size:{buffer.Length} in:{stopwatch.ElapsedMilliseconds}");
        }

        [Test()]
        public void BenchmarkJsonSystemWrite()
        {
            var list = GenerateList();
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            for (var i = 0; i < 100000; i++)
                TestJsonWrite(list);
            stopwatch.Stop();
            Console.WriteLine($"Benchmark JSON Native in:{stopwatch.ElapsedMilliseconds}");
        }

        [Test()]
        public async Task BenchmarkJsonSystemRead()
        {
            var list = GenerateList();
            var buffer = TestJsonWrite(list);
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            for (var i = 0; i < 100000; i++)
                await TestJsonRead<List<TestSerializeClass>>(buffer);
            stopwatch.Stop();
            Console.WriteLine($"Benchmark JSON Native size:{buffer.Length} in:{stopwatch.ElapsedMilliseconds}");
        }

        [Test()]
        public void TestXMLGenericList()
        {
            TestGenericList(new XMLTextSerializer(typeof(List<string>)));
        }

        [Test()]
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
            Assert.AreEqual(3, newList.Count, "Deserialization Fail");
            Assert.AreEqual("one", newList[0], "Deserialization Fail");
        }

        [Test()]
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
            Assert.AreEqual(3, newList.Count, "Deserialization Fail");
            Assert.AreEqual("one", newList[0], "Deserialization Fail");
        }

        [Test()]
        public void TestXMLStringArray()
        {
            TestStringArray(new XMLTextSerializer(typeof(string[])));
        }

        [Test()]
        public void TestBinaryStringArray()
        {
            TestStringArray(new BinarySerializer(typeof(string[])));
        }

        public void TestStringArray(BaseSerializer serializer)
        {
            var list = new string[] { "one", "two", "three" };
            var buffer = TestWrite(serializer, list);
#if DEBUG
            PrintBuffer(buffer);
#endif
            var newList = TestRead<string[]>(serializer, buffer);
            Assert.AreEqual(3, newList.Length, "Deserialization Fail");
            Assert.AreEqual("one", newList[0], "Deserialization Fail");
        }

        [Test()]
        public void TestXMLArrayList()
        {
            TestArrayList(new XMLTextSerializer());
        }

        [Test()]
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
            Assert.AreEqual(4, newList.Count, "Deserialization Fail");
            Assert.AreEqual("one", newList[0], "Deserialization Fail");
            Assert.IsInstanceOf<TestSerializeClass>(newList[3], "Deserialization Fail");
            Assert.AreEqual("bla", ((TestSerializeClass)newList[3]).StringValue, "Deserialization Fail");
        }

        [Test()]
        public void TestXMLDictionary()
        {
            TestDictionary(new XMLTextSerializer());
        }

        [Test()]
        public void TestBinaryDictionary()
        {
            TestDictionary(new BinarySerializer());
        }

        public void TestDictionary(BaseSerializer serializer)
        {
            var dict = new Dictionary<string, int> { { "one", 1 }, { "two", 2 }, { "three", 3 } };

            var buffer = TestWrite(serializer, dict);
#if DEBUG
            PrintBuffer(buffer);
#endif
            var newDict = TestRead<Dictionary<string, int>>(serializer, buffer);
            Assert.AreEqual(3, newDict.Count, "Deserialization Fail");
            Assert.AreEqual(1, newDict["one"], "Deserialization Fail");
        }

        [Test()]
        public void TestXMLHashtable()
        {
            TestHashtable(new XMLTextSerializer());
        }

        [Test()]
        public void TestBinaryHashtable()
        {
            TestHashtable(new BinarySerializer());
        }

        public void TestHashtable(BaseSerializer serialiser)
        {
            var dict = new Hashtable { { "one", 1 }, { "two", 2 }, { "three", 3 } };
            var buffer = TestWrite(serialiser, dict);
#if DEBUG
            PrintBuffer(buffer);
#endif
            var newDict = TestRead<Hashtable>(serialiser, buffer);
            Assert.AreEqual(3, newDict.Count, "Deserialization Fail");
            Assert.AreEqual(1, newDict["one"], "Deserialization Fail");
        }

        [Test()]
        public void TestXMLClass()
        {
            TestClass(new XMLTextSerializer());
        }

        [Test()]
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

        [Test()]
        public void TestXMLClassList()
        {
            TestClassList(new XMLTextSerializer());
        }

        [Test()]
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
            var newItem = TestRead<List<TestSerializeClass>>(serializer, buffer);
            Assert.AreEqual(items.Count, newItem.Count, "Deserialization Fail");
            Assert.AreEqual(items[1].IntValue, newItem[1].IntValue, "Deserialization Fail");
            Assert.AreEqual(items[1].DecimalValue, newItem[1].DecimalValue, "Deserialization Fail");
            Assert.AreEqual(items[1].StringValue, newItem[1].StringValue, "Deserialization Fail");
            Assert.AreEqual(items[1].ClassValue.StringValue, newItem[1].ClassValue.StringValue, "Deserialization Fail");
        }

        [Test()]
        public void TestXMLSerializeableElementList()
        {
            List<TestSerializableElement> items = GenerateSEList();
            var serialiser = new XMLTextSerializer();
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

        private void PrintBuffer(byte[] buffer)
        {
            var text = System.Text.Encoding.UTF8.GetString(buffer);
            using (var reader = new StringReader(text))
            {
                for (; ; )
                {
                    var line = reader.ReadLine();
                    if (line != null)
                        Debug.WriteLine(line);
                    else
                        break;
                }
            }
        }

        private static List<TestSerializableElement> GenerateSEList()
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

        private static TestSerializeClass GenerateClass()
        {
            return new TestSerializeClass
            {
                IntValue = 5,
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

        private static List<TestSerializeClass> GenerateList()
        {
            var item = new List<TestSerializeClass>();
            item.AddRange(new[] {
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
                },
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
                },
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
                },
                new TestSerializeClass
                {
                    IntValue = 20,
                    DecimalValue = 20.5M,
                    StringValue = "bla bla",
                    ClassValue = new TestSerializeClass
                    {
                        IntValue = 55,
                        DecimalValue = 45.25M,
                        StringValue = "bla bla bla"
                    }
                },
                new TestSerializeClass
                {
                    IntValue = 25,
                    DecimalValue = 25.5M,
                    StringValue = "bla bla",
                    ClassValue = new TestSerializeClass
                    {
                        IntValue = 65,
                        DecimalValue = 45.25M,
                        StringValue = "bla bla bla"
                    }
                }
            });
            return item;
        }

        public class TestSerializableElement : ISerializableElement
        {
            public TestSerializeClass ToSerialize { get; set; }

            public void Deserialize(XmlInvokerReader reader)
            {
                ToSerialize = reader.Read(ToSerialize);
            }

            public void Serialize(XmlInvokerWriter writer)
            {
                writer.Write(ToSerialize);
            }
        }
    }
}
