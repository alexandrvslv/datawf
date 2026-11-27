using System.Collections.Concurrent;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using DataWF.Invokers;
using DataWF.Serialization;
using DataWF.Test;
using MemoryPack;
using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text.Json;

namespace DataWF.Benchmark.Serialization;

[Config(typeof(BufferedConfig))]
[MemoryDiagnoser]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
public class Buffered : BaseSerializetion
{
    private static readonly ConcurrentDictionary<string, long> SerializedSizes = new();

    private static readonly JsonSerializerOptions CaseInsensitiveJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    internal static long GetSerializedSize(string methodName) =>
        SerializedSizes.GetOrAdd(methodName, CalculateSerializedSize);

    private static long CalculateSerializedSize(string methodName)
    {
        var items = TestClass.GenerateList(DefaultListLength);
        return methodName switch
        {
            nameof(InvokeXml) => XmlInvoker.Serialize(items).Count,
            nameof(InvokeJson) => JsonInvoker.Serialize(items).Count,
            nameof(InvokeMemoryPack) => MemoryPackInvoker.Serialize(items).Count,
            nameof(InvokeMessagePack) => MessagePackInvoker.Serialize(items).Count,
            nameof(SystemXml) => WriteSystemXml(new DataContractSerializer(typeof(List<TestClass>)), items).Count,
            nameof(SystemJson) => JsonSerializer.SerializeToUtf8Bytes(items).Length,
            nameof(MemoryPack) => MemoryPackSerializer.Serialize(items).Length,
            nameof(MessagePack) => MessagePackSerializer.Serialize(items, MessagePackInvokerSerializer.DefaultOptions).Length,
            _ => throw new ArgumentOutOfRangeException(nameof(methodName), methodName, "Unknown buffered benchmark."),
        };
    }

    [Benchmark]
    public void InvokeXml()
    {
        for (var i = 0; i < Iterations; i++)
        {
            var buffer = XmlInvoker.Serialize(list!);
            var newList = XmlInvoker.Deserialize<List<TestClass>>(buffer);
            if (newList?.Count != list.Count)
                throw new Exception();
        }
    }

    [Benchmark]
    public void InvokeJson()
    {
        for (var i = 0; i < Iterations; i++)
        {
            var buffer = JsonInvoker.Serialize(list!);
            var newList = JsonInvoker.Deserialize<List<TestClass>>(buffer);
            if (newList?.Count != list.Count)
                throw new Exception();
        }
    }

    [Benchmark]
    public void InvokeMemoryPack()
    {
        for (var i = 0; i < Iterations; i++)
        {
            var buffer = MemoryPackInvoker.Serialize(list!);
            var newList = MemoryPackInvoker.Deserialize<List<TestClass>>(buffer);
            if (newList?.Count != list.Count)
                throw new Exception();
        }
    }

    [Benchmark]
    public void InvokeMessagePack()
    {
        for (var i = 0; i < Iterations; i++)
        {
            var buffer = MessagePackInvoker.Serialize(list!);
            var newList = MessagePackInvoker.Deserialize<List<TestClass>>(buffer);
            if (newList?.Count != list.Count)
                throw new Exception();
        }
    }

    [Benchmark]
    public void SystemXml()
    {
        var serializer = new DataContractSerializer(typeof(List<TestClass>));

        for (var i = 0; i < Iterations; i++)
        {
            var buffer = WriteSystemXml(serializer, list);
            var newList = ReadSystemXml<List<TestClass>>(serializer, buffer);
            if (newList.Count != list.Count)
                throw new Exception();
        }
    }

    [Benchmark]
    public void SystemJson()
    {
        for (var i = 0; i < Iterations; i++)
        {
            var buffer = JsonSerializer.SerializeToUtf8Bytes(list);
            var newList = JsonSerializer.Deserialize<List<TestClass>>(buffer);
            if (newList?.Count != list.Count)
                throw new Exception();
        }
    }

    [Benchmark]
    public void MemoryPack()
    {
        for (var i = 0; i < Iterations; i++)
        {
            var buffer = MemoryPackSerializer.Serialize(list);
            var newList = MemoryPackSerializer.Deserialize<List<TestClass>>(buffer.AsSpan())!;
            if (newList?.Count != list.Count)
                throw new Exception();
        }
    }

    [Benchmark]
    public void MessagePack()
    {
        var options = MessagePackInvokerSerializer.DefaultOptions;
        for (var i = 0; i < Iterations; i++)
        {
            var buffer = MessagePackSerializer.Serialize(list, options);
            var newList = MessagePackSerializer.Deserialize<List<TestClass>>(buffer, options)!;
            if (newList.Count != list.Count)
                throw new Exception();
        }
    }

    
    public static T Read<T>(ISerializer serializer, ArraySegment<byte> buffer)
        => serializer.Deserialize<T>(buffer)!;

    public static ArraySegment<byte> WriteSystemXml<T>(DataContractSerializer serializer, T value)
    {
        using var stream = new MemoryStream();
        serializer.WriteObject(stream, value);
        return stream.GetBytes();
    }

    public static T ReadSystemXml<T>(DataContractSerializer serializer, ArraySegment<byte> buffer)
    {
        using var stream = new MemoryStream(buffer.Array!, buffer.Offset, buffer.Count);
        return (T)serializer.ReadObject(stream)!;
    }
}

public sealed class BufferedConfig : ManualConfig
{
    public BufferedConfig()
    {
        AddColumn(new SerializedFileSizeColumn());
    }
}

public sealed class SerializedFileSizeColumn : IColumn
{
    public string Id => nameof(SerializedFileSizeColumn);
    public string ColumnName => "Buffer size";
    public bool AlwaysShow => true;
    public ColumnCategory Category => ColumnCategory.Custom;
    public int PriorityInCategory => 0;
    public bool IsNumeric => false;
    public UnitType UnitType => UnitType.Dimensionless;
    public string Legend => "Serialized output size for one iteration.";

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
    {
        var size = Buffered.GetSerializedSize(benchmarkCase.Descriptor.WorkloadMethod.Name);
        return $"{size:N0} B";
    }

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style) =>
        GetValue(summary, benchmarkCase);

    public bool IsAvailable(Summary summary) => true;
    public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;
}