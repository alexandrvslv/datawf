using DataWF.Common;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DataWF.Test.Common
{
    [TestFixture]
    public class TestDelegateArray
    {
        private EventHandler defaultHandler;
        private ThreadSafeList<EventHandler> safeListHandler;
        private List<EventHandler> listHandler;

        public event EventHandler DefaultHandler
        {
            add => defaultHandler += value;
            remove => defaultHandler -= value;
        }

        public event EventHandler SafeListHandler
        {
            add
            {
                if (safeListHandler == null)
                    safeListHandler = new ThreadSafeList<EventHandler>();
                safeListHandler.Add(value);
            }
            remove => safeListHandler.Remove(value);
        }

        public event EventHandler ListHandler
        {
            add
            {
                if (listHandler == null)
                    listHandler = new List<EventHandler>();
                listHandler.Add(value);
            }
            remove => listHandler.Remove(value);
        }

        [SetUp]
        public void Setup()
        {            
        }

        [Test, Combinatorial]
        public void CallDefault(
            [Values(1, 300)] int callbackCount,
            [Values(1000, 100000)] int callCount)
        {
            defaultHandler = null;
            var call = 0;
            for (int i = 0; i < callbackCount; i++)
                DefaultHandler += Callback;
            for (int i = 0; i < callCount; i++)
            {
                defaultHandler(this, EventArgs.Empty);
            }
            Assert.AreEqual(callbackCount * callCount, call);
            void Callback(object sender, EventArgs args)
            {
                call++;
            }
        }

        [Test, Combinatorial]
        public void CallInvocationList(
            [Values(1, 300)] int callbackCount,
            [Values(1000, 100000)] int callCount)
        {
            defaultHandler = null;
            var call = 0;
            for (int i = 0; i < callbackCount; i++)
                DefaultHandler += Callback;
            for (int i = 0; i < callCount; i++)
            {
                foreach(EventHandler handler in defaultHandler.GetInvocationList())
                    handler(this, EventArgs.Empty);
            }
            Assert.AreEqual(callbackCount * callCount, call);
            void Callback(object sender, EventArgs args)
            {
                call++;
            }
        }

        [Test, Combinatorial]
        public void CallList(
            [Values(1, 300)] int callbackCount,
            [Values(1000, 100000)] int callCount)
        {
            listHandler = null;
            var call = 0;
            for (int i = 0; i < callbackCount; i++)
                ListHandler += Callback;
            for (int i = 0; i < callCount; i++)
            {
                foreach (var handler in listHandler)
                    handler(this, EventArgs.Empty);
            }
            Assert.AreEqual(callbackCount * callCount, call);
            void Callback(object sender, EventArgs args)
            {
                call++;
            }
        }

        [Test, Combinatorial]
        public void CallSafeList(
            [Values(1, 300)] int callbackCount,
            [Values(1000, 100000)] int callCount)
        {
            safeListHandler = null;
            var call = 0;
            for (int i = 0; i < callbackCount; i++)
                SafeListHandler += Callback;
            for (int i = 0; i < callCount; i++)
            {
                foreach (var handler in safeListHandler)
                    handler(this, EventArgs.Empty);
            }
            Assert.AreEqual(callbackCount * callCount, call);
            void Callback(object sender, EventArgs args)
            {
                call++;
            }
        }


    }
}
