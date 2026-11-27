```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD Ryzen 7 2700U with Radeon Vega Mobile Gfx, 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.115
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3


```
| Method        | IterationCount | Mean      | Error     | StdDev     | Median    | Gen0    | Allocated |
|-------------- |--------------- |----------:|----------:|-----------:|----------:|--------:|----------:|
| Exact         | 1000           |  2.278 μs | 0.0456 μs |  0.0682 μs |  2.263 μs |       - |         - |
| Generate      | 1000           |  2.393 μs | 0.0479 μs |  0.0746 μs |  2.384 μs |       - |         - |
| Delegate      | 1000           |  2.660 μs | 0.1034 μs |  0.2814 μs |  2.557 μs |       - |         - |
| Expression    | 1000           |  4.351 μs | 0.3948 μs |  1.1392 μs |  4.084 μs |       - |         - |
| GenerateBox   | 1000           | 26.927 μs | 1.8856 μs |  5.4103 μs | 25.051 μs | 22.9492 |   48000 B |
| DelegateBox   | 1000           | 34.120 μs | 3.0825 μs |  9.0888 μs | 33.991 μs | 22.9492 |   48000 B |
| ExpressionBox | 1000           | 35.633 μs | 2.3762 μs |  7.0062 μs | 32.495 μs | 22.9492 |   48000 B |
| ReflectionBox | 1000           | 38.661 μs | 1.9951 μs |  5.4950 μs | 37.346 μs | 22.9492 |   48000 B |
| Reflection    | 1000           | 49.125 μs | 3.9384 μs | 11.1083 μs | 52.334 μs | 22.9492 |   48000 B |
