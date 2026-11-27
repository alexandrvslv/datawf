using BenchmarkDotNet.Attributes;
using DataWF.Invokers;
using DataWF.Test;

namespace DataWF.Benchmark.Invokers.Benchmarks;

[MemoryDiagnoser]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
public class AccessIndexProperty : AccessBase
{
    private IInvoker<TestClass, int> generatedInvoker = null!;
    private IInvoker<TestClass, int> delegateInvoker = null!;
    private IInvoker<TestClass, int> expressionInvoker = null!;

    [GlobalSetup]
    public void Setup()
    {
        generatedInvoker = new TestClass.ItemInvoker<TestClass>() { Index = 1 };
        delegateInvoker = new ActionIndexInvoker<TestClass, int, int>("Item[1]",
            static (in item, index) => item[index],
            static (ref item, index, value) => item[index] = value)
        { Index = 1 };
        expressionInvoker = InvokeProvider.GetInvoker<TestClass, int>("Item[1]");
    }

    [Benchmark]
    public int Exact()
    {
        for (int i = 0; i < IterationCount; i++)
        {
            var value = testReference[1];
            value += 1;
            testReference[1] = value;
            if (testReference[1] != value)
                throw new System.Exception();
        }
        return testReference[1];
    }

    [Benchmark]
    public int Generate() => Laod(generatedInvoker);

    [Benchmark]
    public int Delegate() => Laod(delegateInvoker);

    [Benchmark]
    public int Expression() => Laod(expressionInvoker);

    private int Laod(IInvoker<TestClass, int> invoker)
    {
        for (int i = 0; i < IterationCount; i++)
        {
            var value = invoker.GetValue(in testReference);
            value += 1;
            invoker.SetValue(ref testReference, value);
            if (testReference[1] != value)
                throw new System.Exception();
        }
        return testReference[1];
    }
}

