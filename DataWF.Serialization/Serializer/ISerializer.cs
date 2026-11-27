using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DataWF.Serialization;

public interface ISerializer
{
    T? Deserialize<T>(ArraySegment<byte> data);
    int Deserialize<T>(ArraySegment<byte> data, scoped ref T? value);
    T? Deserialize<T>(ReadOnlySpan<byte> data);
    int Deserialize<T>(ReadOnlySpan<byte> data, scoped ref T? value);
    T? Deserialize<T>(Stream stream);
    int Deserialize<T>(Stream stream, scoped ref T? value);
    T? Deserialize<T>(string file, bool saveIfNotExist = true);
    int Deserialize<T>(string file, scoped ref T? value, bool saveIfNotExist = true);
    ValueTask<T?> DeserializeAsync<T>(string file, bool saveIfNotExist = true, CancellationToken cancellationToken = default);
    ValueTask<T?> DeserializeAsync<T>(string file, T? value, bool saveIfNotExist = true, CancellationToken cancellationToken = default);
    ValueTask<T?> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken = default);
    ValueTask<T?> DeserializeAsync<T>(Stream stream, T? value, CancellationToken cancellationToken = default);
    void Dispose();
    void Serialize<T>(string file, in T? value);
    ArraySegment<byte> Serialize<T>(in T? value);
    ArraySegment<byte> SerializePooled<T>(in T? value);
    void Serialize<T>(Stream stream, in T? value);
    ValueTask SerializeAsync<T>(string file, T? value, CancellationToken cancellationToken = default);
    ValueTask SerializeAsync<T>(Stream stream, T? value, CancellationToken cancellationToken = default);
    ISerializeTypeInfo<T> GetTypeInfo<T>();
    void SetSerializer(Type type, IElementSerializer serializer);
}
