using DataWF.Common;
using NUnit.Framework;
using System;
using System.Linq;

namespace DataWF.Test.Common
{
    [TestFixture]
    public class TestSpan
    {
        [Test]
        public void SpanSplit()
        {
            string someString = ", bla, blah, , , h, b, ";
            var spanResult = someString.AsSpan().Split(", ".AsSpan());
            Assert.AreEqual(4, spanResult.Count);
            Assert.AreEqual("bla", spanResult[0]);
            Assert.AreEqual("blah", spanResult[1]);
            Assert.AreEqual("h", spanResult[2]);
            Assert.AreEqual("b", spanResult[3]);
        }

        [Test]
        public void MemorySplit()
        {
            string someString = ", bla, blah, , , h, b, ";
            var spanResult = someString.AsMemory().Split(", ".AsMemory()).ToList();
            Assert.AreEqual(4, spanResult.Count);
            Assert.AreEqual("bla", spanResult[0].ToString());
            Assert.AreEqual("blah", spanResult[1].ToString());
            Assert.AreEqual("h", spanResult[2].ToString());
            Assert.AreEqual("b", spanResult[3].ToString());
        }

        [Test]
        public void SubPart()
        {
            string someString = "some text (bla (bl()ah) bla) some other text";
            var i = someString.IndexOf('(');
            var subResult = someString.AsSpan().GetSubPart(ref i, '(', ')');
            Assert.AreEqual("bla (bl()ah) bla", subResult.ToString());

            someString = "some text 'bla bla' some other text";
            i = someString.IndexOf('\'');
            subResult = someString.AsSpan().GetSubPart(ref i, '\'', '\'');

            Assert.AreEqual("bla bla", subResult.ToString());
        }
    }
}
