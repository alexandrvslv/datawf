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
        internal static readonly Stack<Pipe> Pipes = new Stack<Pipe>();
        private static readonly byte[] fin = Encoding.ASCII.GetBytes("<finito>");
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
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                if (socket.RemoteEndPoint is IPEndPoint point)
                {
                    Point = point;
                }
            }
        }

        public IPEndPoint Point { get; set; }

        public IPEndPoint LocalPoint { get { return socket == null ? null : (IPEndPoint)socket.LocalEndPoint; } }

        private Pipe GetPipe()
        {
            if (Pipes.Count > 0)
            {
                var pipe = Pipes.Pop();
                return pipe;
            }
            var options = new PipeOptions(
                minimumSegmentSize: 1024,
                pauseWriterThreshold: 16 * 1024,
                resumeWriterThreshold: 8 * 1024,
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
                var serializer = new BinarySerializer(typeof(T));
                serializer.Serialize(args.WriterStream, element);
                args.WriterStream.Dispose();
                args.Pipe.Writer.Complete();
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
            try
            {
                sendEvent.Wait();
                sendEvent.Reset();

                while (true)
                {
                    Debug.WriteLine($"TcpClient {Point} Start Send");
                    var read = await arg.ReaderStream.ReadAsync(arg.Buffer, 0, TcpStreamEventArgs.BufferSize);
                    if (read > 0)
                    {
                        Debug.WriteLine($"TcpClient {Point} Start Send Packet: {read}");
                        var sended = await Task.Factory.FromAsync<int>(Socket.BeginSend(arg.Buffer, 0, read, SocketFlags.None, null, arg), Socket.EndSend);
                        Debug.WriteLine($"TcpClient {Point} End Send Packet: {sended}");
                        arg.Transfered += sended;
                        arg.PackageCount++;
                        Stamp = DateTime.UtcNow;
                    }
                    else
                    {
                        break;
                    }
                }
                arg.ReleasePipe();

                Socket.NoDelay = true;
                Socket.Send(fin, SocketFlags.None);
                Socket.NoDelay = false;

                _ = Server.OnDataSend(arg);
            }
            catch (Exception ex)
            {
                arg.ReleasePipe();
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
            while (Socket.Connected)
            {
                var arg = new TcpStreamEventArgs(this, TcpStreamMode.Receive)
                {
                    Pipe = GetPipe()
                };
                try
                {
                    loadEvent.Wait();
                    loadEvent.Reset();
                    while (true)
                    {
                        Debug.WriteLine($"TcpClient {Point} Start Receive");
                        int read = await Task.Factory.FromAsync(Socket.BeginReceive(arg.Buffer, 0, TcpStreamEventArgs.BufferSize, SocketFlags.None, null, arg), Socket.EndReceive);
                        if (read > 0)
                        {
                            Debug.WriteLine($"TcpClient {Point} Receive: {read}");
                            if (arg.Transfered == 0)
                            {
                                _ = Server.OnDataLoadStart(arg);
                            }
                            arg.Transfered += read;
                            arg.PackageCount++;

                            if (ByteArrayComparer.Default.EndWith(new ReadOnlySpan<byte>(arg.Buffer, 0, read), fin))
                            {
                                if (read > fin.Length)
                                {
                                    await arg.WriterStream.WriteAsync(arg.Buffer, 0, read - fin.Length);
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
                            await Disconnect(true);
                            return;
                        }
                    }
                    arg.WriterStream.Dispose();
                    arg.Pipe.Writer.Complete();
                    loadEvent.Set();
                    Stamp = DateTime.UtcNow;
                    _ = Server.OnDataLoadEnd(arg);
                }
                catch (Exception ex)
                {
                    arg.ReleasePipe();
                    Server.OnDataException(new TcpExceptionEventArgs(arg, ex));
                }
                finally
                {
                    loadEvent.Set();

                }
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
                }
            }
        }

        public async ValueTask Disconnect(bool reuse)
        {
            if (Socket.Connected)
            {
                var arg = new TcpSocketEventArgs { Client = this };
                try
                {
                    Debug.WriteLine($"TcpClient {Point} Disconnect Reuse:{reuse}");
                    Socket.Shutdown(SocketShutdown.Both);
                    await Task.Factory.FromAsync(Socket.BeginDisconnect(reuse, null, arg), Socket.EndDisconnect);
                    loadEvent.Set();
                    sendEvent.Set();
                    socket.Close();

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
                _ = Disconnect(false);
                loadEvent?.Dispose();
                sendEvent?.Dispose();
                disposed = true;
            }
        }
    }
}
