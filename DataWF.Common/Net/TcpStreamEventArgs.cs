using System;
using System.Buffers;
using System.IO;
using System.IO.Compression;
using System.IO.Pipelines;
using System.Net;
using System.Threading.Tasks;

namespace DataWF.Common
{
    public class TcpStreamEventArgs : TcpSocketEventArgs
    {
        public static readonly int BufferSize = 8 * 1024;
        private Pipe pipe;
        private Stream pipeReaderStream;
        private Stream pipeWriterStream;
        private Stream sourceStream;

        public TcpStreamEventArgs(TcpSocket client, TcpStreamMode mode)
        {
            Mode = mode;
            TcpSocket = client;
            Buffer = new ArraySegment<byte>(new byte[BufferSize]);
        }

        public TcpStreamMode Mode { get; }

        public ArraySegment<byte> Buffer { get; private set; }

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

                    if (Mode == TcpStreamMode.Receive)
                    {
                        WriterStream = pipeWriterStream;
                        switch (TcpSocket.Server.Compression)
                        {
                            case TcpServerCompressionMode.Brotli:
#if NETSTANDARD2_0
                                ReaderStream = new Brotli.BrotliStream(pipeReaderStream, CompressionMode.Decompress, true);
#else
                                ReaderStream = new BrotliStream(pipeReaderStream, CompressionMode.Decompress, true);
#endif
                                break;
                            case TcpServerCompressionMode.GZip:
                                ReaderStream = new GZipStream(pipeReaderStream, CompressionMode.Decompress, true);
                                break;
                            default:
                                ReaderStream = pipeReaderStream;
                                break;
                        }
                    }
                    if (Mode == TcpStreamMode.Send)
                    {
                        ReaderStream = pipeReaderStream;
                        switch (TcpSocket.Server.Compression)
                        {
                            case TcpServerCompressionMode.Brotli:
#if NETSTANDARD2_0
                                WriterStream = new Brotli.BrotliStream(pipeWriterStream, CompressionMode.Compress, true);
#else
                                WriterStream = new BrotliStream(pipeWriterStream, CompressionLevel.Fastest, true);
#endif
                                break;
                            case TcpServerCompressionMode.GZip:
                                WriterStream = new GZipStream(pipeWriterStream, CompressionLevel.Fastest, true);
                                break;
                            default:
                                WriterStream = pipeWriterStream;
                                break;
                        }
                    }
                }
            }
        }

        public Stream WriterStream { get; private set; }

        public Stream ReaderStream { get; private set; }

        public object Tag { get; set; }

        public int Transfered { get; internal set; }

        public int PartsCount { get; internal set; }

        public Stream SourceStream
        {
            get => sourceStream;
            internal set
            {
                sourceStream = value;

                if (Mode == TcpStreamMode.Receive)
                {
                    //if (Client.Server.Compression)
                    //    WriterStream = new Brotli.BrotliStream(sourceStream, CompressionMode.Compress);
                    //else
                    WriterStream = sourceStream;
                }
                else
                {
                    //if (Client.Server.Compression)
                    //    ReaderStream = new Brotli.BrotliStream(sourceStream, CompressionMode.Compress);
                    //else
                    ReaderStream = sourceStream;
                }
            }
        }

        public TcpStreamState ReaderState { get; private set; }

        public TcpStreamState WriterState { get; private set; }

        public Memory<byte> FinCache { get; internal set; }

        internal Task<int> ReadStream()
        {
            return ReaderStream.ReadAsync(Buffer.Array, 0, Buffer.Count);
        }

        //Buffered Pipe reader
        internal async ValueTask<int> ReadPipe()
        {
            int read = 0;
            do
            {
                var result = await Pipe.Reader.ReadAsync();
                var buffer = result.Buffer;
                var bufferLength = buffer.Length;
                var consumed = buffer.Start;
                if (bufferLength != 0)
                {
                    var canRead = Buffer.Count - read;
                    var toRead = (int)Math.Min(canRead, bufferLength);

                    var slize = toRead == bufferLength ? buffer : buffer.Slice(0, toRead);
                    slize.CopyTo(Buffer.AsSpan(read, toRead));
                    read += toRead;
                    consumed = slize.End;
                }
                Pipe.Reader.AdvanceTo(consumed);
                if (result.IsCompleted)
                {
                    break;
                }
            }
            while (read < Buffer.Count);

            return read;
        }

        public void CompleteRead(Exception exception = null)
        {
            try
            {
                if (ReaderStream != pipeReaderStream)
                {
                    ReaderStream?.Dispose();
                    ReaderStream = null;
                }
                else
                {
                    pipeReaderStream?.Flush();
                }
                if (Pipe != null)
                {
                    Pipe.Reader.Complete(exception);
                }
            }
            finally
            {
                ReaderState = TcpStreamState.Complete;
            }
        }

        public void CompleteWrite(Exception exception = null)
        {
            try
            {
                if (WriterStream != pipeWriterStream)
                {
                    WriterStream?.Dispose();
                    WriterStream = null;
                }
                else
                {
                    pipeWriterStream?.Flush();
                }
                if (Pipe != null)
                {
                    Pipe.Writer.Complete(exception);
                }
            }
            finally
            {
                WriterState = TcpStreamState.Complete;
            }

        }

        public async ValueTask ReleasePipe(Exception exception = null)
        {
            while (WriterState != TcpStreamState.Complete)
            {
                await Task.Delay(5);
            }

            while (ReaderState != TcpStreamState.Complete)
            {
                await Task.Delay(5);
            }

            if (WriterStream != pipeWriterStream)
            {
                WriterStream?.Dispose();
                WriterStream = null;
            }
            if (ReaderStream != pipeReaderStream)
            {
                ReaderStream?.Dispose();
                ReaderStream = null;
            }
            pipeWriterStream?.Dispose();
            pipeReaderStream?.Dispose();

            if (Pipe != null)
            {
                Pipe.Reset();
                TcpSocket.Pipes.Enqueue(Pipe);
            }
        }

        public void StartRead()
        {
            if (ReaderState != TcpStreamState.None)
                throw new Exception("Reader Wrong State");
            if (Mode == TcpStreamMode.Receive)
            {
                Task.Factory.StartNew(p => TcpSocket.Server.OnDataLoadStart((TcpStreamEventArgs)p), this, TaskCreationOptions.PreferFairness);
                ReaderState = TcpStreamState.Started;
            }
        }
    }

    public enum TcpStreamMode
    {
        Send,
        Receive
    }

    public enum TcpStreamState
    {
        None = 0,
        Started,
        Complete
    }
}
