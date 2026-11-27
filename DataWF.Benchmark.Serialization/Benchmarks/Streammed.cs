using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using DataWF.Serialization;
using DataWF.Test;
using MemoryPack;
using MessagePack;

namespace DataWF.Benchmark.Serialization;

[MemoryDiagnoser]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
public class Streammed : BaseSerializetion
{
    [Benchmark]
    public async Task InvokeXml()
    {
        var fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DataWF.Benchmark.Serialize.Invoke.xml");
        for (var i = 0; i < Iterations; i++)
        {
            await using (var stream = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite))
                await XmlInvoker.SerializeAsync(stream, list!);
            await using var input = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            var newList = XmlInvoker.Deserialize<List<TestClass>>(input);
            if (newList?.Count != list.Count)
                throw new Exception();
        }
    }

    [Benchmark]
    public async Task InvokeJson()
    {
        var fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DataWF.Benchmark.Serialize.Invoke.json");
        for (var i = 0; i < Iterations; i++)
        {
            await using (var stream = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite))
                await JsonInvoker.SerializeAsync(stream, list!);
            await using var input = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            var newList = await JsonInvoker.DeserializeAsync<List<TestClass>>(input);
            if (newList?.Count != list.Count)
                throw new Exception();
        }
    }

    [Benchmark]
    public async Task InvokeMemoryPack()
    {
        var fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DataWF.Benchmark.Serialize.Invoke.bin");
        for (var i = 0; i < Iterations; i++)
        {
            await using (var stream = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite))
                await MemoryPackInvoker.SerializeAsync(stream, list!);
            await using var input = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            var newList = await MemoryPackInvoker.DeserializeAsync<List<TestClass>>(input);
            if (newList?.Count != list.Count)
                throw new Exception();
        }
    }

    [Benchmark]
    public async Task InvokeMessagePack()
    {
        var fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DataWF.Benchmark.Serialize.Invoke.msgpack");
        for (var i = 0; i < Iterations; i++)
        {
            await using (var stream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                await MessagePackInvoker.SerializeAsync(stream, list!);
            await using var input = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            var newList = await MessagePackInvoker.DeserializeAsync<List<TestClass>>(input);
            if (newList?.Count != list.Count)
                throw new Exception();
        }
    }

    [Benchmark]
    public void SystemXml()
    {
        var fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DataWF.Benchmark.Serialize.System.xml");
        var serializer = new DataContractSerializer(typeof(List<TestClass>));
        for (var i = 0; i < Iterations; i++)
        {
            var stream = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite);
            serializer.WriteObject(stream, list!);
            stream.Dispose();
            stream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            var newList = (List<TestClass>)serializer.ReadObject(stream)!;
            stream.Dispose();
            if (newList?.Count != list.Count)
                throw new Exception();
        }
    }

    [Benchmark]
    public async Task SystemJson()
    {
        var fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DataWF.Benchmark.Serialize.System.json");
        for (var i = 0; i < Iterations; i++)
        {
            await using (var stream = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite))
                await JsonSerializer.SerializeAsync(stream, list!);
            await using var input = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            var newList = await JsonSerializer.DeserializeAsync<List<TestClass>>(input);
            if (newList!.Count != list.Count)
                throw new Exception();
        }
    }

    [Benchmark]
    public async Task MemoryPack()
    {
        var fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DataWF.Benchmark.Serialize.System.bin");
        for (var i = 0; i < Iterations; i++)
        {
            await using (var stream = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite))
                await MemoryPackSerializer.SerializeAsync(stream, list!);
            await using var input = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            var newList = await MemoryPackSerializer.DeserializeAsync<List<TestClass>>(input);
            if (newList!.Count != list.Count)
                throw new Exception();
        }
    }

    [Benchmark]
    public async Task MessagePack()
    {
        var fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DataWF.Benchmark.Serialize.System.msgpack");
        var options = MessagePackInvokerSerializer.DefaultOptions;
        for (var i = 0; i < Iterations; i++)
        {
            await using (var stream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                await MessagePackSerializer.SerializeAsync(stream, list!, options);
            await using var input = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            var newList = await MessagePackSerializer.DeserializeAsync<List<TestClass>>(input, options);
            if (newList?.Count != list.Count)
                throw new Exception();
        }
    }
}