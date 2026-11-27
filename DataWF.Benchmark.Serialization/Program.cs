using System;
using BenchmarkDotNet.Running;

namespace DataWF.Benchmark.Serialization;

class Program
{
    static void Main(string[] args)
    {
        // Run the benchmarks
        if (args.Length == 0)
            BenchmarkRunner.Run(typeof(Program).Assembly);
        else
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
