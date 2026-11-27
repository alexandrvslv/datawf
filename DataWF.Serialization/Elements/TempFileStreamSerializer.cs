using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;

namespace DataWF.Serialization.Elements;

public sealed class TempFileStreamSerializer : TypeInfoSerializer<Stream>
{
    public static readonly TempFileStreamSerializer Instance = new();

}
