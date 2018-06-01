using NUnit.Framework;
using System;
using System.Collections.Generic;
using DataWF.Common;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.ComponentModel;
using System.Xml.Serialization;

namespace DataWF.Test.Common
{
    [TestFixture()]
    public class TestSerialize
    {

        [Test()]
        public void BenchmarkWrite()
        {
            var list = new List<string> { "one", "two", "three" };
            var serializer = new Serializer(typeof(List<string>));
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            for (var i = 0; i < 100000; i++)
                TestWrite(serializer, list);
            stopwatch.Stop();
            Console.WriteLine($"Benchmark {stopwatch.ElapsedMilliseconds}");
        }

        [Test()]
        public void BenchmarkRead()
        {
            var list = new List<string> { "one", "two", "three" };
            var serializer = new Serializer(typeof(List<string>));
            var buffer = TestWrite(serializer, list);
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            for (var i = 0; i < 100000; i++)
                TestRead(serializer, buffer);
            stopwatch.Stop();
            Console.WriteLine($"Benchmark {stopwatch.ElapsedMilliseconds}");
        }

        [Test()]
        public void BenchmarkWriteNative()
        {
            var list = new List<string> { "one", "two", "three" };
            var serializer = new XmlSerializer(typeof(List<string>));
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            for (var i = 0; i < 100000; i++)
                TestWrite(serializer, list);
            stopwatch.Stop();
            Console.WriteLine($"Benchmark {stopwatch.ElapsedMilliseconds}");
        }

        [Test()]
        public void BenchmarkReadNative()
        {
            var list = new List<string> { "one", "two", "three" };
            var serializer = new XmlSerializer(typeof(List<string>));
            var buffer = TestWrite(serializer, list);
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            for (var i = 0; i < 100000; i++)
                TestRead(serializer, buffer);
            stopwatch.Stop();
            Console.WriteLine($"Benchmark {stopwatch.ElapsedMilliseconds}");
        }

        [Test()]
        public void TestGenericList()
        {
            var list = new List<string> { "one", "two", "three" };
            var serializer = new Serializer(typeof(List<string>));
            TestGenericList(serializer, list);
        }

        public byte[] TestWrite(Serializer serializer, object list)
        {
            var buffer = (byte[])null;
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(list, stream);
                buffer = stream.ToArray();
            }
            return buffer;
        }

        public byte[] TestWrite(XmlSerializer serializer, object list)
        {
            var buffer = (byte[])null;
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(stream, list);
                buffer = stream.ToArray();
            }
            return buffer;
        }

        public void TestRead(Serializer serializer, byte[] buffer)
        {
            using (var stream = new MemoryStream(buffer))
            {
                var newList = serializer.Deserialize(stream);
            }
        }

        public void TestRead(XmlSerializer serializer, byte[] buffer)
        {
            using (var stream = new MemoryStream(buffer))
            {
                var newList = serializer.Deserialize(stream);
            }
        }

        public void TestGenericList(Serializer serializer, object list, bool print = true)
        {
            var buffer = (byte[])null;
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(list, stream);
                buffer = stream.ToArray();
            }
            if (print)
            {
#if DEBUG
                PrintBuffer(buffer);
#endif
            }
            using (var stream = new MemoryStream(buffer))
            {
                var newList = serializer.Deserialize(stream);
                Assert.IsInstanceOf<List<string>>(newList, "Deserialization Fail");
                Assert.AreEqual(3, ((List<string>)newList).Count, "Deserialization Fail");
                Assert.AreEqual("one", ((List<string>)newList)[0], "Deserialization Fail");
            }
        }

        [Test()]
        public void TestNativeGenericList()
        {
            var list = new List<string> { "one", "two", "three" };
            var serializer = new XmlSerializer(typeof(List<string>));
            TestNativeGenericList(serializer, list);
        }

        public void TestNativeGenericList(XmlSerializer serializer, object list, bool print = true)
        {
            var buffer = (byte[])null;
            using (var stream = new MemoryStream())
            {
                serializer.Serialize(stream, list);
                buffer = stream.ToArray();
            }
            if (print)
            {
#if DEBUG
                PrintBuffer(buffer);
#endif
            }
            using (var stream = new MemoryStream(buffer))
            {
                var newList = serializer.Deserialize(stream);
                Assert.IsInstanceOf<List<string>>(newList, "Deserialization Fail");
                Assert.AreEqual(3, ((List<string>)newList).Count, "Deserialization Fail");
                Assert.AreEqual("one", ((List<string>)newList)[0], "Deserialization Fail");
            }
        }

        [Test()]
        public void TestArray()
        {
            var list = new[] { "one", "two", "three" };
            var buffer = (byte[])null;
            using (var stream = new MemoryStream())
            {
                Serialization.Serialize(list, stream);
                buffer = stream.ToArray();
            }
#if DEBUG
            PrintBuffer(buffer);
#endif
            using (var stream = new MemoryStream(buffer))
            {
                var newList = Serialization.Deserialize(stream);
                Assert.IsInstanceOf<string[]>(newList, "Deserialization Fail");
                Assert.AreEqual(3, ((string[])newList).Length, "Deserialization Fail");
                Assert.AreEqual("one", ((string[])newList)[0], "Deserialization Fail");
            }
        }

        [Test()]
        public void TestArrayList()
        {
            var list = new ArrayList { "one", 2, 3.0M, new TestSerializeClass { IntValue = 1, DecimalValue = 2.2M, StringValue = "bla" } };
            var buffer = (byte[])null;
            using (var stream = new MemoryStream())
            {
                Serialization.Serialize(list, stream);
                buffer = stream.ToArray();
            }
#if DEBUG
            PrintBuffer(buffer);
#endif
            using (var stream = new MemoryStream(buffer))
            {
                var newList = Serialization.Deserialize(stream);
                Assert.IsInstanceOf<ArrayList>(newList, "Deserialization Fail");
                Assert.AreEqual(4, ((ArrayList)newList).Count, "Deserialization Fail");
                Assert.AreEqual("one", ((ArrayList)newList)[0], "Deserialization Fail");
                Assert.IsInstanceOf<TestSerializeClass>(((ArrayList)newList)[3], "Deserialization Fail");
                Assert.AreEqual("bla", ((TestSerializeClass)((ArrayList)newList)[3]).StringValue, "Deserialization Fail");
            }
        }

        [Test()]
        public void TestDictionary()
        {
            var dict = new Dictionary<string, int> { { "one", 1 }, { "two", 2 }, { "three", 3 } };
            var buffer = (byte[])null;
            using (var stream = new MemoryStream())
            {
                Serialization.Serialize(dict, stream);
                buffer = stream.ToArray();
            }
#if DEBUG
            PrintBuffer(buffer);
#endif
            using (var stream = new MemoryStream(buffer))
            {
                var newDict = Serialization.Deserialize(stream);
                Assert.IsInstanceOf<Dictionary<string, int>>(newDict, "Deserialization Fail");
                Assert.AreEqual(3, ((Dictionary<string, int>)newDict).Count, "Deserialization Fail");
                Assert.AreEqual(1, ((Dictionary<string, int>)newDict)["one"], "Deserialization Fail");
            }
        }

        [Test()]
        public void TestHashtable()
        {
            var dict = new Hashtable { { "one", 1 }, { "two", 2 }, { "three", 3 } };
            var buffer = (byte[])null;
            using (var stream = new MemoryStream())
            {
                Serialization.Serialize(dict, stream);
                buffer = stream.ToArray();
            }
#if DEBUG
            PrintBuffer(buffer);
#endif
            using (var stream = new MemoryStream(buffer))
            {
                var newDict = Serialization.Deserialize(stream);
                Assert.IsInstanceOf<Hashtable>(newDict, "Deserialization Fail");
                Assert.AreEqual(3, ((Hashtable)newDict).Count, "Deserialization Fail");
                Assert.AreEqual(1, ((Hashtable)newDict)["one"], "Deserialization Fail");
            }
        }

        [Test()]
        public void TestClass()
        {
            var item = new TestSerializeClass
            {
                IntValue = 5,
                DecimalValue = 5.5M,
                StringValue = "bla bla",
                ClassValue = new TestSerializeClass
                {
                    IntValue = 25,
                    DecimalValue = 25.25M,
                    StringValue = "bla bla bla"
                }
            };
            var buffer = (byte[])null;
            using (var stream = new MemoryStream())
            {
                Serialization.Serialize(item, stream);
                buffer = stream.ToArray();
            }
#if DEBUG
            PrintBuffer(buffer);
#endif
            using (var stream = new MemoryStream(buffer))
            {
                var newItem = Serialization.Deserialize(stream);
                Assert.IsInstanceOf<TestSerializeClass>(newItem, "Deserialization Fail");
                Assert.AreEqual(item.IntValue, ((TestSerializeClass)newItem).IntValue, "Deserialization Fail");
                Assert.AreEqual(item.DecimalValue, ((TestSerializeClass)newItem).DecimalValue, "Deserialization Fail");
                Assert.AreEqual(item.StringValue, ((TestSerializeClass)newItem).StringValue, "Deserialization Fail");
                Assert.AreEqual(item.ClassValue.StringValue, ((TestSerializeClass)newItem).ClassValue.StringValue, "Deserialization Fail");

            }
        }

        [Test()]
        public void TestClassList()
        {
            var item = new List<TestSerializeClass>();
            item.AddRange(new[] {
                new TestSerializeClass
                {
                    IntValue = 5,
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
                }
            });
            var buffer = (byte[])null;
            using (var stream = new MemoryStream())
            {
                Serialization.Serialize(item, stream);
                buffer = stream.ToArray();
            }
#if DEBUG
            PrintBuffer(buffer);
#endif
            using (var stream = new MemoryStream(buffer))
            {
                var newItem = Serialization.Deserialize(stream);
                Assert.IsInstanceOf<List<TestSerializeClass>>(newItem, "Deserialization Fail");
                Assert.AreEqual(item.Count, ((List<TestSerializeClass>)newItem).Count, "Deserialization Fail");
                Assert.AreEqual(item[1].IntValue, ((List<TestSerializeClass>)newItem)[1].IntValue, "Deserialization Fail");
                Assert.AreEqual(item[1].DecimalValue, ((List<TestSerializeClass>)newItem)[1].DecimalValue, "Deserialization Fail");
                Assert.AreEqual(item[1].StringValue, ((List<TestSerializeClass>)newItem)[1].StringValue, "Deserialization Fail");
                Assert.AreEqual(item[1].ClassValue.StringValue, ((List<TestSerializeClass>)newItem)[1].ClassValue.StringValue, "Deserialization Fail");
            }
        }

        [Test()]
        public void TestSerializeableElementList()
        {
            var item = new List<TestSerializableElement>();
            item.AddRange(new[] {
                new TestSerializableElement{
                    ToSerialize =
                    new TestSerializeClass
                    {
                        IntValue = 5,
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
            var buffer = (byte[])null;
            using (var stream = new MemoryStream())
            {
                Serialization.Serialize(item, stream);
                buffer = stream.ToArray();
            }
#if DEBUG
            PrintBuffer(buffer);
#endif
            using (var stream = new MemoryStream(buffer))
            {
                var newItem = Serialization.Deserialize(stream);
                Assert.IsInstanceOf<List<TestSerializableElement>>(newItem, "Deserialization Fail");
                Assert.AreEqual(item.Count, ((List<TestSerializableElement>)newItem).Count, "Deserialization Fail");
                Assert.AreEqual(item[1].ToSerialize.IntValue, ((List<TestSerializableElement>)newItem)[1].ToSerialize.IntValue, "Deserialization Fail");
                Assert.AreEqual(item[1].ToSerialize.DecimalValue, ((List<TestSerializableElement>)newItem)[1].ToSerialize.DecimalValue, "Deserialization Fail");
                Assert.AreEqual(item[1].ToSerialize.StringValue, ((List<TestSerializableElement>)newItem)[1].ToSerialize.StringValue, "Deserialization Fail");
                Assert.AreEqual(item[1].ToSerialize.ClassValue.StringValue, ((List<TestSerializableElement>)newItem)[1].ToSerialize.ClassValue.StringValue, "Deserialization Fail");
            }
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

        public class TestSerializeClass
        {
            public int IntValue { get; set; }
            [DefaultValue(1)]
            public int DefaultValue { get; set; } = 1;
            public decimal DecimalValue { get; set; }
            public string StringValue { get; set; }
            public TestSerializeClass ClassValue { get; set; }
        }

        public class TestSerializableElement : ISerializableElement
        {
            public TestSerializeClass ToSerialize { get; set; }

            public void Deserialize(ISerializeReader reader)
            {
                ToSerialize = reader.Read(ToSerialize) as TestSerializeClass;                
            }

            public void Serialize(ISerializeWriter writer)
            {
                writer.Write(ToSerialize);
            }
        }
    }
}
