﻿using System;
using System.Collections.Generic;
using System.IO;

namespace DataWF.Common
{
    public class XMLTextSerializer : BaseSerializer
    {
        public XMLTextSerializer()
        { }

        public XMLTextSerializer(Type type) : base(type)
        { }

        public bool CheckIFile { get; set; }

        public bool Indent { get; set; } = true;


        public override ISerializeWriter GetWriter(Stream stream)
        {
            return new XmlInvokerWriter(stream, this);
        }

        public override ISerializeReader GetReader(Stream stream)
        {
            return new XmlInvokerReader(stream, this);
        }
    }
}