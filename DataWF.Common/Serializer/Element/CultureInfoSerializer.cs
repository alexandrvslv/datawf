using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace DataWF.Common
{
    public class CultureInfoSerializer : ElementSerializer<CultureInfo>
    {
        public static readonly CultureInfoSerializer Instance = new CultureInfoSerializer();

        public override bool CanConvertString => true;

        public override CultureInfo FromBinary(BinaryReader reader) => FromString(StringSerializer.Instance.FromBinary(reader));

        public override CultureInfo FromString(string value) => CultureInfo.GetCultureInfo(value);

        public override void ToBinary(BinaryWriter writer, CultureInfo value, bool writeToken) => StringSerializer.Instance.ToBinary(writer, ToString(value), writeToken);

        public override string ToString(CultureInfo value) => value?.Name;
    }
}
