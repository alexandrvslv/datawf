```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD Ryzen 7 2700U with Radeon Vega Mobile Gfx, 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.115
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3


```
| Method                   | ItemCount | Mean      | Error     | StdDev    | Median    | Gen0   | Allocated |
|------------------------- |---------- |----------:|----------:|----------:|----------:|-------:|----------:|
| ListSortComparer         | 1024      |  25.85 μs |  2.600 μs |  7.585 μs |  22.52 μs | 3.9673 |   8.12 KB |
| ListSortInvoker          | 1024      |  25.99 μs |  2.183 μs |  6.122 μs |  22.52 μs | 3.9673 |   8.12 KB |
| ListSortComparison       | 1024      |  26.60 μs |  2.726 μs |  7.734 μs |  23.06 μs | 3.9063 |   8.05 KB |
| ListSortCompoundComparer | 1024      | 162.37 μs | 10.556 μs | 29.773 μs | 151.02 μs | 3.9063 |   8.12 KB |
| ListSortCompoundInvoker  | 1024      | 294.84 μs | 20.395 μs | 59.493 μs | 284.31 μs | 3.9063 |   8.12 KB |
