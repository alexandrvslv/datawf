using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace DataWF.Serialization;

public class XmlInvokerSerializer : BaseSerializer
{

    public XmlInvokerSerializer()
    { }

    public bool Indented { get; set; } = false;
    public JsonIgnoreCondition DefaultIgnoreCondition { get; set; }

    public override ArraySegment<byte> Serialize<T>(in T? value) where T : default
    {
        using var memoryStream = new MemoryStream();
        Serialize(memoryStream, in value);
        return memoryStream.GetBytes();
    }

    public override ArraySegment<byte> SerializePooled<T>(in T? value) where T : default
    {
        using var memoryStream = new MemoryStream();
        Serialize(memoryStream, in value);
        return memoryStream.GetPooledBytes();
    }

    public override void Serialize<T>(Stream stream, in T? value) where T : default
    {
        using var writer = new XmlInvokeWriter(stream, this);
        writer.Write(ref Unsafe.AsRef(in value));
        writer.Flush();
    }

    public override ValueTask SerializeAsync<T>(Stream stream, T? value, CancellationToken cancellationToken = default) where T : default
    {
        using var writer = new XmlInvokeWriter(stream, this);
        writer.Write(ref Unsafe.AsRef(in value));
        writer.Flush();
        return ValueTask.CompletedTask;
    }

    public override int Deserialize<T>(ReadOnlySpan<byte> data, scoped ref T? value) where T : default
    {
        using var memoryStream = new MemoryStream();
        memoryStream.Write(data);
        memoryStream.Position = 0;
        return Deserialize(memoryStream, ref value);
    }

    public override int Deserialize<T>(ArraySegment<byte> data, scoped ref T? value) where T : default
    {
        using var memoryStream = new MemoryStream(data.Array!, data.Offset, data.Count);
        return Deserialize(memoryStream, ref value);
    }

    public override int Deserialize<T>(Stream stream, scoped ref T? value) where T : default
    {
        using var buffer = new BufferedStream(stream);
        using var reader = new XmlInvokeReader(buffer, this);
        reader.ReadNextElement();
        reader.Read(ref value);
        return (int)reader.Consumed;
    }

    public override ValueTask<T?> DeserializeAsync<T>(Stream stream, T? value, CancellationToken cancellationToken = default) where T : default
    {
        using var buffer = new BufferedStream(stream);
        using var reader = new XmlInvokeReader(buffer, this);
        reader.ReadNextElement();
        reader.Read(ref value);
        return new ValueTask<T?>(value);
    }


}
