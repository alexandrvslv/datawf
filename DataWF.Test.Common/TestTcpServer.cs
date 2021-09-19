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
            [Values(1, 100)] int packageCount,
            [Values(1, 100)] int itemsCount,
            [Values(SocketCompressionMode.None, SocketCompressionMode.Brotli, SocketCompressionMode.GZip)] SocketCompressionMode compressionMode)
        {
            var serializer = new BinarySerializer();
            var testList = TestSerialize.GenerateList(itemsCount);
            var newList = (List<TestSerializeClass>)null;
            var receiveCount = 0;
            var sendEvent = new ManualResetEventSlim(false);
            var packageSize = 0;
            var packagePartsCount = 0;
            
            var server1Url = new Uri($"tcp://127.0.0.1:{SocketHelper.GetTcpPort()}");
            var tcpServer1 = new TcpSocketService
            {
                Address = server1Url,
                Compression = compressionMode,
                TransferTimeout = default
            };
            tcpServer1.StartListener(50);
            
            var server2Url = new Uri($"tcp://127.0.0.1:{SocketHelper.GetTcpPort()}");
            var tcpServer2 = new TcpSocketService
            {
                Address = server2Url,
                Compression = compressionMode,
                TransferTimeout = default
            };
            tcpServer2.StartListener(50);

            var tcpClient1 = await tcpServer1.CreateConnection(server2Url);
            await Task.Delay(50);

            var tcpClient2 = await tcpServer2.CreateConnection(server1Url);
            tcpClient2.ReceiveStart = OnDataLoad;

            for (int i = 0; i < packageCount; i++)
            {
                await tcpClient1.SendT(testList);
            }
            sendEvent.Wait(10000);

            Console.WriteLine($"Packet Size: {packageSize} by {packagePartsCount} parts");
            tcpClient1.Dispose();
            tcpServer1.Dispose();
            tcpClient2.Dispose();
            tcpServer2.Dispose();

            Assert.IsNotNull(newList, "Deserialization Compleatly Fail");
            Assert.AreEqual(testList.Count, newList.Count, "Deserialization Count Fail");

            Assert.AreEqual(testList[0].IntValue, newList[0].IntValue, "Deserialization Fail");
            Assert.AreEqual(testList[0].DecimalValue, newList[0].DecimalValue, "Deserialization Fail");
            Assert.AreEqual(testList[0].StringValue, newList[0].StringValue, "Deserialization Fail");
            Assert.AreEqual(testList[0].ClassValue.StringValue, newList[0].ClassValue.StringValue, "Deserialization Fail");

            Assert.AreEqual(packageCount, receiveCount, "Deserialization Packets Fail");

            async ValueTask OnDataLoad(SocketStreamArgs e)
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
                    await e.CompleteRead(ex);
                }
                finally
                {
                    if (receiveCount == packageCount)
                    {
                        sendEvent.Set();
                    }
                }
            }
        }

    }
}
