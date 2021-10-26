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
        private const int count = 1000000;
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
                Array = new int[] { 1, value, 3 },
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
                    Array = new int[] { 1, value, 3 },
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
            BenchmarkResult.Add("Ctors", "Direct", watch.Elapsed);
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
            BenchmarkResult.Add("Ctors", "Expression", watch.Elapsed);
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
        public void PropertyExpression()
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
            TestProperty(new TestPropertyInvoker());
        }

        [Test()]
        public void PropertyReflection()
        {
            var invoker = new ReflectionInvoker(typeof(TestClass), "X");
            var x = (int)invoker.GetValue(testReference);
            Assert.AreEqual(value, x, "Reflection Fail Get Operation");
            invoker.SetValue(testReference, x + 1);
            Assert.AreEqual(value + 1, testReference.X, "Reflection Fail Set Operation");
        }

        [Test()]
        public void BenchmarkPropertyExact()
        {
            Benchmark("Property", "Direct", new TestPropertyInvoker());
        }

        [Test()]
        public void BenchmarkPropertyAction()
        {
            Benchmark("Property", "Action", new ActionInvoker<TestClass, int>("X",
                                                               (item) => item.X,
                                                               (item, value) => item.X = value));
        }

        [Test()]
        public void BenchmarkPropertyExpression()
        {
            Benchmark("Property", "Expression", new PropertyInvoker<TestClass, int>("X"));
        }

        [Test()]
        public void BenchmarkBoxingPropertyExact()
        {
            BenchmarkBoxing("Property", "Direct", new TestPropertyInvoker());
        }

        [Test()]
        public void BenchmarkBoxingPropertyAction()
        {
            BenchmarkBoxing("Property", "Action", new ActionInvoker<TestClass, int>("X",
                                                                                 (item) => item.X,
                                                                                 (item, value) => item.X = value));
        }

        [Test()]
        public void BenchmarkBoxingPropertyExpression()
        {
            BenchmarkBoxing("Property", "Expression", new PropertyInvoker<TestClass, int>("X"));
        }

        [Test()]
        public void BenchmarkPropertyReflection()
        {
            BenchmarkBoxing("Property", "Reflection", new ReflectionInvoker(typeof(TestClass), "X"));
        }
        #endregion

        #region Property Index
        [Test()]
        public void PropertyIndexExpression()
        {
            TestProperty(IndexPropertyInvoker<TestClass, int, int>.Create("Item[1]"));
        }

        [Test()]
        public void PropertyIndexAction()
        {
            TestProperty(new ActionIndexInvoker<TestClass, int, int>("Item[1]",
                                                               (item, index) => item[index],
                                                               (item, index, value) => item[index] = value)
            { Index = 1 });
        }

        [Test()]
        public void PropertyIndexExact()
        {
            TestProperty(new TestIndexInvoker(1));
        }

        [Test()]
        public void BenchmarkIndexPropertyExact()
        {
            Benchmark("Index", "Direct", new TestIndexInvoker(1));
        }

        [Test()]
        public void BenchmarkIndexPropertyAction()
        {
            Benchmark("Index", "Action", new ActionIndexInvoker<TestClass, int, int>("Item[1]",
                                                               (item, index) => item[index],
                                                               (item, index, value) => item[index] = value)
            { Index = 1 });
        }

        [Test()]
        public void BenchmarkIndexPropertyExpression()
        {
            Benchmark("Index", "Expression", IndexPropertyInvoker<TestClass, int, int>.Create("Item[1]"));
        }


        [Test()]
        public void BenchmarkIndexInlinePropertyExact()
        {
            Benchmark("Inline Index", "Direct", new TestInlineIndexInvoker());
        }

        [Test()]
        public void BenchmarkIndexInlinePropertyAction()
        {
            Benchmark("Inline Index", "Action", new ActionInvoker<TestClass, int>("Group.Item[1]",
                                                               (item) => item.Group?[1] ?? 0,
                                                               (item, value) => item.Group[1] = value));
        }

        [Test()]
        public void BenchmarkIndexInlinePropertyExpression()
        {
            Benchmark("Inline Index", "Expression", new ComplexInvoker<TestClass, int>("Group.Item[1]"));
        }

        #endregion

        #region Inline Property
        [Test()]
        public void InlinePropertyExpression()
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
            TestInlineProperty(new TestInlinePropertyInvoker());
        }

        [Test()]
        public void InlinePropertyReflection()
        {
            var invoker = new ReflectionInvoker(typeof(TestClass), "Group.Struct.Width");
            var width = (int)invoker.GetValue(testReference);
            Assert.AreEqual(value, width, "Reflection Fail Get Operation");
            invoker.SetValue(testReference, width + 1);
            Assert.AreEqual(value + 1, testReference.Group.Struct.Width, "Reflection Fail Set Operation");
        }

        private void TestInlineProperty(IInvoker<TestClass, int> invoker, [CallerMemberName] string name = null)
        {
            var width = invoker.GetValue(testReference);
            Assert.AreEqual(value, width, $"{name} Fail Get Operation");
            invoker.SetValue(testReference, width + 1);
            Assert.AreEqual(value + 1, testReference.Group.Struct.Width, $"{name} Fail Set Operation");
        }

        [Test()]
        public void BenchmarkInlinePropertyExact()
        {
            Benchmark("Inline Property", "Direct", new TestInlinePropertyInvoker());
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
        public void BenchmarkInlinePropertyExpression()
        {
            Benchmark("Inline Property", "Emit", new ComplexInvoker<TestClass, int>("Group.Struct.Width"));
        }

        [Test()]
        public void BenchmarkBoxingInlinePropertyExact()
        {
            BenchmarkBoxing("Inline Property", "Direct", new TestInlinePropertyInvoker());
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
        public void BenchmarkBoxingInlinePropertyExpression()
        {
            BenchmarkBoxing("Inline Property", "Expression", new ComplexInvoker<TestClass, int>("Group.Struct.Width"));
        }

        [Test()]
        public void BenchmarkInlineProeprtyReflection()
        {
            BenchmarkBoxing("Inline Property", "Reflection", new ReflectionInvoker(typeof(TestClass), "Group.Struct.Width"));
        }

        #endregion

        #region Field
        [Test()]
        public void FieldExpression()
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
            TestField(new TestFieldInvoker());
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

        private void TestField(IInvoker<TestClass, int> invoker, [CallerMemberName] string name = null)
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
            Benchmark("Field", "Direct", new TestFieldInvoker());
        }

        [Test()]
        public void BenchmarkFieldAction()
        {
            Benchmark("Field", "Action", new ActionInvoker<TestClass, int>("Field",
                                                                     (item) => item.Field,
                                                                     (item, value) => item.Field = value));
        }

        [Test()]
        public void BenchmarkFieldExpression()
        {
            Benchmark("Field", "Expression", new FieldInvoker<TestClass, int>("Field"));
        }

        [Test()]
        public void BenchmarkBoxingFieldExact()
        {
            BenchmarkBoxing("Field", "Direct", new TestFieldInvoker());
        }

        [Test()]
        public void BenchmarkBoxingFieldAction()
        {
            BenchmarkBoxing("Field", "Action", new ActionInvoker<TestClass, int>("Field",
                                                                              (item) => item.Field,
                                                                              (item, value) => item.Field = value));
        }

        [Test()]
        public void BenchmarkBoxingFieldExpression()
        {
            BenchmarkBoxing("Field", "Expression", new FieldInvoker<TestClass, int>("Field"));
        }

        [Test()]
        public void BenchmarkFieldReflection()
        {
            BenchmarkBoxing("Field", "Reflection", new ReflectionInvoker(typeof(TestClass), "Field"));
        }
        #endregion

        #region Inline Field
        [Test()]
        public void InlineFieldExpression()
        {
            TestInlineField(new ComplexInvoker<TestClass, int>("Group.Struct.Field"));
        }

        [Test()]
        public void InlineFieldAction()
        {
            TestInlineField(new ActionInvoker<TestClass, int>("Group.Struct.Field",
                                                        (item) => item.Group?.Struct.Field ?? 0,
                                                        (item, value) =>
                                                        {
                                                            var group = item.Group;
                                                            if (group != null)
                                                            {
                                                                var temp = group.Struct;
                                                                temp.Field = value;
                                                                group.Struct = temp;
                                                            }
                                                        }));
        }

        [Test()]
        public void InlineFieldExact()
        {
            TestInlineField(new TestInlineFieldInvoker());
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

        private void TestInlineField(IInvoker<TestClass, int> invoker, [CallerMemberName] string name = null)
        {
            var x = invoker.GetValue(testReference);
            Assert.AreEqual(value, x, $"{name} Fail Get Operation");
            x++;
            invoker.SetValue(testReference, x);
            Assert.AreEqual(value + 1, testReference.Group.Struct.Field, $"{name} Fail Set Operation");
        }

        [Test()]
        public void BenchmarkInlineFieldExact()
        {
            Benchmark("Inline Field", "Direct", new TestInlineFieldInvoker());
        }

        [Test()]
        public void BenchmarkInlineFieldAction()
        {
            Benchmark("Inline Field", "Action", new ActionInvoker<TestClass, int>("Struct.Field",
                                                        (item) => item.Struct.Field,
                                                        (item, value) => { var temp = item.Struct; temp.Field = value; item.Struct = temp; }));
        }

        [Test()]
        public void BenchmarkInlineFieldExpression()
        {
            Benchmark("Inline Field", "Expression", new ComplexInvoker<TestClass, int>("Struct.Field"));
        }

        [Test()]
        public void BenchmarkBoxingInlineFieldExact()
        {
            BenchmarkBoxing("Inline Field", "Direct", new TestInlineFieldInvoker());
        }

        [Test()]
        public void BenchmarkBoxingInlineFieldAction()
        {
            BenchmarkBoxing("Inline Field", "Action", new ActionInvoker<TestClass, int>("Field",
                                                        (item) => item.Struct.Field,
                                                        (item, value) => { var temp = item.Struct; temp.Field = value; item.Struct = temp; }));
        }

        [Test()]
        public void BenchmarkBoxingInlineFieldExpression()
        {
            BenchmarkBoxing("Inline Field", "Expression", new ComplexInvoker<TestClass, int>("Struct.Field"));
        }

        [Test()]
        public void BenchmarkInlineFieldReflection()
        {
            BenchmarkBoxing("Inline Field", "Reflection", new ReflectionInvoker(typeof(TestClass), "Struct.Field"));
        }
        #endregion

        private void TestProperty(IInvoker<TestClass, int> invoker, [CallerMemberName] string name = null)
        {
            var x = invoker.GetValue(testReference);
            Assert.AreEqual(value, x, $"{name} Fail Get Operation");
            invoker.SetValue(testReference, x + 1);
            Assert.AreEqual(value + 1, invoker.GetValue(testReference), $"{name} Fail Set Operation");
        }


        public void Benchmark(string category, string name, IInvoker<TestClass, int> invoker)
        {
            Stopwatch watch = new Stopwatch();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < count; i++)
            {
                var width = invoker.GetValue(testReference);
                width += 1;
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
                var width = (int)invoker.GetValue(testReference);
                width += 1;
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
