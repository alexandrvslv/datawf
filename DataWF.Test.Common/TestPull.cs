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
            for (int i = 0; i < 100; i++)
            {
                var index = Pull.GetHIndex(i, blockSize, out var block, out var blockIndex);
                Helper.OneToTwoShift(index, out var left, out var right);
                Debug.WriteLine($"pull index {i} = {index} left {left} right {right}");
                pull.SetValue(index, i);
            }

            for (int i = 0; i < 100; i++)
            {
                Pull.GetHIndex(i, blockSize, out var block, out var blockIndex);
                Assert.AreEqual(i, pull.GetValue(block, blockIndex));
            }
        }

        [Test]
        public unsafe void NullablePullIndex()
        {
            var blockSize = 50;
            var pull = new NullablePull<int>(blockSize);
            for (int i = 0; i < 100; i++)
            {
                var index = Pull.GetHIndex(i, blockSize, out var block, out var blockIndex);
                Helper.OneToTwoShift(index, out var left, out var right);
                Debug.WriteLine($"pull index {i} = {index} left {left} right {right}");
                pull.SetValue<int?>(index, i);
            }

            for (int i = 0; i < 100; i++)
            {
                Pull.GetHIndex(i, blockSize, out var block, out var blockIndex);
                Assert.AreEqual(i, pull.GetValue(block, blockIndex));
            }
        }

        [Test]
        public void ShortToIntShift()
        {
            Stopwatch watch = new Stopwatch();
            watch.Reset();
            watch.Start();
            for (short i = 0; i < 100; i++)
            {
                for (short j = 0; j < 100; j++)
                {
                    var value = Helper.TwoToOneShift(i, j);
                    Helper.OneToTwoShift(value, out var a, out var b);
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
            for (short i = 0; i < 100; i++)
            {
                for (short j = 0; j < 100; j++)
                {
                    var value = Helper.TwoToOnePointer(i, j);
                    Helper.OneToTwoPointer(value, out var a, out var b);
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
            for (short i = 0; i < 100; i++)
            {
                for (short j = 0; j < 100; j++)
                {
                    var value = Helper.TwoToOneStruct(i, j);
                    Helper.OneToTwoStruct(value, out var a, out var b);
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
            for (int i = 0; i < 100; i++)
            {
                for (int j = 0; j < 100; j++)
                {
                    var value = Helper.TwoToOneShift(i, j);
                    Helper.OneToTwoShift(value, out var a, out var b);
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
            for (int i = 0; i < 100; i++)
            {
                for (int j = 0; j < 100; j++)
                {
                    var value = Helper.TwoToOnePointer(i, j);
                    Helper.OneToTwoPointer(value, out var a, out var b);
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
            for (int i = 0; i < 100; i++)
            {
                for (int j = 0; j < 100; j++)
                {
                    var value = Helper.TwoToOneStruct(i, j);
                    Helper.OneToTwoStruct(value, out var a, out var b);
                    Assert.AreEqual(i, a);
                    Assert.AreEqual(j, b);
                }
            }
            watch.Stop();
            BenchmarkResult.Add($"IntToLong", nameof(IntToLongStruct), watch.Elapsed);
        }
    }
}
