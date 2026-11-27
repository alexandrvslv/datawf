using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using DataWF.Comparers;
using DataWF.Test;

namespace DataWF.Benchmark.Invokers.Benchmarks;

[MemoryDiagnoser]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
public class SortingByProperties
{
    private TestClass[] items = null!;
    private IInvokerComparer<TestClass, uint> comparer = null!;
    private InvokerComparerList<TestClass> comparers = null!;

    [Params(1024)]//128, 2048)]
    public int ItemCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        items = Enumerable.Range(0, ItemCount)
            .Select(index =>
            {
                var item = TestClass.GenerateTestClass(index);
                item.IntProperty = index % 17;
                item.StringProperty = $"group-{index % 11:D2}";
                item.UIntProperty = (uint)index;
                return item;
            })
            .ToArray();
        comparer = TestClass.UIntPropertyInvoker<TestClass>.Instance.CreateComparer();
        comparers =
        [
            (UniversalComparer<TestClass>)TestClass.IntPropertyInvoker<TestClass>.Instance.CreateComparer(),
            (UniversalComparer<TestClass>)TestClass.StringPropertyInvoker<TestClass>.Instance.CreateComparer(),
            (UniversalComparer<TestClass>)comparer
        ];

        if (!LinqOrderBy().SequenceEqual(InvokerComparer()))
            throw new InvalidOperationException("The sorting implementations produced different results.");

        if (!LinqOrderByThenBy().SequenceEqual(InvokerComparerList()))
            throw new InvalidOperationException("The sorting implementations produced different results.");

        if (!ListSortComparison().SequenceEqual(ListSortComparer())
            || !ListSortComparison().SequenceEqual(ListSortInvoker())
            || !LinqOrderByThenBy().SequenceEqual(ListSortCompoundComparer())
            || !LinqOrderByThenBy().SequenceEqual(ListSortCompoundInvoker()))
            throw new InvalidOperationException("The list sorting implementations produced different results.");
    }

    [Benchmark]
    public TestClass[] LinqOrderByThenBy() =>
        items.OrderBy(static item => item.IntProperty)
            .ThenBy(static item => item.StringProperty)
            .ThenBy(static item => item.UIntProperty)
            .ToArray();

    [Benchmark]
    public TestClass[] InvokerComparerList()
    {
        var sorted = (TestClass[])items.Clone();
        Array.Sort(sorted, comparers);
        return sorted;
    }

    [Benchmark]
    public TestClass[] LinqOrderBy() =>
        items.OrderBy(static item => item.UIntProperty)
            .ToArray();

    [Benchmark]
    public TestClass[] InvokerComparer()
    {
        var sorted = (TestClass[])items.Clone();
        Array.Sort(sorted, comparer);
        return sorted;
    }

    [Benchmark]
    public List<TestClass> ListSortComparison()
    {
        var list = new List<TestClass>(items);
        list.Sort(static (x, y) => x.UIntProperty.CompareTo(y.UIntProperty));
        return list;
    }

    [Benchmark]
    public List<TestClass> ListSortComparer()
    {
        var list = new List<TestClass>(items);
        list.Sort(UIntPropertyComparer.Instance);
        return list;
    }

    [Benchmark]
    public List<TestClass> ListSortCompoundComparer()
    {
        var list = new List<TestClass>(items);
        list.Sort(SeveralPropertyComparer.Instance);
        return list;
    }

    [Benchmark]
    public List<TestClass> ListSortInvoker()
    {
        var list = new List<TestClass>(items);
        list.Sort(comparer);
        return list;
    }

    [Benchmark]
    public List<TestClass> ListSortCompoundInvoker()
    {
        var list = new List<TestClass>(items);
        list.Sort(comparers);
        return list;
    }


    public class UIntPropertyComparer : IComparer<TestClass>
    {
        public static readonly UIntPropertyComparer Instance = new();

        public int Compare(TestClass? x, TestClass? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            return x.UIntProperty.CompareTo(y.UIntProperty);
        }
    }

    public class SeveralPropertyComparer : IComparer<TestClass>
    {
        public static readonly SeveralPropertyComparer Instance = new();

        public int Compare(TestClass? x, TestClass? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            var result = x.IntProperty.CompareTo(y.IntProperty);
            if (result != 0)
                return result;
            result = x.StringProperty.CompareTo(y.StringProperty, StringComparison.Ordinal);
            if (result != 0)
                return result;
            return x.UIntProperty.CompareTo(y.UIntProperty);
        }
    }

}
