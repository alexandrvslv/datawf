```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD Ryzen 7 2700U with Radeon Vega Mobile Gfx, 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.115
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3


```
| Method                 | IterationCount | Mean        | Error       | StdDev       | Median      | Gen0    | Allocated |
|----------------------- |--------------- |------------:|------------:|-------------:|------------:|--------:|----------:|
| ExpressionBox          | 1000           |          NA |          NA |           NA |          NA |      NA |        NA |
| Exact                  | 1000           |    309.0 ns |     6.24 ns |     16.22 ns |    301.3 ns |       - |         - |
| Generated              | 1000           |  2,319.0 ns |    40.28 ns |     35.70 ns |  2,314.5 ns |       - |         - |
| Delegate               | 1000           |  2,412.4 ns |    48.25 ns |    121.93 ns |  2,389.5 ns |       - |         - |
| Expression             | 1000           |  3,881.6 ns |   387.54 ns |  1,118.15 ns |  3,482.2 ns |       - |         - |
| ExpressionValueType    | 1000           |  4,227.4 ns |   372.23 ns |  1,097.52 ns |  4,227.5 ns |       - |         - |
| GeneratedBox           | 1000           | 22,184.6 ns | 1,196.94 ns |  3,376.00 ns | 21,052.1 ns | 22.9492 |   48032 B |
| DelegateBox            | 1000           | 24,429.0 ns | 1,495.94 ns |  4,316.14 ns | 23,702.7 ns | 22.9492 |   48032 B |
| ExpressionValueTypeBox | 1000           | 26,428.8 ns | 1,693.83 ns |  4,967.72 ns | 24,895.3 ns | 22.9492 |   48032 B |
| ReflectionBox          | 1000           | 61,618.3 ns | 3,586.38 ns | 10,461.63 ns | 57,249.7 ns | 22.9492 |   48032 B |

Benchmarks with issues:
  AccessStructProperty.ExpressionBox: DefaultJob [IterationCount=1000]
