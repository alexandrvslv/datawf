# DataWF.Serialization Benchmarks

BenchmarkDotNet benchmarks comparing DataWF's generated/invoker serializers with
the corresponding framework and third-party serializers. The benchmarks use a
generated `TestClass` object graph containing primitive, nullable, collection,
and nested values.

## Benchmark groups

- **Buffered** — serializes to an in-memory byte buffer and deserializes it
  again. Includes DataWF Invoke, System.Text.Json, DataContractSerializer, MemoryPack,
  and MessagePack paths.
- **Streammed** — serializes to a file stream and deserializes from it, using
  asynchronous APIs where available.

Both groups use 100 `TestClass` instances by default and perform 100
serialize/deserialize iterations per benchmark operation. The values are
configured in `Benchmarks/BaseSerializetion.cs`.

## Run

From this directory, run all benchmarks:

```sh
dotnet run -c Release
```

Run only the buffered or streaming group:

```sh
dotnet run -c Release -- --filter '*Buffered*'
dotnet run -c Release -- --filter '*Streammed*'
```

BenchmarkDotNet writes reports to `BenchmarkDotNet.Artifacts/results/`.

## Buffered benchmark results

The charts below are a snapshot of the medians, allocated memory, and serialized
buffer sizes in
[`BenchmarkDotNet.Artifacts/results/DataWF.Benchmark.Serialization.Buffered-report-github.md`](BenchmarkDotNet.Artifacts/results/DataWF.Benchmark.Serialization.Buffered-report-github.md).
Refresh the chart values from the report after rerunning the benchmarks.
Short x-axis labels map to benchmark methods as follows:

| Label | Benchmark method |
|---|---|
| InvMemPack | `InvokeMemoryPack` |
| InvMsgPack | `InvokeMessagePack` |
| InvJson | `InvokeJson` |
| InvXML | `InvokeXml` |
| MemPack | `MemoryPack` |
| MsgPack | `MessagePack` |
| SysJson | `SystemJson` |
| SysXML | `SystemXml` |

### Mean duration

```mermaid
xychart-beta
    title "Buffered benchmark — mean"
    x-axis ["MemPack", "InvMemPack", "InvMsgPack", "MsgPack", "InvJson", "SysJson", "InvXML", "SysXML"]
    y-axis "Mean (ms)" 0 --> 1400
    bar [19.07, 19.01, 55.26, 86.09, 292.62, 359.93, 991.65, 1239.51]
```

### Allocated memory

```mermaid
xychart-beta
    title "Buffered benchmark — allocated memory"
    x-axis ["MemPack", "InvMemPack", "InvMsgPack", "MsgPack", "InvJson", "SysJson", "InvXML", "SysXML"]
    y-axis "Allocated (MB)" 0 --> 300
    bar [30.55, 30.55, 23.81, 23.97, 55.49, 57.68, 155.70, 256.45]
```

### Serialized buffer size

```mermaid
xychart-beta
    title "Buffered benchmark — serialized buffer size"
    x-axis ["MemPack", "InvMemPack", "InvMsgPack", "MsgPack", "InvJson", "SysJson", "InvXML", "SysXML"]
    y-axis "Buffer size (KiB)" 0 --> 700
    bar [119.18, 119.18, 50.20, 51.86, 340.31, 343.73, 348.19, 651.88]
```
