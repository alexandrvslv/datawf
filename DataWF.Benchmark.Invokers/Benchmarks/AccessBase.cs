using BenchmarkDotNet.Attributes;
using DataWF.Invokers;
using DataWF.Test;

namespace DataWF.Benchmark.Invokers.Benchmarks;

public abstract class AccessBase
{
    [Params(1000)]
    public int IterationCount { get; set; }
    protected TestClass testReference = TestClass.GenerateTestClass(3);
    
}

