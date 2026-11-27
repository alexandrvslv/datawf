using BenchmarkDotNet.Attributes;
using DataWF.Invokers;
using DataWF.Test;

namespace DataWF.Benchmark.Invokers.Benchmarks;

[MemoryDiagnoser]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
public class AccessStructProperty : AccessBase
{
    private IInvoker<TestStruct, int> generatedInvoker = null!;
    private IInvoker<TestStruct, int> delegateInvoker = null!;
    private IInvoker<TestStruct, int> expressionInvoker = null!;
    private IInvoker<TestStruct, int> expressionValueTypeInvoker = null!;
    private IInvoker<TestStruct, int> reflectionInvoker = null!;

    [GlobalSetup]
    public void Setup()
    {
        generatedInvoker = TestStruct.IntPropertyInvoker.Instance;
        delegateInvoker = new ValueTypeActionInvoker<TestStruct, int>(nameof(TestStruct.IntProperty),
            static (in item) => item.IntProperty,
            static (ref item, value) => item.IntProperty = value);
        expressionInvoker = new PropertyInvoker<TestStruct, int>(nameof(TestStruct.IntProperty));
        expressionValueTypeInvoker = new ValueTypePropertyInvoker<TestStruct, int>(nameof(TestStruct.IntProperty));
        reflectionInvoker = new ReflectionPropertyInvoker<TestStruct, int>(nameof(TestStruct.IntProperty));
    }

    [Benchmark]
    public int Exact()
    {
        var testStrcut = testReference.Struct;
        for (int i = 0; i < IterationCount; i++)
        {
            var value = testStrcut.IntProperty;
            value += 1;
            testStrcut.IntProperty = value;
            if (testStrcut.IntProperty != value)
                throw new System.Exception();
        }
        return testStrcut.IntProperty;
    }

    [Benchmark]
    public int Generated() => Load(generatedInvoker);

    [Benchmark]
    public int Delegate() => Load(delegateInvoker);

    [Benchmark]
    public int Expression() => Load(expressionInvoker);

    [Benchmark]
    public int ExpressionValueType() => Load(expressionValueTypeInvoker);

    [Benchmark]
    public int GeneratedBox() => LoadBoxing(generatedInvoker);

    [Benchmark]
    public int DelegateBox() => LoadBoxing(delegateInvoker);

    [Benchmark]
    public int ExpressionBox() => LoadBoxing(expressionInvoker);

    [Benchmark]
    public int ExpressionValueTypeBox() => LoadBoxing(expressionValueTypeInvoker);

    [Benchmark]
    public int ReflectionBox() => LoadBoxing(reflectionInvoker);

    private int Load(IInvoker<TestStruct, int> invoker)
    {
        var testStruct = testReference.Struct;
        for (int i = 0; i < IterationCount; i++)
        {
            var value = invoker.GetValue(in testStruct);
            value += 1;
            invoker.SetValue(ref testStruct, value);
            if (testStruct.IntProperty != value)
                throw new System.Exception();
        }
        return testStruct.IntProperty;
    }

    private int LoadBoxing(IInvoker invoker)
    {
        var testStruct = (object)testReference.Struct;
        for (int i = 0; i < IterationCount; i++)
        {
            var value = (int)invoker.GetValue(testStruct)!;
            value += 1;
            invoker.SetValue(testStruct, value);
            if (((TestStruct)testStruct).IntProperty != value)
                throw new System.Exception();
        }
        return ((TestStruct)testStruct).IntProperty;
    }
    
}

