```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD Ryzen 7 2700U with Radeon Vega Mobile Gfx, 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.115
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3


```
| Method     | IterationCount | Mean     | Error     | StdDev    | Median   | Allocated |
|----------- |--------------- |---------:|----------:|----------:|---------:|----------:|
| Exact      | 1000           | 2.266 μs | 0.0449 μs | 0.0583 μs | 2.242 μs |         - |
| Generate   | 1000           | 2.986 μs | 0.1358 μs | 0.3962 μs | 2.815 μs |         - |
| Delegate   | 1000           | 4.240 μs | 0.3501 μs | 1.0267 μs | 3.805 μs |         - |
| Expression | 1000           | 6.208 μs | 0.6265 μs | 1.7874 μs | 5.617 μs |         - |
