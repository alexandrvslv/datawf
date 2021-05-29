using NUnit.Framework;
using DataWF.Common;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;

namespace DataWF.Test.Common
{
    [TestFixture()]
    public class TestTcpServer
    {
        [SetUp]
        public void Setup()
        {
            //Helper.MainThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        [Test, Combinatorial]
        public async Task TestPipeStream(
            [Values(1, 100, 1000)] int packageCount,
            [Values(1, 100, 10000)] int itemsCount,
            [Values(SocketCompressionMode.None, SocketCompressionMode.Brotli, SocketCompressionMode.GZip)] SocketCompressionMode compressionMode)
        {
            var serializer = new BinarySerializer();
            var testList = TestSerialize.GenerateList(itemsCount);
            var newList = (List<TestSerializeClass>)null;
            var receiveCount = 0;
            var sendEvent = new ManualResetEventSlim(false);
            var packageSize = 0;
            var packagePartsCount = 0;
            var tcpServer = new TcpServer
            {
                Point = SocketHelper.ParseEndPoint($"localhost:{SocketHelper.GetTcpPort()}"),
                Compression = compressionMode,
                ParseDataLoad = OnDataLoad,
            };
            tcpServer.StartListener(50);

            var tcpClient = new TcpSocket { Server = tcpServer, Point = SocketHelper.ParseEndPoint($"localhost:{SocketHelper.GetTcpPort()}") };
            await tcpClient.Connect(tcpServer.Point, false);
            while (tcpServer.Clients.Count == 0)
            {
                Debug.WriteLine("Wait connection...");
                await Task.Delay(20);
            }

            for (int i = 0; i < packageCount; i++)
            {
                await tcpClient.SendElement(testList);
            }
            sendEvent.Wait(10000);

            Console.WriteLine($"Packet Size: {packageSize} by {packagePartsCount} parts");
            tcpClient.Dispose();
            tcpServer.Dispose();

            Assert.IsNotNull(newList, "Deserialization Compleatly Fail");
            Assert.AreEqual(testList.Count, newList.Count, "Deserialization Count Fail");

            Assert.AreEqual(testList[0].IntValue, newList[0].IntValue, "Deserialization Fail");
            Assert.AreEqual(testList[0].DecimalValue, newList[0].DecimalValue, "Deserialization Fail");
            Assert.AreEqual(testList[0].StringValue, newList[0].StringValue, "Deserialization Fail");
            Assert.AreEqual(testList[0].ClassValue.StringValue, newList[0].ClassValue.StringValue, "Deserialization Fail");

            Assert.AreEqual(packageCount, receiveCount, "Deserialization Packets Fail");

            Task OnDataLoad(TcpStreamEventArgs e)
            {
                try
                {
                    using (var stream = e.ReaderStream)
                    {
                        newList = serializer.Deserialize<List<TestSerializeClass>>(stream, e.Buffer.Count, null);
                        receiveCount++;
                    }
                    packagePartsCount = e.PartsCount;
                    packageSize = e.Transfered;
                }
                catch (Exception ex)
                {
                    e.CompleteRead(ex);
                }
                finally
                {
                    if (receiveCount == packageCount)
                    {
                        sendEvent.Set();
                    }
                }
                return Task.CompletedTask;
            }
        }

    }
}
