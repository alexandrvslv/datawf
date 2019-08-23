using DataWF.Common;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace DataWF.Test.Common
{
    [TestFixture()]
    public class TestInvoker
    {
        private const int value = 3;
        private const int count = 200000;
        private TestClass testReference;

        [SetUp]
        public void Setup()
        {
            testReference = new TestClass()
            {
                Field = value,
                Name = "Name",
                X = value,
                Y = value,
                Struct = new TestStruct()
                {
                    Field = value,
                    Width = value,
                    Height = value
                },
                Group = new TestClass()
                {
                    Name = "Goup",
                    X = value,
                    Y = value,
                    Struct = new TestStruct()
                    {
                        Field = value,
                        Width = value,
                        Height = value
                    }
                }
            };
        }

        #region Ctor
        [Test()]
        public void BenchmarkCtorExact()
        {
            var watch = new Stopwatch();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < count; i++)
            {
                testReference = new TestClass();
            }
            watch.Stop();
            BenchmarkResult.Add("Ctors", "Exact", watch.Elapsed);
        }

        [Test()]
        public void BenchmarkCtorEmit()
        {
            var ctor = new EmitConstructor(typeof(TestClass));
            var watch = new Stopwatch();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < count; i++)
            {
                testReference = (TestClass)ctor.Create();
            }
            watch.Stop();
            BenchmarkResult.Add("Ctors", "Emit", watch.Elapsed);
        }

        [Test()]
        public void BenchmarkCtorReflection()
        {
            var ctor = typeof(TestClass).GetConstructor(Type.EmptyTypes);
            var watch = new Stopwatch();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < count; i++)
            {
                testReference = (TestClass)ctor.Invoke(null);
            }
            watch.Stop();
            BenchmarkResult.Add("Ctors", "Reflection", watch.Elapsed);
        }

        [Test()]
        public void BenchmarkCtorActivator()
        {
            var watch = new Stopwatch();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < count; i++)
            {
                testReference = Activator.CreateInstance<TestClass>();
            }
            watch.Stop();
            BenchmarkResult.Add("Ctors", "Activator", watch.Elapsed);
        }

        #endregion

        #region Property
        [Test()]
        public void PropertyEmit()
        {
            TestProperty(new PropertyInvoker<TestClass, int>("X"));
        }

        [Test()]
        public void PropertyAction()
        {
            TestProperty(new ActionInvoker<TestClass, int>("X",
                                                        (item) => item.X,
                                                        (item, value) => item.X = value));
        }

        [Test()]
        public void PropertyExact()
        {
            TestProperty(new TestExactPropertyInvoker());
        }

        [Test()]
        public void PropertyReflection()
        {
            var invoker = new ReflectionInvoker(typeof(TestClass), "X");
            var x = (int)invoker.GetValue(testReference);
            Assert.AreEqual(value, x, "Reflection Fail Get Operation");
            x++;
            invoker.SetValue(testReference, x);
            Assert.AreEqual(value + 1, testReference.X, "Reflection Fail Set Operation");
        }

        private void TestProperty(IInvoker<TestClass, int> invoker, [CallerMemberName]string name = null)
        {
            var x = invoker.GetValue(testReference);
            Assert.AreEqual(value, x, $"{name} Fail Get Operation");
            x++;
            invoker.SetValue(testReference, x);
            Assert.AreEqual(value + 1, testReference.X, $"{name} Fail Set Operation");
        }

        [Test()]
        public void BenchmarkPropertyExact()
        {
            Benchmark("Property", "Exact", new TestExactPropertyInvoker());
        }

        [Test()]
        public void BenchmarkPropertyAction()
        {
            Benchmark("Property", "Action", new ActionInvoker<TestClass, int>("X",
                                                               (item) => item.X,
                                                               (item, value) => item.X = value));
        }

        [Test()]
        public void BenchmarkPropertyEmit()
        {
            Benchmark("Property", "Emit", new PropertyInvoker<TestClass, int>("X"));
        }

        [Test()]
        public void BenchmarkBoxingPropertyExact()
        {
            BenchmarkBoxing("Property", "Exact", new TestExactPropertyInvoker());
        }

        [Test()]
        public void BenchmarkBoxingPropertyAction()
        {
            BenchmarkBoxing("Property", "Action", new ActionInvoker<TestClass, int>("X",
                                                                                 (item) => item.X,
                                                                                 (item, value) => item.X = value));
        }

        [Test()]
        public void BenchmarkBoxingPropertyEmit()
        {
            BenchmarkBoxing("Property", "Emit", new ComplexInvoker<TestClass, int>("X"));
        }

        [Test()]
        public void BenchmarkPropertyReflection()
        {
            BenchmarkBoxing("Property", "Reflection", new ReflectionInvoker(typeof(TestClass), "X"));
        }
        #endregion

        #region Inline Property
        [Test()]
        public void InlinePropertyEmit()
        {
            TestInlineProperty(new ComplexInvoker<TestClass, int>("Group.Struct.Width"));
        }

        [Test()]
        public void InlinePropertyAction()
        {
            TestInlineProperty(new ActionInvoker<TestClass, int>("Group.Struct.Width",
                                                              (item) => item.Group?.Struct.Width ?? 0,
                                                              (item, value) =>
                                                              {
                                                                  var temp = item.Group; if (temp != null)
                                                                  {
                                                                      var temp2 = temp.Struct;
                                                                      temp2.Width = value;
                                                                      temp.Struct = temp2;
                                                                  }
                                                              }));
        }

        [Test()]
        public void InlinePropertyExact()
        {
            TestInlineProperty(new TestExactInlinePropertyInvoker());
        }

        [Test()]
        public void InlinePropertyReflection()
        {
            var invoker = new ReflectionInvoker(typeof(TestClass), "Group.Struct.Width");
            var width = (int)invoker.GetValue(testReference);
            Assert.AreEqual(value, width, "Reflection Fail Get Operation");
            width++;
            invoker.SetValue(testReference, width);
            Assert.AreEqual(value + 1, testReference.Group.Struct.Width, "Reflection Fail Set Operation");
        }

        private void TestInlineProperty(IInvoker<TestClass, int> invoker, [CallerMemberName]string name = null)
        {
            var width = invoker.GetValue(testReference);
            Assert.AreEqual(value, width, $"{name} Fail Get Operation");
            width++;
            invoker.SetValue(testReference, width);
            Assert.AreEqual(value + 1, testReference.Group.Struct.Width, $"{name} Fail Set Operation");
        }

        [Test()]
        public void BenchmarkInlinePropertyExact()
        {
            Benchmark("Inline Property", "Exact", new TestExactInlinePropertyInvoker());
        }

        [Test()]
        public void BenchmarkInlinePropertyAction()
        {
            Benchmark("Inline Property", "Action", new ActionInvoker<TestClass, int>("Group.Struct.Width",
                                                              (item) => item.Group?.Struct.Width ?? 0,
                                                              (item, value) =>
                                                              {
                                                                  var temp = item.Group; if (temp != null)
                                                                  {
                                                                      var temp2 = temp.Struct;
                                                                      temp2.Width = value;
                                                                      temp.Struct = temp2;
                                                                  }
                                                              }));
        }

        [Test()]
        public void BenchmarkInlinePropertyEmit()
        {
            Benchmark("Inline Property", "Emit", new ComplexInvoker<TestClass, int>("Group.Struct.Width"));
        }

        [Test()]
        public void BenchmarkBoxingInlinePropertyExact()
        {
            BenchmarkBoxing("Inline Property", "Exact", new TestExactInlinePropertyInvoker());
        }

        [Test()]
        public void BenchmarkBoxingInlinePropertyAction()
        {
            BenchmarkBoxing("Inline Property", "Action", new ActionInvoker<TestClass, int>("Group.Struct.Width",
                                                              (item) => item.Group?.Struct.Width ?? 0,
                                                              (item, value) =>
                                                              {
                                                                  var temp = item.Group; if (temp != null)
                                                                  {
                                                                      var temp2 = temp.Struct;
                                                                      temp2.Width = value;
                                                                      temp.Struct = temp2;
                                                                  }
                                                              }));
        }

        [Test()]
        public void BenchmarkBoxingInlinePropertyEmit()
        {
            BenchmarkBoxing("Inline Property", "Emit", new ComplexInvoker<TestClass, int>("Group.Struct.Width"));
        }

        [Test()]
        public void BenchmarkInlineProeprtyReflection()
        {
            BenchmarkBoxing("Inline Property", "Reflection", new ReflectionInvoker(typeof(TestClass), "Group.Struct.Width"));
        }

        #endregion

        #region Field
        [Test()]
        public void FieldEmit()
        {
            TestField(new FieldInvoker<TestClass, int>("Field"));
        }

        [Test()]
        public void FieldAction()
        {
            TestField(new ActionInvoker<TestClass, int>("Field",
                                                  (item) => item.Field,
                                                  (item, value) => item.Field = value));
        }

        [Test()]
        public void FieldExact()
        {
            TestField(new TestExactFieldInvoker());
        }

        [Test()]
        public void FieldReflection()
        {
            var invoker = new ReflectionInvoker(typeof(TestClass), "Field");
            var x = (int)invoker.GetValue(testReference);
            Assert.AreEqual(value, x, "Reflection Fail Get Operation");
            x++;
            invoker.SetValue(testReference, x);
            Assert.AreEqual(value + 1, testReference.Field, "Reflection Fail Set Operation");
        }

        private void TestField(IInvoker<TestClass, int> invoker, [CallerMemberName]string name = null)
        {
            var x = invoker.GetValue(testReference);
            Assert.AreEqual(value, x, $"{name} Fail Get Operation");
            x++;
            invoker.SetValue(testReference, x);
            Assert.AreEqual(value + 1, testReference.Field, $"{name} Fail Set Operation");
        }

        [Test()]
        public void BenchmarkFieldExact()
        {
            Benchmark("Field", "Exact", new TestExactFieldInvoker());
        }

        [Test()]
        public void BenchmarkFieldAction()
        {
            Benchmark("Field", "Action", new ActionInvoker<TestClass, int>("Field",
                                                                     (item) => item.Field,
                                                                     (item, value) => item.Field = value));
        }

        [Test()]
        public void BenchmarkFieldEmit()
        {
            Benchmark("Field", "Emit", new FieldInvoker<TestClass, int>("Field"));
        }

        [Test()]
        public void BenchmarkBoxingFieldExact()
        {
            BenchmarkBoxing("Field", "Exact", new TestExactFieldInvoker());
        }

        [Test()]
        public void BenchmarkBoxingFieldAction()
        {
            BenchmarkBoxing("Field", "Action", new ActionInvoker<TestClass, int>("Field",
                                                                              (item) => item.Field,
                                                                              (item, value) => item.Field = value));
        }

        [Test()]
        public void BenchmarkBoxingFieldEmit()
        {
            BenchmarkBoxing("Field", "Emit", new FieldInvoker<TestClass, int>("Field"));
        }

        [Test()]
        public void BenchmarkFieldReflection()
        {
            BenchmarkBoxing("Field", "Reflection", new ReflectionInvoker(typeof(TestClass), "Field"));
        }
        #endregion

        #region Inline Field
        [Test()]
        public void InlineFieldEmit()
        {
            TestInlineField(new ComplexInvoker<TestClass, int>("Struct.Field"));
        }

        [Test()]
        public void InlineFieldAction()
        {
            TestInlineField(new ActionInvoker<TestClass, int>("Struct.Field",
                                                        (item) => item.Struct.Field,
                                                        (item, value) => { var temp = item.Struct; temp.Field = value; item.Struct = temp; }));
        }

        [Test()]
        public void InlineFieldExact()
        {
            TestInlineField(new TestExactInlineFieldInvoker());
        }

        [Test()]
        public void InlineFieldReflection()
        {
            var invoker = new ReflectionInvoker(typeof(TestClass), "Struct.Field");
            var x = (int)invoker.GetValue(testReference);
            Assert.AreEqual(value, x, "Reflection Fail Get Operation");
            x++;
            invoker.SetValue(testReference, x);
            Assert.AreEqual(value + 1, testReference.Struct.Field, "Reflection Fail Set Operation");
        }

        private void TestInlineField(IInvoker<TestClass, int> invoker, [CallerMemberName]string name = null)
        {
            var x = invoker.GetValue(testReference);
            Assert.AreEqual(value, x, $"{name} Fail Get Operation");
            x++;
            invoker.SetValue(testReference, x);
            Assert.AreEqual(value + 1, testReference.Struct.Field, $"{name} Fail Set Operation");
        }

        [Test()]
        public void BenchmarkInlineFieldExact()
        {
            Benchmark("Inline Field", "Exact", new TestExactInlineFieldInvoker());
        }

        [Test()]
        public void BenchmarkInlineFieldAction()
        {
            Benchmark("Inline Field", "Action", new ActionInvoker<TestClass, int>("Struct.Field",
                                                        (item) => item.Struct.Field,
                                                        (item, value) => { var temp = item.Struct; temp.Field = value; item.Struct = temp; }));
        }

        [Test()]
        public void BenchmarkInlineFieldEmit()
        {
            Benchmark("Inline Field", "Emit", new ComplexInvoker<TestClass, int>("Struct.Field"));
        }

        [Test()]
        public void BenchmarkBoxingInlineFieldExact()
        {
            BenchmarkBoxing("Inline Field", "Exact", new TestExactInlineFieldInvoker());
        }

        [Test()]
        public void BenchmarkBoxingInlineFieldAction()
        {
            BenchmarkBoxing("Inline Field", "Action", new ActionInvoker<TestClass, int>("Field",
                                                        (item) => item.Struct.Field,
                                                        (item, value) => { var temp = item.Struct; temp.Field = value; item.Struct = temp; }));
        }

        [Test()]
        public void BenchmarkBoxingInlineFieldEmit()
        {
            BenchmarkBoxing("Inline Field", "Emit", new ComplexInvoker<TestClass, int>("Struct.Field"));
        }

        [Test()]
        public void BenchmarkInlineFieldReflection()
        {
            BenchmarkBoxing("Inline Field", "Reflection", new ReflectionInvoker(typeof(TestClass), "Struct.Field"));
        }
        #endregion

        public void Benchmark<T, V>(string category, string name, IInvoker<T, V> invoker)
        {
            Stopwatch watch = new Stopwatch();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < count; i++)
            {
                var width = invoker.GetValue(testReference);
                width = default(V);
                invoker.SetValue(testReference, width);
            }
            watch.Stop();
            BenchmarkResult.Add(category, name, watch.Elapsed);
        }

        public void BenchmarkBoxing(string category, string name, IInvoker invoker)
        {
            Stopwatch watch = new Stopwatch();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < count; i++)
            {
                var width = invoker.GetValue(testReference);
                width = 0;
                invoker.SetValue(testReference, width);
            }
            watch.Stop();
            BenchmarkResult.Add($"{category}(Boxing)", name, watch.Elapsed);
        }
    }

    public class BenchmarkResult
    {
        private static List<BenchmarkResult> Results = new List<BenchmarkResult>();
        public static void Add(string category, string name, TimeSpan elapsed)
        {
            Console.WriteLine($"Benchmark {category}-{name}: {elapsed}");
            Results.Add(new BenchmarkResult() { Category = category, Name = name, Elapsed = elapsed });
        }
        public string Category { get; set; }
        public string Name { get; set; }
        public TimeSpan Elapsed { get; set; }
    }
}
