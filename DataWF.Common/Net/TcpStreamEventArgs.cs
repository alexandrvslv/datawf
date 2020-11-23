using System;
using System.IO;
using System.IO.Compression;
using System.IO.Pipelines;
using System.Net;
using System.Threading.Tasks;

namespace DataWF.Common
{
    public class TcpStreamEventArgs : TcpSocketEventArgs
    {
        public static readonly int BufferSize = 2048;
        private Pipe pipe;
        private Stream pipeReaderStream;
        private Stream pipeWriterStream;
        private Stream sourceStream;

        public TcpStreamEventArgs(TcpSocket client, TcpStreamMode mode)
        {
            Buffer = new byte[BufferSize];
            Client = client;
            Mode = mode;
        }
        public TcpStreamMode Mode { get; }

        public byte[] Buffer { get; set; }

        public Pipe Pipe
        {
            get => pipe;
            internal set
            {
                pipe = value;
                if (pipe != null)
                {
                    pipeReaderStream = pipe.Reader.AsStream(true);
                    pipeWriterStream = pipe.Writer.AsStream(true);

                    if (Mode == TcpStreamMode.Receive && Client.Server.Compression)
                        ReaderStream = new Brotli.BrotliStream(pipeReaderStream, CompressionMode.Decompress, true);
                    else
                        ReaderStream = pipeReaderStream;

                    if (Mode == TcpStreamMode.Send && Client.Server.Compression)
                        WriterStream = new Brotli.BrotliStream(pipeWriterStream, CompressionMode.Compress, true);
                    else
                        WriterStream = pipeWriterStream;
                }
            }
        }

        public Stream WriterStream { get; set; }

        public Stream ReaderStream { get; set; }

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

        public bool IsReaderComplete { get; private set; }

        public bool IsWriteComplete { get; private set; }

        public void CompleteRead(Exception exception = null)
        {
            try
            {
                if (ReaderStream is Brotli.BrotliStream brotliReader)
                {
                    brotliReader.Dispose();
                    ReaderStream = null;
                }
                if (Pipe != null)
                {
                    Pipe.Reader.Complete(exception);
                }
            }
            finally
            {
                IsReaderComplete = true;
            }
        }

        public void CompleteWrite(Exception exception = null)
        {
            try
            {
                WriterStream.Flush();
                if (WriterStream is Brotli.BrotliStream brotliWriter)
                {
                    brotliWriter.Dispose();
                    WriterStream = null;
                }
                if (Pipe != null)
                {
                    Pipe.Writer.Complete(exception);
                }
            }
            finally
            {
                IsWriteComplete = true;
            }

        }

        public async ValueTask ReleasePipe(Exception exception = null)
        {
            while (!IsWriteComplete)
            {
                await Task.Delay(5);
            }

            if (WriterStream is Brotli.BrotliStream brotliWriter)
            {
                brotliWriter.Dispose();
                WriterStream = null;
            }
            if (ReaderStream is Brotli.BrotliStream brotliReader)
            {
                brotliReader.Dispose();
            }
            pipeWriterStream?.Dispose();
            pipeReaderStream?.Dispose();

            if (Pipe != null)
            {
                Pipe.Reset();
                TcpSocket.Pipes.Enqueue(Pipe);
            }
        }
    }

    public enum TcpStreamMode
    {
        Send,
        Receive
    }
}
