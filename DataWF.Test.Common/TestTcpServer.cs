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
        [Test, Combinatorial]
        public async Task TestPipeStream([Values(1, 10, 100)] int packageCount, [Values(1, 10, 100)] int itemsCount = 20)
        {
            var testList = TestSerialize.GenerateList(itemsCount);
            var newList = (List<TestSerializeClass>)null;
            var newLists = new List<List<TestSerializeClass>>();
            var sendEvent = new ManualResetEventSlim(false);

            var tcpServer = new TcpServer { Point = SocketHelper.ParseEndPoint("localhost:51000") };
            tcpServer.StartListener(50);
            tcpServer.DataLoad += OnDataLoad;

            var tcpClient = new TcpSocket { Server = tcpServer, Point = SocketHelper.ParseEndPoint("localhost:51001") };
            await tcpClient.Connect(tcpServer.Point, false);
            while (tcpServer.Clients.Count == 0)
            {
                Debug.WriteLine("Wait connection...");
                await Task.Delay(10);
            }

            for (int i = 0; i < packageCount; i++)
            {
                await Task.Delay(5);
                await tcpClient.SendElement(testList);
            }
            sendEvent.Wait(10000);

            tcpClient.Dispose();
            tcpServer.Dispose();

            Assert.IsNotNull(newList, "Deserialization Compleatly Fail");
            Assert.AreEqual(testList.Count, newList.Count, "Deserialization Count Fail");

            Assert.AreEqual(testList[0].IntValue, newList[0].IntValue, "Deserialization Fail");
            Assert.AreEqual(testList[0].DecimalValue, newList[0].DecimalValue, "Deserialization Fail");
            Assert.AreEqual(testList[0].StringValue, newList[0].StringValue, "Deserialization Fail");
            Assert.AreEqual(testList[0].ClassValue.StringValue, newList[0].ClassValue.StringValue, "Deserialization Fail");

            Assert.AreEqual(packageCount, newLists.Count, "Deserialization Packets Fail");

            void OnDataLoad(object sender, TcpStreamEventArgs e)
            {
                try
                {
                    using (var stream = e.ReaderStream)
                    {
                        var serializer = new BinarySerializer();
                        newList = serializer.Deserialize<List<TestSerializeClass>>(stream, null);
                        newLists.Add(newList);
                    }
                }
                catch (Exception ex)
                {

                }
                finally
                {
                    if (newLists.Count == packageCount)
                    {
                        sendEvent.Set();
                    }
                }
            }
        }

    }
}
