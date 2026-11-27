using BenchmarkDotNet.Attributes;
using DataWF.Invokers;
using DataWF.Test;
using DataWF.Test.Invokers;

namespace DataWF.Benchmark.Invokers.Benchmarks;

[MemoryDiagnoser]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
public class AccessField : AccessBase
{
    private IInvoker<TestClass, int> generateInvoker = null!;
    private IInvoker<TestClass, int> delegateInvoker = null!;
    private IInvoker<TestClass, int> reflectionInvoker = null!;
    private IInvoker<TestClass, int> expressionInvoker = null!;

    [GlobalSetup]
    public void Setup()
    {
        generateInvoker = TestClass.FieldInvoker<TestClass>.Instance;
        delegateInvoker = new ActionInvoker<TestClass, int>(nameof(TestClass.Field),
            static (in item) => item.Field,
            static (ref item, value) => item.Field = value);
        reflectionInvoker = new ReflectionFieldInvoker<TestClass, int>(nameof(TestClass.Field));
        expressionInvoker = new FieldInvoker<TestClass, int>(nameof(TestClass.Field));
    }

    [Benchmark]
    public int Exact()
    {
        for (int i = 0; i < IterationCount; i++)
        {
            var value = testReference.Field;
            value += 1;
            testReference.Field = value;
            if (testReference.Field != value)
                throw new System.Exception();
        }
        return testReference.Field;
    }

    [Benchmark]
    public int Generate() => Load(generateInvoker);

    [Benchmark]
    public int Delegate() => Load(delegateInvoker);

    [Benchmark]
    public int Expression() => Load(expressionInvoker);

    [Benchmark]
    public int Reflection() => Load(reflectionInvoker);

    [Benchmark]
    public int GenerateBox() => LoadBoxing(generateInvoker);

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
            if (testReference.Field != value)
                throw new System.Exception();
        }
        return testReference.Field;
    }

    private int LoadBoxing(IInvoker invoker)
    {
        for (int i = 0; i < IterationCount; i++)
        {
            var value = (int)invoker.GetValue(testReference)!;
            value += 1;
            invoker.SetValue(testReference, value);
            if (testReference.Field != value)
                throw new System.Exception();
        }
        return testReference.Field;
    }

}

