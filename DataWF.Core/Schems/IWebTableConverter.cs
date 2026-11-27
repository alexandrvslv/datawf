
using System.Text.Json;
using DataWF.Models;
using DataWF.Serialization;

namespace DataWF;

public interface IModelTableConverter
{
    IModelTable Table { get; }

    ISerializeTypeInfo SerializationInfo { get; }

    object Read(ref Utf8JsonReader jreader, object item, JsonSerializerOptions options);
}