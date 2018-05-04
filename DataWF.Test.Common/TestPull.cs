using DataWF.Common;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataWF.Test.Common
{
    [TestFixture]
    public class TestPull
    {
        [Test]
        public unsafe void PullIndex()
        {
            var blockSize = 50;
            var pull = new Pull<int>(blockSize);
            for (int i = 0; i < 100; i++)
            {
                var index = Pull.GetHIndexUnsafe(i, blockSize);
                short* left = (short*)&index;
                short* right = left + 1;
                Debug.WriteLine($"pull index {i} = {index} left {*left} right {*right}");
                pull.SetValue(index, i);
            }

            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(i, pull.GetValueInternal(Pull.GetHIndexUnsafe(i, blockSize)));
            }
        }

        [Test]
        public unsafe void NullablePullIndex()
        {
            var blockSize = 50;
            var pull = new NullablePull<int>(blockSize);
            for (int i = 0; i < 100; i++)
            {
                var index = Pull.GetHIndexUnsafe(i, blockSize);
                short* left = (short*)&index;
                short* right = left + 1;
                Debug.WriteLine($"pull index {i} = {index} left {*left} right {*right}");
                pull.SetValue<int?>(index, i);
            }

            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(i, pull.GetValueInternal(Pull.GetHIndexUnsafe(i, blockSize)));
            }
        }
    }
}
