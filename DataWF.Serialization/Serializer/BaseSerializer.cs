using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DataWF.Serialization;

public abstract class BaseSerializer : IDisposable, ISerializer
{
    private static void CheckDirectory(string file)
    {
        var directory = Path.GetDirectoryName(file);
        if (directory != null && directory.Length > 0 && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);
    }

    private static string GetTemp(string file)
    {
        return file + "~";
    }

    private static string GetBack(string file)
    {
        return file + ".bac";
    }

    private static void Forward(string file, string temp)
    {
        if (File.Exists(file))
        {
            File.Replace(temp, file, GetBack(file));
        }
        else
        {
            File.Move(temp, file);
        }
    }

    protected Dictionary<Type, IElementSerializer> serializers = new();

    public BaseSerializer()
    { }

    public ISerializeTypeInfo<T> GetTypeInfo<T>()
    {
        if (serializers.Count > 0 && serializers.TryGetValue(typeof(T), out var serializer))
            return (ISerializeTypeInfo<T>)serializer.TypeInfo;

        return SerializeProvider.GetTypeInfo<T>();
    }

    public virtual void SetSerializer(Type type, IElementSerializer serializer)
        => this.serializers[type] = serializer;

    public IElementSerializer<T> Register<T>()
        where T : ISerializerRegister
    {
        if (serializers.Count > 0 && serializers.TryGetValue(typeof(T), out var serializer))
            return (IElementSerializer<T>)serializer;

        return SerializeProvider.Register<T>();
    }

    public virtual IElementSerializer<T> GetSerializer<T>()
    {
        if (serializers.Count > 0 && serializers.TryGetValue(typeof(T), out var serializer))
            return (IElementSerializer<T>)serializer;

        return SerializeProvider.GetSerializer<T>();
    }

    public IElementSerializer GetSerializer(Type type)
    {
        if (serializers.Count > 0 && serializers.TryGetValue(type, out var serializer))
            return serializer;

        return SerializeProvider.GetSerializer(type);
    }

    public void Serialize<T>(string file, in T? value)
    {
        CheckDirectory(file);

        if (value == null)
            return;
        var temp = GetTemp(file);
        using (var stream = new FileStream(temp, FileMode.Create))
        {
            Serialize(stream, value);
        }
        Forward(file, temp);
    }

    public async ValueTask SerializeAsync<T>(string file, T? value, CancellationToken cancellationToken = default)
    {
        CheckDirectory(file);

        if (value == null)
            return;
        var temp = GetTemp(file);
        using (var stream = new FileStream(temp, FileMode.Create))
        {
            await SerializeAsync(stream, value, cancellationToken);
        }
        Forward(file, temp);
    }

    public abstract ArraySegment<byte> Serialize<T>(in T? value);

    public abstract ArraySegment<byte> SerializePooled<T>(in T? value);

    public abstract void Serialize<T>(Stream stream, in T? value);

    public abstract ValueTask SerializeAsync<T>(Stream stream, T? value, CancellationToken cancellationToken = default);

    public T? Deserialize<T>(string file, bool saveIfNotExist = true)
    {
        T? value = default;
        Deserialize(file, ref value, saveIfNotExist);
        return value;
    }

    public int Deserialize<T>(string file, scoped ref T? value, bool saveIfNotExist = true)
    {
        if (!File.Exists(file))
        {
            if (saveIfNotExist)
            {
                Serialize(file, value);
            }
            return 0;
        }
        try
        {
            using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return Deserialize(stream, ref value);
        }
        catch
        {
            string bacFile = GetBack(file);
            if (File.Exists(bacFile))
            {
                return Deserialize(bacFile, ref value);
            }

            throw;
        }
    }

    public ValueTask<T?> DeserializeAsync<T>(string file, bool saveIfNotExist = true, CancellationToken cancellationToken = default)
        => DeserializeAsync<T>(file, default, saveIfNotExist, cancellationToken);

    public async ValueTask<T?> DeserializeAsync<T>(string file, T? value, bool saveIfNotExist = true, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(file))
        {
            if (saveIfNotExist)
            {
                await SerializeAsync(file, value, cancellationToken);
            }
            return value;
        }
        try
        {
            using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return await DeserializeAsync(stream, value, cancellationToken);
        }
        catch
        {
            string bacFile = GetBack(file);
            if (File.Exists(bacFile))
            {
                return await DeserializeAsync(bacFile, value, cancellationToken: cancellationToken);
            }

            throw;
        }
    }

    public T? Deserialize<T>(ReadOnlySpan<byte> data)
    {
        T? value = default;
        Deserialize(data, ref value);
        return value;
    }

    public T? Deserialize<T>(ArraySegment<byte> data)
    {
        T? value = default;
        Deserialize(data, ref value);
        return value;
    }

    public T? Deserialize<T>(Stream stream)
    {
        T? value = default;
        Deserialize(stream, ref value);
        return value;
    }

    public abstract int Deserialize<T>(ReadOnlySpan<byte> data, scoped ref T? value);

    public abstract int Deserialize<T>(ArraySegment<byte> data, scoped ref T? value);

    public abstract int Deserialize<T>(Stream stream, scoped ref T? value);

    public ValueTask<T?> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken = default)
        => DeserializeAsync<T>(stream, default, cancellationToken);

    public abstract ValueTask<T?> DeserializeAsync<T>(Stream stream, T? value, CancellationToken cancellationToken = default);

    public virtual void Dispose() => serializers.Clear();


}
