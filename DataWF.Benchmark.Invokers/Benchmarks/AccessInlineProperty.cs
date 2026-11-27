using BenchmarkDotNet.Attributes;
using DataWF.Invokers;
using DataWF.Test;
using DataWF.Test.Invokers;

namespace DataWF.Benchmark.Invokers.Benchmarks;

[MemoryDiagnoser]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
public class AccessInlineProperty : AccessBase
{
    private IInvoker<TestClass, int> exactInvoker = null!;
    private IInvoker<TestClass, int> delegateInvoker = null!;
    private IInvoker<TestClass, int> expressionInvoker = null!;

    [GlobalSetup]
    public void Setup()
    {
        exactInvoker = new TestInlinePropertyInvoker();
        delegateInvoker = new ActionInvoker<TestClass, int>("Group.Struct.IntProperty",
            static (in item) => item.Group?.Struct.IntProperty ?? 0,
            static (ref item, value) =>
            {
                var temp = item.Group;
                if (temp != null)
                {
                    var temp2 = temp.Struct;
                    temp2.IntProperty = value;
                    temp.Struct = temp2;
                }
            });
        expressionInvoker = new ComplexInvoker<TestClass, int>("Group.Struct.IntProperty");
    }

    [Benchmark]
    public int? Exact() => Load(exactInvoker);

    [Benchmark]
    public int? Delegate() => Load(delegateInvoker);

    [Benchmark]
    public int? Expression() => Load(expressionInvoker);

    private int? Load(IInvoker<TestClass, int> invoker)
    {
        for (int i = 0; i < IterationCount; i++)
        {
            var value = invoker.GetValue(in testReference);
            value += 1;
            invoker.SetValue(ref testReference, value);
            if (testReference.Group?.Struct.IntProperty != value)
                throw new System.Exception();
        }
        return testReference.Group?.Struct.IntProperty;
    }
}

