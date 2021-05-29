using System;
using System.Buffers;
using System.IO;
using System.IO.Compression;
using System.IO.Pipelines;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace DataWF.Common
{
    public class SocketStreamArgs : SocketConnectionArgs
    {
        private Pipe pipe;
        private Stream pipeReaderStream;
        private Stream pipeWriterStream;
        private Stream sourceStream;

        public SocketStreamArgs(ISocketConnection connection, SocketStreamMode mode)
            : base(connection)
        {
            Mode = mode;
            Buffer = new ArraySegment<byte>(new byte[connection.Server.BufferSize]);
        }

        public SocketStreamMode Mode { get; }

        public ArraySegment<byte> Buffer { get; private set; }

        public CancellationTokenSource CancellationToken { get; set; }

        public SocketStreamState ReaderState { get; private set; }

        public SocketStreamState WriterState { get; private set; }

        public Memory<byte> FinCache { get; internal set; }

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

                    if (Mode == SocketStreamMode.Receive)
                    {
                        WriterStream = pipeWriterStream;
                        switch (Connection.Server.Compression)
                        {
                            case SocketCompressionMode.Brotli:
#if NETSTANDARD2_0
                                ReaderStream = new Brotli.BrotliStream(pipeReaderStream, CompressionMode.Decompress, true);
#else
                                ReaderStream = new BrotliStream(pipeReaderStream, CompressionMode.Decompress, true);
#endif
                                break;
                            case SocketCompressionMode.GZip:
                                ReaderStream = new GZipStream(pipeReaderStream, CompressionMode.Decompress, true);
                                break;
                            default:
                                ReaderStream = pipeReaderStream;
                                break;
                        }
                    }
                    if (Mode == SocketStreamMode.Send)
                    {
                        ReaderStream = pipeReaderStream;
                        switch (Connection.Server.Compression)
                        {
                            case SocketCompressionMode.Brotli:
#if NETSTANDARD2_0
                                WriterStream = new Brotli.BrotliStream(pipeWriterStream, CompressionMode.Compress, true);
#else
                                WriterStream = new BrotliStream(pipeWriterStream, CompressionLevel.Fastest, true);
#endif
                                break;
                            case SocketCompressionMode.GZip:
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

                if (Mode == SocketStreamMode.Receive)
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

        public async Task CompleteRead(Exception exception = null)
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
                ReaderState = SocketStreamState.Complete;
                await ReleasePipe();
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
                WriterState = SocketStreamState.Complete;
            }

        }

        public async ValueTask ReleasePipe(Exception exception = null)
        {
            while (WriterState != SocketStreamState.Complete)
            {
                await Task.Delay(5);
            }

            while (ReaderState != SocketStreamState.Complete)
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
                SocketConnection.Pipes.Enqueue(Pipe);
            }
        }

        public void StartRead()
        {
            if (ReaderState != SocketStreamState.None)
                throw new Exception("Reader Wrong State");
            if (Mode == SocketStreamMode.Receive)
            {
                Task.Factory.StartNew(p => Connection.Server.OnReceiveStart((SocketStreamArgs)p), this, TaskCreationOptions.PreferFairness);
                ReaderState = SocketStreamState.Started;
            }
        }
    }

    public enum SocketStreamMode
    {
        Send,
        Receive
    }

    public enum SocketStreamState
    {
        None = 0,
        Started,
        Complete
    }
}
