using System;
using System.IO;

namespace DataWF.Common
{
    public sealed class UriSerializer : ElementSerializer<Uri>
    {
        public static readonly UriSerializer Instance = new UriSerializer();

        public override bool CanConvertString => true;

        public override Uri FromString(string value) => new Uri(value);

        public override string ToString(Uri value) => value.ToString();

        public override Uri Read(BinaryReader reader)
        {
            var url = StringSerializer.Instance.Read(reader);
            return url == null ? null : new Uri(url);
        }

        public override void Write(BinaryWriter writer, Uri value, bool writeToken)
        {
            StringSerializer.Instance.Write(writer, value.ToString(), writeToken);
        }

    }
}

