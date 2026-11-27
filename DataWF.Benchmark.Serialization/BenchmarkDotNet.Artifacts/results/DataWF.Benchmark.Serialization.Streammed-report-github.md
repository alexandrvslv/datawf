```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD Ryzen 7 2700U with Radeon Vega Mobile Gfx, 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.115
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3


```
| Method            | Mean        | Error      | StdDev     | Median      | Gen0       | Gen1       | Allocated |
|------------------ |------------:|-----------:|-----------:|------------:|-----------:|-----------:|----------:|
| MemoryPack        |    71.18 ms |   1.420 ms |   3.691 ms |    72.31 ms |  7666.6667 |  1333.3333 |  22.26 MB |
| InvokeMemoryPack  |    83.46 ms |   1.492 ms |   1.396 ms |    83.74 ms |  7333.3333 |  2000.0000 |   22.3 MB |
| MessagePack       |   176.35 ms |   5.006 ms |  14.760 ms |   177.72 ms |  8000.0000 |  2000.0000 |  22.61 MB |
| InvokeMessagePack |   187.91 ms |   3.564 ms |   5.443 ms |   188.23 ms |  7000.0000 |  2000.0000 |  22.82 MB |
| InvokeJson        |   647.52 ms |  12.713 ms |  14.640 ms |   646.91 ms | 10000.0000 |  3000.0000 |  28.94 MB |
| SystemJson        |   875.22 ms |  19.407 ms |  57.223 ms |   890.61 ms | 10000.0000 |  2000.0000 |  31.53 MB |
| InvokeXml         | 1,170.97 ms |  22.789 ms |  34.110 ms | 1,179.31 ms | 19000.0000 | 13000.0000 |  92.12 MB |
| SystemXml         | 2,057.32 ms | 107.588 ms | 296.328 ms | 1,967.96 ms | 32000.0000 |  9000.0000 |   93.5 MB |
