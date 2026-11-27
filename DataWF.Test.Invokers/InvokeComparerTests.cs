using System;
using System.Collections.Generic;
using System.Linq;
using DataWF.Comparers;
using NUnit.Framework;

namespace DataWF.Test.Invokers;

[TestFixture]
public partial class InvokeComparerTests
{
    private TestClass[] items = null!;

    [SetUp]
    public void Setup()
    {
        items = Enumerable.Range(0, 180)
            .Select(index =>
            {
                var item = TestClass.GenerateTestClass(index);
                item.IntProperty = index % 17;
                item.StringProperty = $"group-{index % 11:D2}";
                item.UIntProperty = (uint)index;
                return item;
            })
            .ToArray();
    }

    [Test]
    public void SortByOneProperty()
    {
        var comparer = new InvokerComparer<TestClass, uint>(TestClass.UIntPropertyInvoker<TestClass>.Instance);

        var linqSorted = items.OrderBy(static item => item.UIntProperty)
            .ToArray();
        Array.Sort(items, comparer);
        Assert.That(linqSorted.SequenceEqual(items), Is.True);
    }

    [Test]
    public void GeneratedComparerSorts()
    {
        var toSort = (TestClass[])items.Clone();

        var comparer = TestClass.UIntPropertyInvoker<TestClass>.Instance.CreateComparer();
        Array.Sort(toSort, comparer);
        Assert.That(toSort.SequenceEqual(items.OrderBy(static item => item.UIntProperty)), Is.True);

        comparer.Direction = System.ComponentModel.ListSortDirection.Descending;
        Array.Sort(toSort, comparer);
        Assert.That(toSort.SequenceEqual(items.OrderByDescending(static item => item.UIntProperty)), Is.True);

    }

    [Test]
    public void CompareUsesAssignedComparer()
    {
        var comparer = new InvokerComparer<TestClass, uint>(
            invoker: TestClass.UIntPropertyInvoker<TestClass>.Instance,
            comparer: new InterfaceUniversalComparer<uint>(
                EqualityComparer<uint>.Default,
                Comparer<uint>.Create(static (x, y) => y.CompareTo(x))
            )
        );
        var sorted = (TestClass[])items.Clone();
        Array.Sort(sorted, comparer);
        Assert.That(comparer.Compare(items[0], items[1]), Is.GreaterThan(0));
    }

    [Test]
    public void SortBySeveralProperties()
    {
        var comparers = new InvokerComparerList<TestClass>(
        [
            new InvokerComparer<TestClass, int>(TestClass.IntPropertyInvoker<TestClass>.Instance),
            new InvokerComparer<TestClass, string>(TestClass.StringPropertyInvoker<TestClass>.Instance),
            new InvokerComparer<TestClass, uint>(TestClass.UIntPropertyInvoker<TestClass>.Instance)
        ]);

        var linqSorted = items
            .OrderBy(static item => item.IntProperty)
            .ThenBy(static item => item.StringProperty)
            .ThenBy(static item => item.UIntProperty)
            .ToArray();
        Array.Sort(items, comparers);
        Assert.That(linqSorted.SequenceEqual(items), Is.True);
    }
}
