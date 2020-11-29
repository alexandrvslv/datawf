using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataWF.Common
{
    public class TcpSocket : IDisposable
    {
        internal static readonly ConcurrentQueue<Pipe> Pipes = new ConcurrentQueue<Pipe>();
        private static readonly byte[] fin = new byte[] { 1, 60, 2, 102, 105, 110, 3, 62, 4 };
        private static readonly int finLength = fin.Length;
        private static readonly int finCacheHalf = fin.Length - 1;
        private static readonly int finCacheLength = finCacheHalf * 2;
        protected readonly BinarySerializer serializer = new BinarySerializer();
        protected ManualResetEventSlim sendEvent = new ManualResetEventSlim(true);
        protected ManualResetEventSlim loadEvent = new ManualResetEventSlim(true);
        private Socket socket;
        private bool disposed;

        public TcpSocket()
        {
            Stamp = DateTime.UtcNow;
        }

        public string PointName => Point.ToString();

        public DateTime Stamp { get; set; }

        public TcpServer Server { get; set; }

        public Socket Socket
        {
            get { return socket; }
            set
            {
                socket = value;
                if (socket != null)
                {
                    socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    if (socket.RemoteEndPoint is IPEndPoint point)
                    {
                        Point = point;
                    }
                }
            }
        }

        public IPEndPoint Point { get; set; }

        public IPEndPoint LocalPoint { get { return socket == null ? null : (IPEndPoint)socket.LocalEndPoint; } }

        public Pipe GetPipe()
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

        public async Task<bool> SendElement<T>(T element)
        {
            var args = new TcpStreamEventArgs(this, TcpStreamMode.Send)
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

        public Task<bool> Send(byte[] data)
        {
            using (var stream = new MemoryStream(data))
            {
                return Send(stream);
            }
        }

        public Task<bool> Send(Stream stream)
        {
            return Send(new TcpStreamEventArgs(this, TcpStreamMode.Send)
            {
                SourceStream = stream
            });
        }

        public async Task<bool> Send(TcpStreamEventArgs arg)
        {
            if (!(Socket?.Connected ?? false))
            {
                throw new Exception("Socket is Discconected!");
            }
            try
            {
                sendEvent.Wait();
                sendEvent.Reset();

                while (true)
                {
                    //buffering Compresison results
                    var read = await arg.ReadPipe();
                    if (read > 0)
                    {
#if NETSTANDARD2_0
                        var sended = await Task.Factory.FromAsync<int>(Socket.BeginSend(arg.Buffer.Array, 0, read, SocketFlags.None, null, arg), Socket.EndSend);
#else
                        var sended = await Socket.SendAsync(arg.Buffer.Slice(0, read), SocketFlags.None);
#endif
                        //Debug.WriteLine($"TcpClient {Point} Send {sended}");
                        arg.Transfered += sended;
                        arg.PartsCount++;
                        Stamp = DateTime.UtcNow;
                    }
                    else if (arg.Pipe == null || arg.WriterState == TcpStreamState.Complete)
                    {
                        break;
                    }
                    else
                    { }
                }
                //Latency hack
                Socket.NoDelay = true;
#if NETSTANDARD2_0
                await Task.Factory.FromAsync<int>(Socket.BeginSend(fin, 0, finLength, SocketFlags.None, null, arg), Socket.EndSend);
#else
                await Socket.SendAsync(fin, SocketFlags.None);
#endif
                Socket.NoDelay = false;
                arg.Transfered += finLength;

                arg.CompleteRead();
                await arg.ReleasePipe();

                _ = Server.OnDataSend(arg);
                return true;
            }
            catch (Exception ex)
            {
                arg.CompleteRead(ex);
                await arg.ReleasePipe(ex);
                Server.OnDataException(new TcpExceptionEventArgs(arg, ex));
                return false;
            }
            finally
            {
                sendEvent.Set();
            }
        }

        public async Task ListenerLoop()
        {
            while (Socket?.Connected ?? false)
            {
                loadEvent.Wait();
                loadEvent.Reset();

                var arg = new TcpStreamEventArgs(this, TcpStreamMode.Receive)
                {
                    Pipe = GetPipe(),
                    FinCache = new Memory<byte>(new byte[finCacheLength])
                };

                await Load(arg);
            }
        }

        private bool CheckFinBuffer(ref Memory<byte> buffer, TcpStreamEventArgs arg, out bool setLoad)
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

        public static Span<byte> Concat(ReadOnlySpan<byte> s1, ReadOnlySpan<byte> s2)
        {
            var array = new byte[s1.Length + s2.Length];
            s1.CopyTo(array);
            s2.CopyTo(array.AsSpan(s1.Length));
            return array;
        }

        private async ValueTask Load(Memory<byte> buffer)
        {
            var arg = new TcpStreamEventArgs(this, TcpStreamMode.Receive)
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

#if NETSTANDARD2_0
        private async Task Load(TcpStreamEventArgs arg)
        {
            try
            {
                var setLoad = true;
                while (true)
                {
                    var read = await Task.Factory.FromAsync(Socket.BeginReceive(arg.Buffer.Array, 0, TcpStreamEventArgs.BufferSize, SocketFlags.None, null, arg), Socket.EndReceive);
                    if (read > 0)
                    {
                        var slice = new Memory<byte>(arg.Buffer.Array, 0, read);
                        if (arg.ReaderState == TcpStreamState.None)
                        {
                            arg.StartRead();
                        }
                        var isBreak = CheckFinBuffer(ref slice, arg, out setLoad);
                        if (!slice.IsEmpty)
                        {
                            arg.PartsCount++;
                            arg.Transfered += slice.Length;
                            await arg.WriterStream.WriteAsync(slice.ToArray(), 0, slice.Length);
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
                Server.OnDataException(new TcpExceptionEventArgs(arg, ex));
            }
        }
#else
        private async Task Load(TcpStreamEventArgs arg)
        {
            try
            {
                var setLoad = true;
                while (true)
                {
                    var memory = arg.Pipe.Writer.GetMemory(arg.Buffer.Count + finLength);
                    var read = await Socket.ReceiveAsync(memory, SocketFlags.None);
                    if (read > 0)
                    {
                        var slice = memory.Length == read ? memory : memory.Slice(0, read);

                        if (arg.ReaderState == TcpStreamState.None)
                        {
                            arg.StartRead();
                        }
                        var isBreak = CheckFinBuffer(ref slice, arg, out setLoad);
                        if (!slice.IsEmpty)
                        {
                            arg.PartsCount++;
                            arg.Transfered += slice.Length;
                            arg.Pipe.Writer.Advance(slice.Length);
                            var result = await arg.Pipe.Writer.FlushAsync();
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
                Server.OnDataException(new TcpExceptionEventArgs(arg, ex));
            }
        }
#endif
        private void EndLoad(TcpStreamEventArgs arg, bool setLoad)
        {
            arg.CompleteWrite();
            Stamp = DateTime.UtcNow;
            _ = Server.OnDataLoadEnd(arg);
            if (setLoad)
            {
                loadEvent.Set();
            }
        }

        public async ValueTask Connect(IPEndPoint address, bool attachToServer = true)
        {
            if (Socket == null)
            {
                Socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                if (Point == null)
                    throw new Exception("Point not Specified");
                Socket.Bind(Point);
            }
            if (!Socket.Connected)
            {
                var arg = new TcpSocketEventArgs { TcpSocket = this };
                try
                {
                    Debug.WriteLine($"TcpClient {Point} Connect to {address}");
                    await Task.Factory.FromAsync(Socket.BeginConnect(address, null, arg), Socket.EndDisconnect);
                    Stamp = DateTime.UtcNow;
                    if (attachToServer)
                    {
                        _ = Server.OnClientConnect(arg);
                    }
                }
                catch (Exception ex)
                {
                    Server.OnDataException(new TcpExceptionEventArgs(arg, ex));
                    throw;
                }
            }
        }

        public async ValueTask Disconnect()
        {
            if (Socket?.Connected ?? false)
            {
                var arg = new TcpSocketEventArgs { TcpSocket = this };
                try
                {
                    Debug.WriteLine($"TcpClient {Point} Disconnect");
                    Socket.Shutdown(SocketShutdown.Both);
                    await Task.Factory.FromAsync(Socket.BeginDisconnect(true, null, arg), Socket.EndDisconnect);
                    Socket.Close();
                    Socket.Dispose();
                    Socket = null;
                    loadEvent.Set();
                    sendEvent.Set();
                    _ = Server.OnClientDisconect(arg);
                }
                catch (Exception ex)
                {
                    Server.OnDataException(new TcpExceptionEventArgs(arg, ex));
                }
            }
        }

        public void WaitAll()
        {
            loadEvent.Wait();
            sendEvent.Wait();
        }

        public void Dispose()
        {
            if (!disposed)
            {
                Debug.WriteLine($"TcpClient {Point} Dispose");
                if (Socket?.Connected ?? false)
                {
                    Debug.WriteLine($"TcpClient {Point} Dispose Disconnect");
                    Socket.Shutdown(SocketShutdown.Both);
                    Socket.Disconnect(true);
                    Socket.Close();
                    Socket.Dispose();
                    Socket = null;
                    loadEvent.Set();
                    sendEvent.Set();
                    _ = Server.OnClientDisconect(new TcpSocketEventArgs { TcpSocket = this });
                }

                loadEvent?.Dispose();
                sendEvent?.Dispose();
                disposed = true;
            }
        }
    }
}
