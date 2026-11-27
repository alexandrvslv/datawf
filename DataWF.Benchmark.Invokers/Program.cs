using System;
using BenchmarkDotNet.Running;

namespace DataWF.Benchmark.Invokers
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args == null || args.Length == 0)
                BenchmarkRunner.Run(typeof(Program).Assembly);
            else
                BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}