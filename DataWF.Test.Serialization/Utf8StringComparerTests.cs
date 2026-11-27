using System.Text;
using DataWF.Serialization;
using NUnit.Framework;

namespace DataWF.Test.Serialization;

[TestFixture]
public class Utf8StringComparerTests
{
    private static readonly Utf8StringOrdinalIgnoreCaseComparer Comparer =
        Utf8StringOrdinalIgnoreCaseComparer.Instance;

    [Test]
    public void Utf8EqualityAndHashCodesMatchStringValues()
    {
        (string Upper, string Lower)[] values =
        [
            ("", ""),
            ("A", "a"),
            ("AB", "ab"),
            ("ABC", "abc"),
            ("ABCD", "abcd"),
            ("ABCDE", "abcde"),
            ("ABCDEF", "abcdef"),
            ("ABCDEFG", "abcdefg"),
            ("ÄPFEL", "äpfel"),
            ("Καλημέρα", "καλημέρα"),
            ("ЛОПАТА ЛОПАТА ЛОПАТА ЛОПАТА ЛОПАТА ЛОПАТА ЛОПАТА ЛОПАТА ЛОПАТА ЛОПАТА ЛОПАТА ЛОПАТА ЛОПАТА ЛОПАТА ЛОПАТА", 
             "лопата лопата лопата лопата лопата лопата лопата лопата лопата лопата лопата лопата лопата лопата лопата"),
        ];

        foreach (var (upper, lower) in values)
        {
            var utf8Upper = Encoding.UTF8.GetBytes(upper);
            var utf8Lower = Encoding.UTF8.GetBytes(lower);

            Assert.That(Comparer.Equals(upper, lower), Is.True, upper);
            Assert.That(Comparer.Equals(utf8Upper, lower), Is.True, upper);
            Assert.That(Comparer.Equals(utf8Upper, utf8Lower), Is.True, upper);
            Assert.That(Comparer.GetHashCode(upper), Is.EqualTo(Comparer.GetHashCode(lower)), upper);
            Assert.That(Comparer.GetHashCode(utf8Upper), Is.EqualTo(Comparer.GetHashCode(lower)), upper);
            Assert.That(Comparer.Create(utf8Upper), Is.EqualTo(upper), upper);
        }
    }

    [Test]
    public void Utf8EqualityRejectsDifferentAndInvalidValues()
    {
        Assert.That(Comparer.Equals(Encoding.UTF8.GetBytes("DataWF"), "Data"), Is.False);
        Assert.That(Comparer.Equals(Encoding.UTF8.GetBytes("Data"), "DataWF"), Is.False);
        Assert.That(Comparer.Equals(Encoding.UTF8.GetBytes("Data"), Encoding.UTF8.GetBytes("DataWF")), Is.False);
        Assert.That(Comparer.Equals(new byte[] { 0xFF }, "�"), Is.False);
    }
}
