using System.Text.Json.Serialization;

namespace DataWF.Test;

[JsonSerializable(typeof(TestClass))]
[JsonSerializable(typeof(TestStruct))]
public partial class TestJsonSerializerContext : JsonSerializerContext { }

