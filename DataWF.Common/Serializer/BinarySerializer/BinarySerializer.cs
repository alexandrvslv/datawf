using System;
using System.Collections.Generic;
using System.IO;

namespace DataWF.Common
{
    public class BinarySerializer : BaseSerializer
    {
        public static BinarySerializer Instance { get; } = new BinarySerializer();

        public BinarySerializer()
        { }

        public BinarySerializer(Type type) : base(type)
        { }

        public bool WriteSchema { get; set; } = true;
        public bool TypeShortName { get; set; }

        public override ISerializeWriter GetWriter(Stream stream)
        {
            return new BinaryInvokerWriter(stream, this);
        }

        public override ISerializeReader GetReader(Stream stream)
        {
            return new BinaryInvokerReader(stream, this);
        }
    }


}
