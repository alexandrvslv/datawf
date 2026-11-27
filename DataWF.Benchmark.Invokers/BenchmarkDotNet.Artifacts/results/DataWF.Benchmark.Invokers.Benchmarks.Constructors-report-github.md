```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD Ryzen 7 2700U with Radeon Vega Mobile Gfx, 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.115
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3


```
| Method        | IterationCount | Mean     | Error   | StdDev   | Gen0     | Gen1     | Allocated |
|-------------- |--------------- |---------:|--------:|---------:|---------:|---------:|----------:|
| Uninitialized | 1000           | 105.8 μs | 2.09 μs |  5.09 μs | 106.3232 | 106.2012 | 617.21 KB |
| Exact         | 1000           | 210.0 μs | 4.14 μs |  3.67 μs | 175.0488 | 113.2813 | 835.96 KB |
| Expression    | 1000           | 215.8 μs | 4.26 μs |  9.44 μs | 175.0488 | 113.5254 | 835.96 KB |
| Generate      | 1000           | 219.9 μs | 5.05 μs | 14.72 μs | 174.8047 | 113.0371 | 835.96 KB |
| Activator     | 1000           | 222.7 μs | 5.20 μs | 14.99 μs | 174.5605 | 113.0371 | 835.96 KB |
| Reflection    | 1000           | 224.1 μs | 6.13 μs | 17.59 μs | 174.8047 | 113.0371 | 835.96 KB |
