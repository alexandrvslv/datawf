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
                var index = Pull.GetHIndex(i, blockSize);
                char* left = (char*)&index;
                char* right = left + 1;
                Debug.WriteLine($"pull index {i} = {index} left {(int)*left} right {(int)*right}");
                pull.SetValue(index, i);
            }

            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(i, pull.GetValueInternal(Pull.GetHIndex(i, blockSize)));
            }
        }
    }
}
