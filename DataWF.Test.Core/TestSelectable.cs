using DataWF;
using DataWF.Collections;
using DataWF.Collections.Query;
using DataWF.Invokers;
using NUnit.Framework;
using System.Linq;

namespace DataWF.Test.Core
{
    [TestFixture]
    public class TestSelectable
    {
        private SelectableList<TestClass> list;

        [SetUp]
        public void Setup()
        {
            list = new SelectableList<TestClass>();
            list.Indexes.Add(new ActionInvoker<TestClass, int>(nameof(TestClass.Field),
                        (ref item) => item.Field,
                        (ref item, value) => item.Field = value));
            list.AddRange(new[]{
                new TestClass(){ Name = "System", Field = 1, X = 0, Y = 1 },
                new TestClass(){ Name = "System.Collections", Field = 2, X = 1, Y = 2 },
                new TestClass(){ Name = "System.Diagnostics", Field = 3, X = 1, Y = 3 },
                new TestClass(){ Name = "System.Linq", Field = 4, X = 2, Y = 4 },
                new TestClass(){ Name = "System.Text", Field = 5, X = 2, Y = 5 },
                new TestClass(){ Name = "System.Threading", Field = 6, X = 3, Y = 6 },
                new TestClass(){ Name = "System.Threading.Task", Field = 7, X = 3, Y = 7 }
            });
        }

        [Test]
        public void TestIndex()
        {
            var resultOne = list.SelectOne(nameof(TestClass.Field), 3);
            Assert.AreEqual("System.Diagnostics", resultOne?.Name, "SelectOne by Index Fail");

            var result = list.Select(nameof(TestClass.Field), CompareOperator.Equal, 2);
            Assert.AreEqual(1, result.Count(), "Select by Index Field.Equal Fail");

            result = list.Select(nameof(TestClass.Field), CompareOperator.NotEqual, 2);
            Assert.AreEqual(6, result.Count(), "Select by Index Field.NotEqual Fail");

            result = list.Select(nameof(TestClass.Field), CompareOperator.Greater, 6);
            Assert.AreEqual(1, result.Count(), "Select by Index Field.Greater Fail");

            result = list.Select(nameof(TestClass.Field), CompareOperator.GreaterOrEqual, 6);
            Assert.AreEqual(2, result.Count(), "Select by Index Field.GreaterOrEqual Fail");

            result = list.Select(nameof(TestClass.Field), CompareOperator.Less, 6);
            Assert.AreEqual(5, result.Count(), "Select by Index Field.Less Fail");

            result = list.Select(nameof(TestClass.Field), CompareOperator.LessOrEqual, 6);
            Assert.AreEqual(6, result.Count(), "Select by Index Field.LessOrEqual Fail");

            result = list.Select(nameof(TestClass.Field), CompareOperator.In, new[] { 2, 5 });
            Assert.AreEqual(2, result.Count(), "Select by Index Field.In Fail");

            result = list.Select(nameof(TestClass.Field), CompareOperator.NotIn, new[] { 2, 5 });
            Assert.AreEqual(5, result.Count(), "Select by Index Field.NotIn Fail");
        }

        [Test]
        public void TestParameter()
        {
            var result = list.Select(nameof(TestClass.Name), CompareOperator.Equal, "System.Linq");
            Assert.AreEqual(1, result.Count(), "Select by Name.Equal Fail");

            result = list.Select(nameof(TestClass.Name), CompareOperator.NotEqual, "System.Linq");
            Assert.AreEqual(6, result.Count(), "Select by Name.NotEqual Fail");

            result = list.Select(nameof(TestClass.Name), CompareOperator.Like, "Threading");
            Assert.AreEqual(2, result.Count(), "Select by Name.Like Fail");

            result = list.Select(nameof(TestClass.Name), CompareOperator.NotLike, "Threading");
            Assert.AreEqual(5, result.Count(), "Select by Name.NotLike Fail");

            result = list.Select(nameof(TestClass.Name), CompareOperator.Greater, "System.Text");
            Assert.AreEqual(2, result.Count(), "Select by Name.Greater Fail");

            result = list.Select(nameof(TestClass.Name), CompareOperator.GreaterOrEqual, "System.Text");
            Assert.AreEqual(3, result.Count(), "Select by Name.GreaterOrEqual Fail");

            result = list.Select(nameof(TestClass.Name), CompareOperator.Less, "System.Text");
            Assert.AreEqual(4, result.Count(), "Select by Name.Less Fail");

            result = list.Select(nameof(TestClass.Name), CompareOperator.LessOrEqual, "System.Text");
            Assert.AreEqual(5, result.Count(), "Select by Name.LessOrEqual Fail");

            result = list.Select(nameof(TestClass.Name), CompareOperator.In, new[] { "System.Diagnostics", "System.Text" });
            Assert.AreEqual(2, result.Count(), "Select by Name.In Fail");

            result = list.Select(nameof(TestClass.Name), CompareOperator.NotIn, new[] { "System.Diagnostics", "System.Text" });
            Assert.AreEqual(5, result.Count(), "Select by Name.NotIn Fail");
        }

        [Test]
        public void TestQuery()
        {
            var query = new QQuery<TestClass>(new IQParameter<TestClass>[] {
                new QueryParameter<TestClass, string>()
                {
                    Invoker = TestClass.NameInvoker.Instance,
                    Comparer = CompareOperator.Like,
                    RightValue = "Threading"
                },
                new QueryParameter<TestClass, int>()
                {
                    Invoker = (IInvoker<TestClass, int>)InvokeProvider.Initialize<TestClass>(nameof(TestClass.Field)),
                    Comparer = CompareOperator.NotEqual,
                    RightValue = 6
                }
            });
            var result = list.Select(query);
            Assert.AreEqual(1, result.Count(), "Select by Query Name and Field Fail");

            query = new QQuery<TestClass>(new IQParameter<TestClass>[]{
                new QueryParameter<TestClass, string>()
                {
                    Invoker = (IInvoker<TestClass, string>)InvokeProvider.Initialize<TestClass>(nameof(TestClass.Name)),
                    Comparer = CompareOperator.Like,
                    RightValue = "Threading"
                },
                new QueryParameter<TestClass, int>()
                {
                    Logic = CompareOperator.AndNot,
                    Invoker = (IInvoker<TestClass, int>)InvokeProvider.Initialize<TestClass>(nameof(TestClass.Field)),
                    Comparer = CompareOperator.Equal,
                    RightValue = 6
                }
            });
            result = list.Select(query);
            Assert.AreEqual(1, result.Count(), "Select by Query Name and not Field Fail");

            query = new QQuery<TestClass>(new IQParameter<TestClass>[] {
                new QueryParameter<TestClass, string>()
                {
                    Invoker = (IInvoker<TestClass, string>)InvokeProvider.Initialize<TestClass>(nameof(TestClass.Name)),
                    Comparer = CompareOperator.Like,
                    RightValue = "Threading"
                },
                new QueryParameter<TestClass, int>()
                {
                    Logic = CompareOperator.Or,
                    Invoker = (IInvoker<TestClass, int>)InvokeProvider.Initialize<TestClass>(nameof(TestClass.Field)),
                    Comparer = CompareOperator.Less,
                    RightValue = 3
                }
            });
            result = list.Select(query);
            Assert.AreEqual(4, result.Count(), "Select by Query Name or Field Fail");
        }
    }
}
