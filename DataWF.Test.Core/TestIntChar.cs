using DataWF;
using NUnit.Framework;
using System.Diagnostics;

namespace DataWF.Test.Core
{
    [TestFixture]
    public class TestIntChar
    {
        [Test]
        public void Convertor()
        {
            for (int i = 0; i < 1000; i++)
            {
                var str = CellReference.IntToAbc(i);
                Debug.WriteLine($"{i} : {str}");
                var c = CellReference.AbcToInt(str);
                Assert.AreEqual(i, c);
            }
            //Assert.AreEqual(0, 1, $"For fun {CellReference.IntToAbc(10000)}");
        }


    }
}
