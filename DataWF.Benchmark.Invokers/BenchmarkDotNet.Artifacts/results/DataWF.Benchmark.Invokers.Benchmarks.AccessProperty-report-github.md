```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD Ryzen 7 2700U with Radeon Vega Mobile Gfx, 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.115
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3


```
| Method           | IterationCount | Mean      | Error     | StdDev     | Median    | Gen0    | Allocated |
|----------------- |--------------- |----------:|----------:|-----------:|----------:|--------:|----------:|
| Exact            | 1000           |  2.268 μs | 0.0452 μs |  0.0538 μs |  2.260 μs |       - |         - |
| Generated        | 1000           |  2.362 μs | 0.0466 μs |  0.0555 μs |  2.347 μs |       - |         - |
| Delegate         | 1000           |  2.718 μs | 0.1046 μs |  0.3017 μs |  2.624 μs |       - |         - |
| Expression       | 1000           |  4.193 μs | 0.3536 μs |  1.0371 μs |  3.895 μs |       - |         - |
| DelegateNonRef   | 1000           | 12.041 μs | 1.1634 μs |  3.3194 μs | 10.722 μs |       - |         - |
| GeneratedNonRef  | 1000           | 12.885 μs | 0.8011 μs |  2.3367 μs | 12.487 μs |       - |         - |
| ExpressionNonRef | 1000           | 14.241 μs | 0.8198 μs |  2.4043 μs | 13.513 μs |       - |         - |
| GeneratedBox     | 1000           | 29.668 μs | 1.8167 μs |  5.2417 μs | 28.419 μs | 22.9492 |   48000 B |
| DelegateBox      | 1000           | 30.449 μs | 2.8125 μs |  8.0241 μs | 27.407 μs | 22.9492 |   48000 B |
| ExpressionBox    | 1000           | 35.993 μs | 1.8475 μs |  5.4183 μs | 33.746 μs | 22.9492 |   48000 B |
| Reflection       | 1000           | 53.153 μs | 4.1897 μs | 12.1552 μs | 48.178 μs | 22.9492 |   48000 B |
| ReflectionBox    | 1000           | 54.935 μs | 4.7311 μs | 13.8008 μs | 53.961 μs | 22.9492 |   48000 B |
