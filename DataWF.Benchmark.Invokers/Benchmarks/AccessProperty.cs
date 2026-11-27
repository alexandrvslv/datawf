using BenchmarkDotNet.Attributes;
using DataWF.Invokers;
using DataWF.Test;
using DataWF.Test.Invokers;

namespace DataWF.Benchmark.Invokers.Benchmarks;

[MemoryDiagnoser]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
public class AccessProperty : AccessBase
{
    private IInvoker<TestClass, int> generatedInvoker = null!;
    private IInvoker<TestClass, int> delegateInvoker = null!;
    private IInvoker<TestClass, int> expressionInvoker = null!;
    private IInvoker<TestClass, int> reflectionInvoker = null!;

    [GlobalSetup]
    public void Setup()
    {
        generatedInvoker = TestClass.IntPropertyInvoker<TestClass>.Instance;
        delegateInvoker = new ActionInvoker<TestClass, int>(nameof(TestClass.IntProperty),
            static (in item) => item.IntProperty,
            static (ref item, value) => item.IntProperty = value);
        expressionInvoker = new PropertyInvoker<TestClass, int>(nameof(TestClass.IntProperty));
        reflectionInvoker = new ReflectionPropertyInvoker<TestClass, int>(nameof(TestClass.IntProperty));
    }

    [Benchmark]
    public int Exact()
    {
        for (int i = 0; i < IterationCount; i++)
        {
            var value = testReference.IntProperty;
            value += 1;
            testReference.IntProperty = value;
            if (testReference.IntProperty != value)
                throw new System.Exception();
        }
        return testReference.IntProperty;
    }

    [Benchmark]
    public int GeneratedNonRef() => LoadNonRef(generatedInvoker);

    [Benchmark]
    public int DelegateNonRef() => LoadNonRef(delegateInvoker);

    [Benchmark]
    public int ExpressionNonRef() => LoadNonRef(expressionInvoker);

    [Benchmark]
    public int Generated() => Load(generatedInvoker);

    [Benchmark]
    public int Delegate() => Load(delegateInvoker);

    [Benchmark]
    public int Expression() => Load(expressionInvoker);

    [Benchmark]
    public int Reflection() => Load(reflectionInvoker);

    [Benchmark]
    public int GeneratedBox() => LoadBoxing(generatedInvoker);

    [Benchmark]
    public int DelegateBox() => LoadBoxing(delegateInvoker);

    [Benchmark]
    public int ExpressionBox() => LoadBoxing(expressionInvoker);

    [Benchmark]
    public int ReflectionBox() => LoadBoxing(reflectionInvoker);

    private int Load(IInvoker<TestClass, int> invoker)
    {
        for (int i = 0; i < IterationCount; i++)
        {
            var value = invoker.GetValue(in testReference);
            value += 1;
            invoker.SetValue(ref testReference, value);
            if (testReference.IntProperty != value)
                throw new System.Exception();
        }
        return testReference.IntProperty;
    }

    private int LoadNonRef(IInvoker<TestClass, int> invoker)
    {
        for (int i = 0; i < IterationCount; i++)
        {
            var value = ((IInvokerGetter<TestClass, int>)invoker).GetValue(testReference);
            value += 1;
            invoker.SetValue(testReference, value);
            if (testReference.IntProperty != value)
                throw new System.Exception();
        }
        return testReference.IntProperty;
    }

    private int LoadBoxing(IInvoker invoker)
    {
        for (int i = 0; i < IterationCount; i++)
        {
            var value = (int)invoker.GetValue(testReference)!;
            value += 1;
            invoker.SetValue(testReference, value);
            if (testReference.IntProperty != value)
                throw new System.Exception();
        }
        return testReference.IntProperty;
    }


}

