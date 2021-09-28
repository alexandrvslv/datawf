using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;

namespace DataWF.Common
{
    public class CultureInfoSerializer : ElementSerializer<CultureInfo>
    {
        public static readonly CultureInfoSerializer Instance = new CultureInfoSerializer();

        public override bool CanConvertString => true;

        public override CultureInfo Read(BinaryReader reader) => FromString(StringSerializer.Instance.Read(reader));

        public override CultureInfo FromString(string value) => CultureInfo.GetCultureInfo(value);

        public override void Write(BinaryWriter writer, CultureInfo value, bool writeToken) => StringSerializer.Instance.Write(writer, ToString(value), writeToken);

        public override string ToString(CultureInfo value) => value?.Name;
    }

    public static class CultureInfoExtensions
    {
        public static CultureInfo ToCulture(this string cultureName) => CultureInfo.GetCultureInfo(cultureName);
    }
}
