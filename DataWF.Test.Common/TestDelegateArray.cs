using DataWF.Common;
using NUnit.Framework;
using System;
using System.Diagnostics;

namespace DataWF.Test.Common
{
    [TestFixture]
    public class TestDelegateArray
    {
        private EventHandler defaultHandler;
        private ThreadSafeList<EventHandler> arrayHandler;
        private int call;

        public event EventHandler DefaultHandler
        {
            add => defaultHandler += value;
            remove => defaultHandler -= value;
        }

        public event EventHandler ArrayHandler
        {
            add
            {
                if (arrayHandler == null)
                    arrayHandler = new ThreadSafeList<EventHandler>();
                arrayHandler.Add(value);
            }
            remove => arrayHandler.Remove(value);
        }

        [Test, Combinatorial]
        public void CallDefault(
            [Values(1, 2, 3)] int callbackCount,
            [Values(100, 10000)] int callCount)
        {
            defaultHandler = null;
            call = 0;
            for (int i = 0; i < callbackCount; i++)
                DefaultHandler += Callback;
            for (int i = 0; i < callCount; i++)
            {
                defaultHandler(this, EventArgs.Empty);
            }
            Assert.AreEqual(call, callbackCount * callCount);
        }

        [Test, Combinatorial]
        public void CallArray(
            [Values(1, 2, 3)] int callbackCount,
            [Values(100, 10000)] int callCount)
        {
            arrayHandler = null;
            call = 0;
            for (int i = 0; i < callbackCount; i++)
                ArrayHandler += Callback;
            for (int i = 0; i < callCount; i++)
            {
                defaultHandler(this, EventArgs.Empty);
            }
            Assert.AreEqual(call, callbackCount * callCount);
        }

        private void Callback(object sender, EventArgs args)
        {
            call++;
        }

       
    }
}
