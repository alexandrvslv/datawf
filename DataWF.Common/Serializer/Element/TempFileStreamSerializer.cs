using System;
using System.Collections.Generic;
using System.IO;

namespace DataWF.Common
{
    public class TempFileStreamSerializer : ObjectSerializer<Stream>
    {
        public static readonly TempFileStreamSerializer Instance = new TempFileStreamSerializer();
        public static readonly byte[] endOfStream = new byte[] { 1, (byte)'e', 2, 1, (byte)'o', 2, 1, (byte)'s', 2 };
        private readonly int bufferSize;

        public TempFileStreamSerializer(int bufferSize = 8 * 1024)
        {
            this.bufferSize = bufferSize;
        }

        public override bool CanConvertString => false;

        public override Stream Read(BinaryInvokerReader reader, Stream value, TypeSerializeInfo typeInfo, Dictionary<ushort, IPropertySerializeInfo> map)
        {
            var token = reader.ReadToken();
            if (token == BinaryToken.Null)
            {
                return null;
            }

            if (token == BinaryToken.ObjectBegin)
            {
                token = reader.ReadToken();
            }
            if (token == BinaryToken.SchemaBegin)
            {
                map = reader.ReadType(out typeInfo);
                token = reader.ReadToken();
            }

            if (value == null)
            {
                value = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite, bufferSize);
            }
            if (token == BinaryToken.ObjectEntry)
            {
                var property = reader.ReadSchemaIndex();
                if (property == 0)
                {
                    var length = reader.Reader.ReadInt64();
                    reader.ReadToken();
                    reader.ReadSchemaIndex();
                    ReadByLength(reader, value, length);
                }
                else
                {
                    ReadBySeparator(reader, value);
                }
                value.Position = 0;
            }
            return value;
        }

        private void ReadByLength(BinaryInvokerReader reader, Stream value, long length)
        {
            var buffer = new byte[bufferSize];
            var read = 0;
            var readTotal = 0L;
            while (readTotal < length
                && (read = reader.Reader.Read(buffer, 0, Math.Min(bufferSize, (int)(length - readTotal)))) > 0)
            {
                value.Write(buffer, 0, read);
                readTotal += read;
            }
        }

        private void ReadBySeparator(BinaryInvokerReader reader, Stream value)
        {
            var buffer = new byte[bufferSize];
            int read = 0;
            while ((read = reader.Reader.Read(buffer, 0, bufferSize)) > 0)
            {
                var index = buffer.AsSpan(0, read).IndexOf(endOfStream);
                if (index < 0)
                {
                    value.Write(buffer, 0, read);
                }
                else
                {
                    if (index > 0)
                    {
                        value.Write(buffer, 0, index);
                        if (reader.Reader.BaseStream.CanSeek)
                        {
                            reader.Reader.BaseStream.Position -= read - (index + endOfStream.Length);
                        }
                    }
                    break;
                }
            }
        }

        public override void Write(BinaryInvokerWriter writer, Stream value, TypeSerializeInfo typeInfo, Dictionary<ushort, IPropertySerializeInfo> map)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }
            writer.WriteObjectBegin();

            writer.WriteSchemaBegin();
            writer.WriteSchemaName(nameof(FileStream));
            writer.WriteSchemaEnd();
            if (value.CanSeek)
            {
                writer.WriteObjectEntry();
                writer.WriteSchemaIndex(0);
                writer.Writer.Write(value.Length);
            }
            writer.WriteObjectEntry();
            writer.WriteSchemaIndex(1);
            var buffer = new byte[bufferSize];
            int read = 0;
            while ((read = value.Read(buffer, 0, bufferSize)) > 0)
            {
                writer.Writer.Write(buffer, 0, read);
            }
            if (!value.CanSeek)
            {
                writer.Writer.Write(endOfStream);
            }
            writer.WriteObjectEnd();
        }
    }
}
