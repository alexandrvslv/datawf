using DataWF.Common;
using DataWF.Data;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataWF.Test.Data
{
    public class TestSerialization
    {
        public const string SchemaName = "test";
        private TestSchema schema;
        private PositionTable positionTable;
        private EmployerTable<Employer> employerTable;

        [SetUp]
        public async Task Setup()
        {
           
        }

        [Test]
        public void BinarySerializePoistions()
        {
            var positions = ((IEnumerable<Position>)positionTable).ToList();
            var serializer = DBBinarySerializer.Instance;
            var buffer = serializer.Serialize(positions);

            var newList = serializer.Deserialize<List<Position>>(buffer, null);

            Assert.AreEqual(positions.Count, newList.Count, "Deserialize Fail");

            foreach (var newItem in newList)
            {
                var oldItem = newItem.AttachOrUpdate(DBLoadParam.None);
                Assert.AreNotEqual(newItem, oldItem);
                Assert.IsFalse(oldItem.IsChanged);
            }
        }

        [Test]
        public void BinarySerializeEmployers()
        {
            var employers = ((IEnumerable<Employer>)employerTable).ToList();
            var serializer = DBBinarySerializer.Instance;
            var buffer = serializer.Serialize(employers);

            var newList = serializer.Deserialize<List<Employer>>(buffer, null);

            Assert.AreEqual(employers.Count, newList.Count, "Deserialize Fail");

            foreach (var newItem in newList)
            {
                var oldItem = newItem.AttachOrUpdate(DBLoadParam.None);
                Assert.AreNotEqual(newItem, oldItem);
                Assert.IsFalse(oldItem.IsChanged);
            }
        }
    }
}
