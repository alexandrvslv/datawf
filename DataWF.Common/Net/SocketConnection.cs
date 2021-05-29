using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace DataWF.Common
{
    [InvokerGenerator(Instance = true)]
    public abstract partial class SocketConnection : DefaultItem, ISocketConnection
    {
        protected static readonly byte[] fin = new byte[] { 1, 60, 2, 102, 105, 110, 3, 62, 4 };
        protected static readonly int finLength = fin.Length;
        protected static readonly int finCacheHalf = fin.Length - 1;
        protected static readonly int finCacheLength = finCacheHalf * 2;

        internal static readonly ConcurrentQueue<Pipe> Pipes = new ConcurrentQueue<Pipe>();
        protected readonly BinarySerializer serializer = new BinarySerializer();
        protected ManualResetEventSlim sendEvent = new ManualResetEventSlim(true);
        protected ManualResetEventSlim loadEvent = new ManualResetEventSlim(true);
        protected bool disposed;

        public SocketConnection()
        {
            Stamp = DateTime.UtcNow;
        }
        public abstract bool Connected { get; }
        public string Name { get; set; }
        [JsonIgnore]
        public ISocketService Server { get; set; }
        public DateTime Stamp { get; set; }
        public virtual Uri Address { get; set; }

        public int ReceiveCount { get; set; }
        public long ReceiveLength { get; set; }
        public int SendCount { get; set; }
        public long SendLength { get; set; }
        public int SendErrors { get; set; }
        public string SendError { get; set; }
        public int SendingCount { get; internal set; }

        public abstract ValueTask Connect(Uri address);

        public abstract ValueTask Disconnect();

        public virtual void Dispose()
        {
            if (!disposed)
            {
                _ = Server.OnClientDisconect(new SocketConnectionArgs(this));
                loadEvent?.Dispose();
                sendEvent?.Dispose();
                disposed = true;
            }
        }

        public virtual Pipe GetPipe()
        {
            if (Pipes.TryDequeue(out var pipe))
            {
                return pipe;
            }
            //var options = new PipeOptions(
            //    minimumSegmentSize: 1024,
            //    pauseWriterThreshold: 32 * 1024,
            //    resumeWriterThreshold: 16 * 1024,
            //    useSynchronizationContext: false);
            return new Pipe();//options
        }

        public Task<bool> Send(byte[] data)
        {
            using (var stream = new MemoryStream(data))
            {
                return Send(stream);
            }
        }

        public async Task SendText(string text)
        {
            var buffer = Encoding.UTF8.GetBytes(text);
            using (var stream = new MemoryStream(buffer))
            {
                await Send(stream);
            }
        }

        public Task<bool> Send(Stream stream)
        {
            return Send(new SocketStreamArgs(this, SocketStreamMode.Send)
            {
                SourceStream = stream
            });
        }

        public async Task<bool> SendT<T>(T element)
        {
            var args = new SocketStreamArgs(this, SocketStreamMode.Send)
            {
                Pipe = GetPipe(),
                Tag = element
            };
            //Cache type information
            serializer.GetTypeInfo(element.GetType());

            _ = Task.Run(Serialize);

            return await Send(args);

            void Serialize()
            {
                try
                {
                    serializer.Serialize(args.WriterStream, element, args.Buffer.Count);
                    args.CompleteWrite();
                }
                catch (Exception ex)
                {
                    args.CompleteWrite(ex);
                }
            }
        }

        public virtual async Task<bool> Send(SocketStreamArgs arg)
        {
            if (Server.TransferTimeOut != default(TimeSpan))
                arg.CancellationToken = new CancellationTokenSource(Server.TransferTimeOut);
            if (!Connected)
            {
                throw new Exception("Socket is Discconected!");
            }
            try
            {
                sendEvent.Wait();
                sendEvent.Reset();
                SendingCount++;

                while (true)
                {
                    //buffering Compresison results
                    var read = await arg.ReadPipe();
                    if (read > 0)
                    {
                        int sended = await SendPart(arg, read);
                        if (arg.CancellationToken != null)
                        {
                            if (arg.CancellationToken.IsCancellationRequested)
                                throw new TimeoutException($"Timeout of sending message {Helper.SizeFormat(arg.Transfered)}");
                            arg.CancellationToken.CancelAfter(Server.TransferTimeOut);
                        }
                        arg.Transfered += sended;
                        arg.PartsCount++;
                        Stamp = DateTime.UtcNow;
                    }
                    else if (arg.Pipe == null || arg.WriterState == SocketStreamState.Complete)
                    {
                        break;
                    }
                }
                //Latency hack
                await SendFin(arg);
                arg.Transfered += finLength;

                SendCount++;
                SendLength += arg.Transfered;

                await arg.CompleteRead();

                _ = Server.OnSended(arg);
                return true;
            }
            catch (Exception ex)
            {
                SendErrors++;
                SendError = ex.Message;
                await arg.CompleteRead(ex);
                Server.OnDataException(new SocketExceptionArgs(arg, ex));
                return false;
            }
            finally
            {
                sendEvent.Set();
            }
        }

        protected abstract Task SendFin(SocketStreamArgs arg);

        protected abstract Task<int> SendPart(SocketStreamArgs arg, int read);

        public void WaitAll()
        {
            loadEvent.Wait();
            sendEvent.Wait();
        }

        public virtual async Task ListenerLoop()
        {
            while (Connected)
            {
                loadEvent.Wait();
                loadEvent.Reset();

                var arg = new SocketStreamArgs(this, SocketStreamMode.Receive)
                {
                    Pipe = GetPipe(),
                    FinCache = new Memory<byte>(new byte[finCacheLength])
                };

                await Load(arg);
            }
        }

        protected abstract Task<int> LoadPart(SocketStreamArgs arg);

        protected virtual async ValueTask Load(Memory<byte> buffer)
        {
            var arg = new SocketStreamArgs(this, SocketStreamMode.Receive)
            {
                Pipe = GetPipe(),
                FinCache = new Memory<byte>(new byte[finCacheLength])
            };
            arg.StartRead();

            bool isBreak = CheckFinBuffer(ref buffer, arg, out var setLoad);
            if (!buffer.IsEmpty)
            {
                arg.PartsCount++;
                arg.Transfered += buffer.Length;
#if NETSTANDARD2_0
                await arg.WriterStream.WriteAsync(buffer.ToArray(), 0, buffer.Length);
#else
                await arg.WriterStream.WriteAsync(buffer);
#endif
            }

            if (isBreak)
            {
                EndLoad(arg, setLoad);
            }
            else
            {
                _ = Load(arg);
            }
        }

        protected virtual async Task Load(SocketStreamArgs arg)
        {
            try
            {
                var setLoad = true;
                while (true)
                {
                    int read = await LoadPart(arg);
                    if (read > 0)
                    {
#if NETSTANDARD2_0
                        var slice = new Memory<byte>(arg.Buffer.Array, 0, read);
#else
                        var slice = memory.Length == read ? memory : memory.Slice(0, read);
#endif
                        if (arg.ReaderState == SocketStreamState.None)
                        {
                            arg.StartRead();
                        }
                        var isBreak = CheckFinBuffer(ref slice, arg, out setLoad);
                        if (!slice.IsEmpty)
                        {
                            arg.PartsCount++;
                            arg.Transfered += slice.Length;
#if NETSTANDARD2_0
                            await arg.WriterStream.WriteAsync(slice.ToArray(), 0, slice.Length);
#else
                            arg.Pipe.Writer.Advance(slice.Length);
                            var result = await arg.Pipe.Writer.FlushAsync();
#endif
                        }
                        if (isBreak)
                            break;
                    }
                    else
                    {
                        await Disconnect();
                        return;
                    }
                }
                EndLoad(arg, setLoad);
            }
            catch (Exception ex)
            {
                arg.CompleteWrite(ex);
                await arg.ReleasePipe(ex);
                Server.OnDataException(new SocketExceptionArgs(arg, ex));
            }
        }

        protected virtual void EndLoad(SocketStreamArgs arg, bool setLoad)
        {
            arg.CompleteWrite();
            Stamp = DateTime.UtcNow;
            _ = Server.OnReceiveFinish(arg);
            if (setLoad)
            {
                loadEvent.Set();
            }
        }

        protected bool CheckFinBuffer(ref Memory<byte> buffer, SocketStreamArgs arg, out bool setLoad)
        {
            setLoad = true;
            var index = buffer.Span.IndexOf(fin);
            if (index < 0)
            {
                buffer.Slice(0, Math.Min(finCacheHalf, buffer.Length)).Span.CopyTo(arg.FinCache.Slice(finCacheHalf).Span);

                index = arg.FinCache.Span.IndexOf(fin);
                if (index < 0)
                {
                    if (buffer.Length < finCacheHalf)
                    { }

                    var maxFinCache = Math.Min(finCacheHalf, buffer.Length);
                    var finCacheSlice = arg.FinCache.Slice(finCacheHalf - maxFinCache);
                    buffer.Span.Slice(buffer.Length - maxFinCache).CopyTo(finCacheSlice.Span);

                    setLoad = false;
                    return false;
                }
                else
                {
                    index -= finCacheHalf;
                }
            }

            var endIndex = index + finLength;
            if (endIndex < buffer.Length)
            {
                setLoad = false;
                var slice = buffer.Slice(endIndex).ToArray();
                _ = Task.Run(() => _ = Load(slice));
            }
            buffer = index > 0 ? buffer.Slice(0, index) : Memory<byte>.Empty;

            return true;
        }

    }
}
