using System;
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
        internal static readonly Queue<Pipe> Pipes = new Queue<Pipe>();
        private static readonly byte[] fin = Encoding.ASCII.GetBytes("<finito>");
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
            if (Pipes.Count > 0)
            {
                var pipe = Pipes.Dequeue();
                return pipe;
            }
            var options = new PipeOptions(
                minimumSegmentSize: 1024,
                pauseWriterThreshold: 32 * 1024,
                resumeWriterThreshold: 16 * 1024,
                useSynchronizationContext: false);
            return new Pipe(options);
        }

        public async ValueTask SendElement<T>(T element)
        {
            var args = new TcpStreamEventArgs(this, TcpStreamMode.Send)
            {
                Pipe = GetPipe(),
                Tag = element
            };

            _ = Task.Run(Serialize).ConfigureAwait(false);

            await Send(args);

            void Serialize()
            {
                try
                {
                    serializer.Serialize(args.WriterStream, element);
                    args.CompleteWrite();
                }
                catch (Exception ex)
                {
                    args.CompleteWrite(ex);
                }
            }
        }

        public ValueTask Send(byte[] data)
        {
            using (var stream = new MemoryStream(data))
            {
                return Send(stream);
            }
        }

        public ValueTask Send(Stream stream)
        {
            return Send(new TcpStreamEventArgs(this, TcpStreamMode.Send)
            {
                SourceStream = stream
            });
        }

        public async ValueTask Send(TcpStreamEventArgs arg)
        {
            if (!(Socket?.Connected ?? false))
            {
                throw new Exception("Socket is Discconected!");
            }
            try
            {
                sendEvent.Wait();
                sendEvent.Reset();

                Debug.WriteLine($"TcpClient {Point} Start Send");
                while (true)
                {
                    var read = await arg.ReaderStream.ReadAsync(arg.Buffer, 0, TcpStreamEventArgs.BufferSize);
                    if (read > 0)
                    {
                        var sended = await Task.Factory.FromAsync<int>(Socket.BeginSend(arg.Buffer, 0, read, SocketFlags.None, null, arg), Socket.EndSend);
                        Debug.WriteLine($"TcpClient {Point} Send Packet: {sended}");
                        arg.Transfered += sended;
                        arg.PackageCount++;
                        Stamp = DateTime.UtcNow;
                    }
                    else if (arg.Pipe == null || arg.IsWriteComplete)
                    {
                        break;
                    }
                    else
                    { }
                }
                Socket.Send(fin, SocketFlags.None);
                Debug.WriteLine($"TcpClient {Point} End Send");

                //Socket.NoDelay = true;
                //Socket.NoDelay = false;
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

        public Task SartListen()
        {
            return ListenerLoop();
        }

        private async Task ListenerLoop()
        {
            while (Socket?.Connected ?? false)
            {
                var arg = new TcpStreamEventArgs(this, TcpStreamMode.Receive)
                {
                    Pipe = GetPipe()
                };
                await Load(arg);
            }
        }

        private async ValueTask Load(ArraySegment<byte> startBlock)
        {
            var arg = new TcpStreamEventArgs(this, TcpStreamMode.Receive)
            {
                Pipe = GetPipe()
            };
            _ = Server.OnDataLoadStart(arg);
            arg.Transfered = startBlock.Count;
            await arg.WriterStream.WriteAsync(startBlock.Array, startBlock.Offset, startBlock.Count);
            await Load(arg);
        }

        private async ValueTask Load(TcpStreamEventArgs arg)
        {
            try
            {
                loadEvent.Wait();
                loadEvent.Reset();
                Debug.WriteLine($"TcpClient {Point} Start Receive");
                while (true)
                {
                    int read = await Task.Factory.FromAsync(Socket.BeginReceive(arg.Buffer, 0, TcpStreamEventArgs.BufferSize, SocketFlags.None, null, arg), Socket.EndReceive);
                    Debug.WriteLine($"TcpClient {Point} Receive: {read}");
                    if (read > 0)
                    {
                        if (arg.Transfered == 0)
                        {
                            _ = Server.OnDataLoadStart(arg);
                        }
                        arg.Transfered += read;
                        arg.PackageCount++;
                        var index = ByteArrayComparer.Default.IndexOf(new ReadOnlySpan<byte>(arg.Buffer, 0, read), fin);
                        if (index > -1)
                        {
                            if (index > 0)
                            {
                                await arg.WriterStream.WriteAsync(arg.Buffer, 0, index);
                            }
                            var endIndex = index + fin.Length;
                            if (endIndex < read)
                            {
                                _ = Load(new ArraySegment<byte>(arg.Buffer, endIndex, read - endIndex));
                            }
                            break;
                        }
                        else
                        {
                            await arg.WriterStream.WriteAsync(arg.Buffer, 0, read);
                        }
                    }
                    else
                    {
                        await Disconnect();
                        return;
                    }
                }
                Debug.WriteLine($"TcpClient {Point} End Receive");
                arg.CompleteWrite();
                Stamp = DateTime.UtcNow;
                _ = Server.OnDataLoadEnd(arg);
            }
            catch (Exception ex)
            {
                arg.CompleteWrite(ex);
                await arg.ReleasePipe(ex);
                Server.OnDataException(new TcpExceptionEventArgs(arg, ex));
            }
            finally
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
