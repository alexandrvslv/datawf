using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using DataWF.Test;

namespace DataWF.Benchmark.Serialization;

public class BaseSerializetion
{
    public List<TestClass> list = null!;

    public int Iterations { get; set; } = 100;

    //[Params(100)]//, 1000
    public int ListLength { get; set; } = DefaultListLength;

    public const int DefaultListLength = 100;

    [GlobalSetup]
    public void Setup()
    {
        list = TestClass.GenerateList(ListLength);
    }
}