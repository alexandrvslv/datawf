using DataWF.Common;
using NUnit.Framework;
using System.Diagnostics;

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
            for (int i = 0; i < 1000; i++)
            {
                var index = PullHandler.FromSeqence(i, blockSize);
                Debug.WriteLine($"pull index {i} = {(int)index} left {index.Block} right {index.BlockIndex}");
                pull.SetValue(index, i);
            }

            for (int i = 0; i < 1000; i++)
            {
                var index = PullHandler.FromSeqence(i, blockSize);
                Assert.AreEqual(i, pull.GetValue(index));
            }
        }

        [Test]
        public unsafe void NullablePullIndex()
        {
            var blockSize = 50;
            var pull = new NullablePull<int>(blockSize);
            for (int i = 0; i < 1000; i++)
            {
                var index = PullHandler.FromSeqence(i, blockSize);
                Debug.WriteLine($"pull index {i} = {(int)index} left {index.Block} right {index.BlockIndex}");
                pull.SetValue<int?>(index, i);
            }

            for (int i = 0; i < 1000; i++)
            {
                var index = PullHandler.FromSeqence(i, blockSize);
                Assert.AreEqual(i, pull.GetValue(index));
            }
        }

        [Test]
        public void ShortToIntShift()
        {
            Stopwatch watch = new Stopwatch();
            watch.Reset();
            watch.Start();
            for (short i = 0; i < 1000; i++)
            {
                for (short j = 0; j < 1000; j++)
                {
                    var value = Helper.TwoToOneShift(i, j);
                    (short a, short b) = Helper.OneToTwoShift(value);
                    Assert.AreEqual(i, a);
                    Assert.AreEqual(j, b);
                }
            }
            watch.Stop();
            BenchmarkResult.Add($"ShortToInt", nameof(ShortToIntShift), watch.Elapsed);
        }

        [Test]
        public void ShortToIntPointer()
        {
            Stopwatch watch = new Stopwatch();
            watch.Reset();
            watch.Start();
            for (short i = 0; i < 1000; i++)
            {
                for (short j = 0; j < 1000; j++)
                {
                    var value = Helper.TwoToOnePointer(i, j);
                    (short a, short b) = Helper.OneToTwoPointer(value);
                    Assert.AreEqual(i, a);
                    Assert.AreEqual(j, b);
                }
            }
            watch.Stop();
            BenchmarkResult.Add($"ShortToInt", nameof(ShortToIntPointer), watch.Elapsed);
        }

        [Test]
        public void ShortToIntStruct()
        {
            Stopwatch watch = new Stopwatch();
            watch.Reset();
            watch.Start();
            for (short i = 0; i < 1000; i++)
            {
                for (short j = 0; j < 1000; j++)
                {
                    var value = Helper.TwoToOneStruct(i, j);
                    (short a, short b) = Helper.OneToTwoStruct(value);
                    Assert.AreEqual(i, a);
                    Assert.AreEqual(j, b);
                }
            }
            watch.Stop();
            BenchmarkResult.Add($"ShortToInt", nameof(ShortToIntStruct), watch.Elapsed);
        }

        [Test]
        public void IntToLongShift()
        {
            Stopwatch watch = new Stopwatch();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < 1000; i++)
            {
                for (int j = 0; j < 1000; j++)
                {
                    var value = Helper.TwoToOneShiftLong(i, j);
                    (int a, int b) = Helper.OneToTwoShiftLong(value);
                    Assert.AreEqual(i, a);
                    Assert.AreEqual(j, b);
                }
            }
            watch.Stop();
            BenchmarkResult.Add($"IntToLong", nameof(IntToLongShift), watch.Elapsed);
        }

        [Test]
        public void IntToLongPointer()
        {
            Stopwatch watch = new Stopwatch();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < 1000; i++)
            {
                for (int j = 0; j < 1000; j++)
                {
                    var value = Helper.TwoToOnePointer(i, j);
                    (int a, int b) = Helper.OneToTwoPointer(value);
                    Assert.AreEqual(i, a);
                    Assert.AreEqual(j, b);
                }
            }
            watch.Stop();
            BenchmarkResult.Add($"IntToLong", nameof(IntToLongPointer), watch.Elapsed);
        }

        [Test]
        public void IntToLongStruct()
        {
            Stopwatch watch = new Stopwatch();
            watch.Reset();
            watch.Start();
            for (int i = 0; i < 1000; i++)
            {
                for (int j = 0; j < 1000; j++)
                {
                    var value = Helper.TwoToOneStruct(i, j);
                    (int a, int b) = Helper.OneToTwoStruct(value);
                    Assert.AreEqual(i, a);
                    Assert.AreEqual(j, b);
                }
            }
            watch.Stop();
            BenchmarkResult.Add($"IntToLong", nameof(IntToLongStruct), watch.Elapsed);
        }
    }
}
