```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD Ryzen 7 2700U with Radeon Vega Mobile Gfx, 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.115
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3


```
| Method            | Mean        | Error      | StdDev     | Median      | Buffer size | Gen0       | Gen1       | Gen2       | Allocated |
|------------------ |------------:|-----------:|-----------:|------------:|------------ |-----------:|-----------:|-----------:|----------:|
| MemoryPack        |    24.81 ms |   0.492 ms |   1.331 ms |    24.53 ms | 132 124 B   |  9666.6667 |  4133.3333 |  4133.3333 |   34.7 MB |
| InvokeMemoryPack  |    25.03 ms |   0.497 ms |   1.220 ms |    24.82 ms | 132 124 B   |  9718.7500 |  4156.2500 |  4156.2500 |   34.7 MB |
| InvokeMessagePack |    72.83 ms |   2.551 ms |   7.360 ms |    71.69 ms | 57 746 B    |  8000.0000 |  2000.0000 |          - |  27.61 MB |
| MessagePack       |    94.57 ms |   1.778 ms |   2.820 ms |    93.60 ms | 59 446 B    |  9000.0000 |  3000.0000 |          - |  27.76 MB |
| InvokeJson        |   381.74 ms |  31.902 ms |  93.059 ms |   361.22 ms | 376 751 B   | 20000.0000 | 12000.0000 | 10000.0000 |  63.68 MB |
| SystemJson        |   387.79 ms |   7.676 ms |  21.270 ms |   386.83 ms | 379 169 B   | 19000.0000 | 12000.0000 | 10000.0000 |  66.74 MB |
| InvokeXml         | 1,072.81 ms |  67.792 ms | 199.887 ms | 1,050.75 ms | 396 617 B   | 82000.0000 | 38000.0000 | 37000.0000 | 241.23 MB |
| SystemXml         | 1,693.53 ms | 126.203 ms | 372.111 ms | 1,528.49 ms | 751 401 B   | 58000.0000 | 49000.0000 | 49000.0000 | 281.01 MB |
