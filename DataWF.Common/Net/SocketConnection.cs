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
        protected static readonly byte[] fin = new byte[] { 1, 2, 4, 8, 16, 32, 64, 128, 64, 32, 16, 8, 4, 2, 1 };
        protected static readonly int finLength = fin.Length;
        protected static readonly int finCacheHalf = fin.Length * 2;
        protected static readonly int finCacheLength = finCacheHalf * 2;

        internal static readonly ConcurrentQueue<Pipe> Pipes = new ConcurrentQueue<Pipe>();
        protected readonly BinarySerializer serializer = new BinarySerializer();
        protected ManualResetEventSlim sendEvent = new ManualResetEventSlim(true);
        protected ManualResetEventSlim receiveEvent = new ManualResetEventSlim(true);
        protected bool disposed;
        private string name;
        private DateTime stamp;
        private Uri address;

        public SocketConnection()
        {
            Stamp = DateTime.UtcNow;
        }

        public Func<SocketStreamArgs, ValueTask> ReceiveStart { get; set; }

        public abstract bool Connected { get; }
        public string Name
        {
            get => name;
            set
            {
                name = value;
                OnPropertyChanged();
            }
        }

        [JsonIgnore]
        public ISocketService Server { get; set; }
        public DateTime Stamp
        {
            get => stamp;
            set
            {
                stamp = value;
                OnPropertyChanged();
            }
        }

        public virtual Uri Address
        {
            get => address;
            set
            {
                address = value;
                Name = value?.ToString();
                OnPropertyChanged();
            }
        }

        public int ReceiveCount { get; set; }
        public long ReceiveLength { get; set; }
        public int SendCount { get; set; }
        public long SendLength { get; set; }
        public int SendErrors { get; set; }
        public string SendError { get; set; }
        public int SendingCount { get; internal set; }

        public abstract ValueTask Connect();

        public abstract ValueTask Disconnect();

        public abstract void OnTimeOut();

        public virtual void Dispose()
        {
            if (!disposed)
            {
                _ = Server.OnClientDisconect(new SocketConnectionArgs(this));
                receiveEvent?.Dispose();
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

        public async Task Send(string text)
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
                    serializer.Serialize(args.WriterStream, element, Server.BufferSize);
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
            if (!Connected)
            {
                throw new Exception("Socket is Discconected!");
            }
            try
            {
                sendEvent.Wait();
                sendEvent.Reset();
                SendingCount++;
                if (Server.TransferTimeout != default(TimeSpan))
                {
                    arg.CancellationToken = new CancellationTokenSource(Server.TransferTimeout);
                }
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
                            arg.CancellationToken.CancelAfter(Server.TransferTimeout);
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
            receiveEvent.Wait();
            sendEvent.Wait();
        }

        public virtual async Task ListenerLoop()
        {
            while (Connected)
            {
                receiveEvent.Wait();
                receiveEvent.Reset();

                var arg = new SocketStreamArgs(this, SocketStreamMode.Receive)
                {
                    Pipe = GetPipe(),
                    FinCache = new Memory<byte>(new byte[finCacheLength])
                };

                await Receive(arg);
            }
        }

        protected abstract Task<int> ReceivePart(SocketStreamArgs arg);

        protected virtual async ValueTask Receive(Memory<byte> buffer)
        {
            var arg = new SocketStreamArgs(this, SocketStreamMode.Receive)
            {
                Pipe = GetPipe(),
                FinCache = new Memory<byte>(new byte[finCacheLength]),
            };
            CacheFinHalf(buffer, arg);
#if !NETSTANDARD2_0
            var memory = arg.Pipe.Writer.GetMemory(buffer.Length);
            buffer.Span.CopyTo(memory.Span);
            buffer = memory.Slice(0, buffer.Length);
#endif
            await Receive(arg, buffer, true);
        }

        protected virtual async ValueTask<bool> Receive(SocketStreamArgs arg, Memory<byte> buffer, bool fromBuffer = false)
        {
            if (arg.ReaderState == SocketStreamState.None)
            {
                arg.StartRead();
                arg.Receiver = Task.Factory.StartNew(p => OnReceiveStart((SocketStreamArgs)p), arg, TaskCreationOptions.PreferFairness);
            }
            bool isBreak = CheckFinBuffer(ref buffer, arg, out var setReceiver);
            if (!buffer.IsEmpty)
            {
                arg.PartsCount++;
                arg.Transfered += buffer.Length;
#if NETSTANDARD2_0
                await arg.WriterStream.WriteAsync(buffer.ToArray(), 0, buffer.Length);
#else
                arg.Pipe.Writer.Advance(buffer.Length);
                var result = await arg.Pipe.Writer.FlushAsync();
#endif
            }

            if (isBreak)
            {
                OnReceiveFinish(arg, setReceiver);
            }
            else if (fromBuffer)
            {
                _ = Receive(arg);
            }
            return isBreak;
        }

        protected virtual async Task Receive(SocketStreamArgs arg)
        {
            try
            {
                while (true)
                {
                    int read = await ReceivePart(arg);
                    if (read > 0)
                    {
                        Stamp = DateTime.UtcNow;
#if NETSTANDARD2_0
                        var buffer = new Memory<byte>(arg.Buffer.Array, 0, arg.BufferSize);
#else
                        var memory = arg.Pipe.Writer.GetMemory(arg.BufferSize);
                        var buffer = memory.Length == read ? memory : memory.Slice(0, read);
#endif
                        if (await Receive(arg, buffer))
                            break;
                    }
                    else
                    {
                        await Disconnect();
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                arg.CompleteWrite(ex);
                await arg.ReleasePipe(ex);
                Server.OnDataException(new SocketExceptionArgs(arg, ex));
            }
        }

        protected virtual async Task OnReceiveStart(SocketStreamArgs args)
        {
            try
            {
                if (ReceiveStart != null)
                {
                    await ReceiveStart(args);
                }
                else if (Server != null)
                {
                    await Server.OnReceiveStart(args);
                }
                else
                {
                    await Task.Delay(1);
                    throw new Exception("No data load listener specified!");
                }
                await args.CompleteRead();
            }
            catch (Exception ex)
            {
                await args.CompleteRead(ex);
            }
        }

        protected virtual void OnReceiveFinish(SocketStreamArgs arg, bool setLoad)
        {
            arg.CompleteWrite();

            _ = Server.OnReceiveFinish(arg);
            if (setLoad)
            {
                receiveEvent.Set();
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
                    CacheFinHalf(buffer, arg);

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
                _ = Task.Run(() => _ = Receive(slice));
            }
            buffer = index > 0 ? buffer.Slice(0, index) : Memory<byte>.Empty;

            return true;
        }

        private static void CacheFinHalf(Memory<byte> buffer, SocketStreamArgs arg)
        {
            var maxFinCache = Math.Min(finCacheHalf, buffer.Length);
            var finCacheSlice = arg.FinCache.Slice(finCacheHalf - maxFinCache);
            buffer.Span.Slice(buffer.Length - maxFinCache).CopyTo(finCacheSlice.Span);
        }
    }
}
