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
            a1 = new byte[80000];
            a2 = new byte[80000];
            for (int i = 0; i < 20000; i++)
            {
                var bytes = BitConverter.GetBytes(i);
                for (int j = 0; j < 4; j++)
                {
                    var index = j * i + j;
                    a1[index] = a2[index] = bytes[j];
                }
            }
        }

        [Test]
        public void CompareBytes()
        {
            Assert.IsTrue(Helper.EqualsBytesUnsafe(a1, a2));
        }

        [Test]
        public void CompareBytesAsSpan()
        {
            Assert.IsTrue(Helper.EqualsBytes(a1, a2));

        }

    }
}
