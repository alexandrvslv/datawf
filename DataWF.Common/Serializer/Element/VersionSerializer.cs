using System;
using System.Collections.Generic;
using System.IO;

namespace DataWF.Common
{
    public class VersionSerializer : ElementSerializer<Version>
    {
        public static readonly VersionSerializer Instance = new VersionSerializer();

        public override bool CanConvertString => true;

        public override Version Read(BinaryReader reader)
        {
            return new Version(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
        }

        public override void Write(BinaryWriter writer, Version value, bool writeToken)
        {
            if (writeToken)
            {
                writer.Write((byte)BinaryToken.ByteArray);
            }
            writer.Write(value.Major);
            writer.Write(value.Minor);
            writer.Write(value.Build);
            writer.Write(value.Revision);
        }

        public override Version FromString(string value) => Version.TryParse(value, out var version) ? version : new Version(0, 0, 0, 0);

        public override string ToString(Version value) => value?.ToString();
    }
}
