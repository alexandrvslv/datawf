using DataWF.Common;
using NUnit.Framework;
using System;
using System.Diagnostics;

namespace DataWF.Test.Common
{
    [TestFixture]
    public class TestByteComparer
    {
        private byte[] a1;
        private byte[] a2;

        [SetUp]
        public void Setup()
        {
            a1 = new byte[83000];
            a2 = new byte[83000];
        }

        [Test]
        public void CompareBytes()
        {
            for (int i = 0; i < 10000; i++)
            {
                var bytes = BitConverter.GetBytes(i);
                for (int j = 0; j < 4; j++)
                {
                    a1[j] = a2[j] = bytes[j];
                }

                Assert.IsTrue(Helper.CompareByte(a1, a2));
            }
        }

        [Test]
        public void CompareBytesAsSpan()
        {
            for (int i = 0; i < 10000; i++)
            {
                var bytes = BitConverter.GetBytes(i);
                for (int j = 0; j < 4; j++)
                {
                    a1[j] = a2[j] = bytes[j];
                }

                Assert.IsTrue(Helper.CompareByteAsSpan(a1, a2));
            }
        }

    }
}
