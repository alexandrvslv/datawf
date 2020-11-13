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

        public override object ConvertFromString(string value) => CultureInfo.GetCultureInfo(value);

        public override string ConvertToString(object value) => ((CultureInfo)value).Name;

        public override object ConvertFromBinary(BinaryReader reader) => FromBinary(reader);

        public override void ConvertToBinary(BinaryWriter writer, object value, bool writeToken) => ToBinary(writer, (CultureInfo)value, writeToken);

        public override CultureInfo FromBinary(BinaryReader reader) => FromString(StringSerializer.Instance.FromBinary(reader));

        public override CultureInfo FromString(string value) => CultureInfo.GetCultureInfo(value);

        public override void ToBinary(BinaryWriter writer, CultureInfo value, bool writeToken) => StringSerializer.Instance.ToBinary(writer, ToString(value), writeToken);

        public override string ToString(CultureInfo value) => value?.Name;
    }
}
