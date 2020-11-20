using System;
using System.IO;
using System.IO.Compression;
using System.IO.Pipelines;
using System.Net;

namespace DataWF.Common
{
    public class TcpStreamEventArgs : EventArgs
    {
        public static readonly int BufferSize = 2048;
        private Pipe pipe;
        private Stream readerStream;
        private Stream writerStream;
        private Stream sourceStream;

        public TcpStreamEventArgs(TcpSocket client, TcpStreamMode mode)
        {
            Buffer = new byte[BufferSize];
            Client = client;
            Mode = mode;
        }

        public TcpStreamMode Mode { get; }

        public byte[] Buffer { get; set; }

        public TcpSocket Client { get; }

        public Pipe Pipe
        {
            get => pipe;
            internal set
            {
                pipe = value;
                if (pipe != null)
                {
                    ReaderStream = pipe.Reader.AsStream(true);
                    WriterStream = pipe.Writer.AsStream(true);

                    if (Mode == TcpStreamMode.Receive && Client.Server.Compression)
                        ReaderStream = new Brotli.BrotliStream(ReaderStream, CompressionMode.Decompress, true);

                    if (Mode == TcpStreamMode.Send && Client.Server.Compression)
                        WriterStream = new Brotli.BrotliStream(WriterStream, CompressionMode.Compress, true);
                }
            }
        }

        public Stream WriterStream
        {
            get => writerStream;
            internal set
            {
                writerStream = value;
            }
        }

        public Stream ReaderStream
        {
            get => readerStream;
            internal set
            {
                readerStream = value;
            }
        }

        public object Tag { get; set; }

        public int Transfered { get; internal set; }

        public int PackageCount { get; internal set; }

        public Stream SourceStream
        {
            get => sourceStream;
            internal set
            {
                sourceStream = value;
                if (Mode == TcpStreamMode.Receive)
                {
                    if (Client.Server.Compression)
                        WriterStream = new Brotli.BrotliStream(sourceStream, CompressionMode.Compress);
                    else
                        WriterStream = sourceStream;
                }
                else
                {
                    if (Client.Server.Compression)
                        ReaderStream = new Brotli.BrotliStream(sourceStream, CompressionMode.Compress);
                    else
                        ReaderStream = sourceStream;
                }
            }
        }

        public void ReleasePipe()
        {
            if (Pipe != null)
            {
                WriterStream?.Dispose();
                ReaderStream?.Dispose();

                Pipe.Writer.Complete();
                Pipe.Reader.Complete();

                Pipe.Reset();
                TcpSocket.Pipes.Push(Pipe);
            }
        }
    }

    public enum TcpStreamMode
    {
        Send,
        Receive
    }
}
