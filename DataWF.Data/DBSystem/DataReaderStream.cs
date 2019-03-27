using System;
using System.Data;
using System.IO;

namespace DataWF.Data
{
    public class DataReaderStream : Stream
    {
        private long position = 0;

        public DataReaderStream(IDataReader reader)
        {
            Reader = reader;
        }

        public IDataReader Reader { get; }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotImplementedException();

        public override long Position { get => position; set => throw new NotImplementedException(); }

        public override void Flush()
        { }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var readed = Reader.GetBytes(0, position, buffer, offset, count);
            position += readed;
            return (int)readed;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Reader?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
