```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD Ryzen 7 2700U with Radeon Vega Mobile Gfx, 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.115
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3


```
| Method     | IterationCount | Mean      | Error     | StdDev    | Allocated |
|----------- |--------------- |----------:|----------:|----------:|----------:|
| Delegate   | 1000           |  9.712 μs | 0.1376 μs | 0.1149 μs |         - |
| Expression | 1000           |  9.724 μs | 0.1939 μs | 0.2308 μs |         - |
| Exact      | 1000           | 10.977 μs | 0.2153 μs | 0.2874 μs |         - |
