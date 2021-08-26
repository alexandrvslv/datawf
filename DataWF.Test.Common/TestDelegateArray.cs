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
        private HashSet<EventHandler> hashSetHandler;

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

        public event EventHandler HashSetHandler
        {
            add
            {
                if (hashSetHandler == null)
                    hashSetHandler = new HashSet<EventHandler>();
                hashSetHandler.Add(value);
            }
            remove => hashSetHandler.Remove(value);
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
            DefaultHandler += Callback1;
            DefaultHandler += Callback2;
            DefaultHandler -= Callback1;
            Assert.AreEqual(defaultHandler.GetInvocationList().Length, 1);

            defaultHandler = null;
            var call = 0;
            for (int i = 0; i < callbackCount; i++)
                DefaultHandler += new EventHandler(Callback);
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
            DefaultHandler += Callback1;
            DefaultHandler += Callback2;
            DefaultHandler -= Callback1;
            Assert.AreEqual(defaultHandler.GetInvocationList().Length, 1);

            defaultHandler = null;
            var call = 0;
            for (int i = 0; i < callbackCount; i++)
                DefaultHandler += new EventHandler(Callback);
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
            ListHandler += Callback1;
            ListHandler += Callback2;
            ListHandler -= Callback1;
            Assert.AreEqual(listHandler.Count, 1);

            listHandler = null;
            var call = 0;
            for (int i = 0; i < callbackCount; i++)
                ListHandler += new EventHandler(Callback);
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
            SafeListHandler += Callback1;
            SafeListHandler += Callback2;
            SafeListHandler -= Callback1;
            Assert.AreEqual(safeListHandler.Count, 1);

            safeListHandler = null;
            var call = 0;
            for (int i = 0; i < callbackCount; i++)
                SafeListHandler += new EventHandler(Callback);
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

        [Test, Combinatorial]
        public void CallHashSet(
            [Values(1, 300)] int callbackCount,
            [Values(1000, 100000)] int callCount)
        {
            hashSetHandler = null;
            HashSetHandler += Callback1;
            HashSetHandler += Callback2;
            HashSetHandler -= Callback1;
            Assert.AreEqual(hashSetHandler.Count, 1);

            hashSetHandler = null;
            var call = 0;
            for (int i = 0; i < callbackCount; i++)
                HashSetHandler += new EventHandler(Callback);
            for (int i = 0; i < callCount; i++)
            {
                foreach (var handler in hashSetHandler)
                    handler(this, EventArgs.Empty);
            }
            Assert.AreEqual(callCount, call);
            void Callback(object sender, EventArgs args)
            {
                call++;
            }
        }


        void Callback1(object sender, EventArgs args) { }
        void Callback2(object sender, EventArgs args) { }
    }
}
