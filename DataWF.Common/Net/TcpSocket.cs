using System;
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
        private static readonly byte[] fin = Encoding.ASCII.GetBytes("<finito>");
        private static readonly int finLength = fin.Length;
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

        public async Task SendElement<T>(T element)
        {
            var args = new TcpStreamEventArgs(this, TcpStreamMode.Send)
            {
                Pipe = GetPipe(),
                Tag = element
            };
            //Cache type information
            serializer.GetTypeInfo(element.GetType());

            _ = Task.Run(Serialize);

            await Send(args);

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

        public Task Send(byte[] data)
        {
            using (var stream = new MemoryStream(data))
            {
                return Send(stream);
            }
        }

        public Task Send(Stream stream)
        {
            return Send(new TcpStreamEventArgs(this, TcpStreamMode.Send)
            {
                SourceStream = stream
            });
        }

        public async Task Send(TcpStreamEventArgs arg)
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
                    var read = await arg.ReadStream();
                    if (read > 0)
                    {
                        var sended = await Task.Factory.FromAsync<int>(Socket.BeginSend(arg.Buffer.Array, 0, read, SocketFlags.None, null, arg), Socket.EndSend);
                        //Debug.WriteLine($"TcpClient {Point} Send {sended}");
                        arg.Transfered += sended;
                        arg.PartsCount++;
                        Stamp = DateTime.UtcNow;
                    }
                    else if (arg.Pipe == null || arg.IsWriteComplete)
                    {
                        break;
                    }
                    else
                    { }
                }
                //Latency hack
                Socket.NoDelay = true;
                Socket.Send(fin, SocketFlags.None);
                Socket.NoDelay = false;
                arg.Transfered += finLength;

                arg.CompleteRead();
                await arg.ReleasePipe();

                _ = Server.OnDataSend(arg);
            }
            catch (Exception ex)
            {
                arg.CompleteRead(ex);
                await arg.ReleasePipe(ex);
                Server.OnDataException(new TcpExceptionEventArgs(arg, ex));
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
                    Pipe = GetPipe()
                };

                await Load(arg);
            }
        }

        private bool CheckFinBuffer(ref Memory<byte> buffer, out bool setLoad)
        {
            setLoad = true;
            var index = buffer.Span.IndexOf(fin);
            if (index < 0)
            {
                setLoad = false;
                return false;
            }

            var endIndex = index + finLength;
            if (endIndex < buffer.Length)
            {
                setLoad = false;
                var slice = buffer.Slice(endIndex);
                _ = Task.Run(async () => await Load(slice));
            }
            buffer = index > 0 ? buffer.Slice(0, index) : Memory<byte>.Empty;

            return true;
        }

        private async ValueTask Load(Memory<byte> buffer)
        {
            var arg = new TcpStreamEventArgs(this, TcpStreamMode.Receive)
            {
                Pipe = GetPipe(),
            };
            _ = Task.Run(async () => await Server.OnDataLoadStart(arg));

            bool isBreak = CheckFinBuffer(ref buffer, out var setLoad);
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
                        var memory = new Memory<byte>(arg.Buffer.Array, 0, read);
                        var isBreak = CheckFinBuffer(ref memory, out setLoad);
                        if (!memory.IsEmpty)
                        {
                            if (arg.Transfered == 0)
                            {
                                _ = Task.Run(async () => await Server.OnDataLoadStart(arg));
                            }

                            arg.PartsCount++;
                            arg.Transfered += memory.Length;
                            await arg.WriterStream.WriteAsync(memory.ToArray(), 0, memory.Length);
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
                    Memory<byte> memory = arg.Pipe.Writer.GetMemory(arg.Buffer.Count);
                    var read = await Socket.ReceiveAsync(memory, SocketFlags.None);
                    if (read > 0)
                    {
                        var slice = memory.Length == read ? memory : memory.Slice(0, read);
                        var isBreak = CheckFinBuffer(ref slice, out setLoad);
                        if (!slice.IsEmpty)
                        {
                            if (arg.Transfered == 0)
                            {
                                _ = Task.Run(async () => await Server.OnDataLoadStart(arg));
                            }

                            arg.PartsCount++;
                            arg.Transfered += slice.Length;
                            arg.Pipe.Writer.Advance(slice.Length);
                            var result = await arg.Pipe.Writer.FlushAsync();
                            if (result.IsCompleted)
                            {
                                break;
                            }
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
                var arg = new TcpSocketEventArgs { Client = this };
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
                var arg = new TcpSocketEventArgs { Client = this };
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
                    _ = Server.OnClientDisconect(new TcpSocketEventArgs { Client = this });
                }

                loadEvent?.Dispose();
                sendEvent?.Dispose();
                disposed = true;
            }
        }
    }
}
