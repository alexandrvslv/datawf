using DataWF.Common;
using NUnit.Framework;
using System.Diagnostics;

namespace DataWF.Test.Common
{
    [TestFixture]
    public class TestIntChar
    {
        [Test]
        public void Convertor()
        {
            for (int i = 0; i < 1000; i++)
            {
                var str = Helper.IntToChar(i);
                Debug.WriteLine($"{i} : {str}");
                var c = Helper.CharToInt(str);
                Assert.AreEqual(i, c);
            }
            //Assert.AreEqual(0, 1, $"For fun {Helper.IntToChar(10000)}");
        }


    }
}
