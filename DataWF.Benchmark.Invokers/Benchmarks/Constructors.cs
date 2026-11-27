using BenchmarkDotNet.Attributes;
using DataWF.Invokers;
using DataWF.Test;
using System;

namespace DataWF.Benchmark.Invokers.Benchmarks;

[MemoryDiagnoser]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
public class Constructors : AccessBase
{
    private ActionConstructor<TestClass> expressionConstructor = null!;
    private UninitializedConstructor<TestClass> uninitializedConstructor = null!;
    private IRuntimeConstructor<TestClass> generatedConstructor = null!;
    private System.Reflection.ConstructorInfo reflectionConstructor = null!;

    [GlobalSetup]
    public void Setup()
    {
        expressionConstructor = new ActionConstructor<TestClass>();
        uninitializedConstructor = new UninitializedConstructor<TestClass>();
        generatedConstructor = TestClass.TestClassConstructor.Instance;
        reflectionConstructor = typeof(TestClass).GetConstructor(Type.EmptyTypes)!;
    }

    [Benchmark]
    public TestClass[] Exact()
    {
        var buffer = new TestClass[IterationCount];
        for (int i = 0; i < IterationCount; i++)
        {
            testReference = new TestClass();
            buffer[i] = testReference;
            if (testReference == null)
                throw new Exception();
        }
        return buffer;
    }

    [Benchmark]
    public TestClass[] Expression()
    {
        var buffer = new TestClass[IterationCount];
        for (int i = 0; i < IterationCount; i++)
        {
            testReference = expressionConstructor.Create();
            buffer[i] = testReference;
            if (testReference == null)
                throw new Exception();
        }
        return buffer;
    }

    [Benchmark]
    public TestClass[] Uninitialized()
    {
        var buffer = new TestClass[IterationCount];
        for (int i = 0; i < IterationCount; i++)
        {
            testReference = uninitializedConstructor.Create();
            buffer[i] = testReference;
            if (testReference == null)
                throw new Exception();
        }
        return buffer;
    }

    [Benchmark]
    public TestClass[] Generate()
    {
        var buffer = new TestClass[IterationCount];
        for (int i = 0; i < IterationCount; i++)
        {
            testReference = generatedConstructor.Create();
            buffer[i] = testReference;
            if (testReference == null)
                throw new Exception();
        }
        return buffer;
    }

    [Benchmark]
    public TestClass[] Reflection()
    {
        var buffer = new TestClass[IterationCount];
        for (int i = 0; i < IterationCount; i++)
        {
            testReference = (TestClass)reflectionConstructor.Invoke(null);
            buffer[i] = testReference;
            if (testReference == null)
                throw new Exception();
        }
        return buffer;
    }

    [Benchmark]
    public TestClass[] Activator()
    {
        var buffer = new TestClass[IterationCount];
        for (int i = 0; i < IterationCount; i++)
        {
            testReference = System.Activator.CreateInstance<TestClass>();
            buffer[i] = testReference;
            if (testReference == null)
                throw new Exception();
        }
        return buffer;
    }
}

