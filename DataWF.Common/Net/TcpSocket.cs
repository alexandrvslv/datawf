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
        protected readonly BinarySerializer serializer = new BinarySerializer();
        protected ManualResetEventSlim sendEvent = new ManualResetEventSlim(true);
        protected ManualResetEventSlim loadEvent = new ManualResetEventSlim(true);
        private Socket socket;
        private bool disposed;
        private bool disconnected;

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

        public Pipe GetPipe()
        {
            if (Pipes.Count > 0)
            {
                var pipe = Pipes.Pop();
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
                serializer.Serialize(args.WriterStream, element);
                args.CompleteWrite();
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

                Debug.WriteLine($"TcpClient {Point} Start Send");
                while (true)
                {
                    var read = await arg.ReaderStream.ReadAsync(arg.Buffer, 0, TcpStreamEventArgs.BufferSize);
                    Debug.WriteLine($"TcpClient {Point} Send Reader: {read}");
                    if (read > 0)
                    {
                        Debug.WriteLine($"TcpClient {Point} Start Send Packet: {read}");
                        var sended = await Task.Factory.FromAsync<int>(Socket.BeginSend(arg.Buffer, 0, read, SocketFlags.None, null, arg), Socket.EndSend);
                        Debug.WriteLine($"TcpClient {Point} End Send Packet: {sended}");
                        arg.Transfered += sended;
                        arg.PackageCount++;
                        Stamp = DateTime.UtcNow;
                    }
                    else if (arg.Pipe == null || arg.IsPipeComplete)
                    {
                        break;
                    }
                    else
                    { }
                }
                Socket.Send(fin, SocketFlags.None);

                Socket.NoDelay = true;
                Socket.NoDelay = false;

                arg.ReleasePipe();
                Debug.WriteLine($"TcpClient {Point} End Send");

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
                                if ((index + fin.Length) < read)
                                {
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
                    disconnected = false;
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
            if (Socket.Connected)
            {
                var arg = new TcpSocketEventArgs { Client = this };
                try
                {
                    Debug.WriteLine($"TcpClient {Point} Disconnect");
                    Socket.Shutdown(SocketShutdown.Both);
                    await Task.Factory.FromAsync(Socket.BeginDisconnect(true, null, arg), Socket.EndDisconnect);
                    loadEvent.Set();
                    sendEvent.Set();
                    socket.Close();
                    disconnected = true;
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
                if (!disconnected)
                {
                    Debug.WriteLine($"TcpClient {Point} Dispose Disconnect");
                    Socket.Shutdown(SocketShutdown.Both);
                    Socket.Disconnect(true);
                    loadEvent.Set();
                    sendEvent.Set();
                    socket.Close();
                    disconnected = true;
                    _ = Server.OnClientDisconect(new TcpSocketEventArgs { Client = this });
                }

                loadEvent?.Dispose();
                sendEvent?.Dispose();
                disposed = true;
            }
        }
    }
}
