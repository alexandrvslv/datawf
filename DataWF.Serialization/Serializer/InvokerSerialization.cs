using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DataWF.Serialization;

public class InvokerSerialization<TSerializer> where TSerializer : ISerializer, new()
{
    public static TSerializer Instance = new();

    public static T? Deserialize<T>(ReadOnlySpan<byte> data)
    {
        OnNotify(null, SerializeType.Load, "buffer");
        return Instance.Deserialize<T>(data);
    }

    public static int Deserialize<T>(ReadOnlySpan<byte> data, scoped ref T? value)
    {
        OnNotify(value, SerializeType.Load, "buffer");
        return Instance.Deserialize(data, ref value);
    }

    public static T? Deserialize<T>(ArraySegment<byte> data)
    {
        OnNotify(null, SerializeType.Load, "buffer");
        return Instance.Deserialize<T>(data);
    }

    public static int Deserialize<T>(ArraySegment<byte> data, scoped ref T? value)
    {
        OnNotify(value, SerializeType.Load, "buffer");
        return Instance.Deserialize(data, ref value);
    }

    public static T? Deserialize<T>(Stream stream)
    {
        OnNotify(null, SerializeType.Load, stream.ToString());
        return Instance.Deserialize<T>(stream);
    }

    public static int Deserialize<T>(Stream stream, scoped ref T? value)
    {
        OnNotify(value, SerializeType.Load, stream.ToString());
        return Instance.Deserialize(stream, ref value);
    }

    public static T? Deserialize<T>(string file, bool saveIfNotExist = true)
    {
        OnNotify(null, SerializeType.Load, file);
        return Instance.Deserialize<T>(file, saveIfNotExist);
    }

    public static int Deserialize<T>(string file, scoped ref T? value, bool saveIfNotExist = true)
    {
        OnNotify(value, SerializeType.Load, file);
        return Instance.Deserialize(file, ref value, saveIfNotExist);
    }

    public static ValueTask<T?> DeserializeAsync<T>(string file, T? value, bool saveIfNotExist = true, CancellationToken cancellationToken = default)
    {
        OnNotify(value, SerializeType.Load, file);
        return Instance.DeserializeAsync(file, value, saveIfNotExist, cancellationToken);
    }

    public static ValueTask<T?> DeserializeAsync<T>(Stream stream, T? value, CancellationToken cancellationToken = default)
    {
        OnNotify(value, SerializeType.Load, stream.ToString());
        return Instance.DeserializeAsync(stream, value, cancellationToken);
    }

    public static ValueTask<T?> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken = default)
    {
        OnNotify(null, SerializeType.Load, stream.ToString());
        return Instance.DeserializeAsync<T>(stream, cancellationToken);
    }

    public static ArraySegment<byte> Serialize<T>(in T? value)
    {
        OnNotify(value, SerializeType.Save, string.Empty);
        return Instance.Serialize(value);
    }

    public static ArraySegment<byte> SerializePooled<T>(in T? value)
    {
        OnNotify(value, SerializeType.Save, string.Empty);
        return Instance.SerializePooled(value);
    }

    public static void Serialize<T>(string file, in T? value)
    {
        OnNotify(value, SerializeType.Save, file);
        Instance.Serialize(file, value);
    }

    public static void Serialize<T>(Stream stream, in T? value)
    {
        OnNotify(value, SerializeType.Save, stream.ToString());
        Instance.Serialize(stream, value);
    }

    public static ValueTask SerializeAsync<T>(Stream stream, T? value, CancellationToken cancellationToken = default)
    {
        OnNotify(value, SerializeType.Save, stream.ToString());
        return Instance.SerializeAsync(stream, value, cancellationToken);
    }

    public static event EventHandler<SerializatorNotifyEventArgs>? Notify;

    private static void OnNotify(object? sender, SerializeType type, string? file)
    {
        if (Notify != null)
        {
            var arg = new SerializatorNotifyEventArgs(sender, type, file);
            Notify?.Invoke(sender, arg);
            //Helper.OnSerializeNotify(sender, arg);
        }
    }




}

