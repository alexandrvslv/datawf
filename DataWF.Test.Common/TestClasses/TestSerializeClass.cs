using System;
using System.ComponentModel;

namespace DataWF.Test.Common
{
    public class TestSerializeClass
    {
        public int IntValue { get; set; }

        public Guid GuidValue { get; set; }

        public int? NullableIntValue { get; set; }

        [DefaultValue(1)]
        public int DefaultValue { get; set; } = 1;

        public decimal DecimalValue { get; set; } = 2M;

        public decimal? NullableDecimalValue { get; set; }

        public double DoubleValue { get; set; } = 3D;

        public string StringValue { get; set; } = "bla";

        public DateTime DateValue { get; set; } = DateTime.UtcNow;

        public byte[] ByteArrayValue { get; set; } = new byte[] { (byte)'b', (byte)'l', (byte)'a' };

        public TestSerializeClass ClassValue { get; set; }
    }
}
