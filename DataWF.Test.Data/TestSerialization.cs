using DataWF.Common;
using DataWF.Data;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text;

namespace DataWF.Test.Data
{
    public class TestSerialization
    {
        public const string SchemaName = "test";
        private TestProvider provider;
        private TestSchema schema;
        private PositionTable positions;
        private EmployerTable<Employer> employers;

        [SetUp]
        public async Task Setup()
        {
            provider = new TestProvider() { SchemaName = SchemaName };
            provider.CreateNew();
            schema = provider.Schema;
            positions = schema.Position;
            employers = schema.Employer;
        }

        [Test]
        public void BinarySerializePoistions()
        {
            var positions = ((IEnumerable<Position>)this.positions).ToList();
            var serializer = new DBBinarySerializer(schema);
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
            var employers = ((IEnumerable<Employer>)this.employers).ToList();
            var serializer = new DBBinarySerializer(schema);
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

        [Test]
        public void SchemaSerialization()
        {
            provider.Schems.Clear();
            provider.Schems.Add(schema);
            provider.Schems.Add((DBSchema)schema.LogSchema);

            var buffer = Serialization.Instance.Serialize(provider.Schems);
            PrintBuffer(buffer);

            provider.Schems.Clear();
            Serialization.Instance.Deserialize(buffer, provider.Schems);

            Assert.AreEqual(2, provider.Schems.Count);
            Assert.AreEqual(6, schema.Tables.Count);

            var table = schema.Tables[Employer.TableName];

            Assert.IsNotNull(table);
            Assert.IsInstanceOf<EmployerTable<Employer>>(table);

            var column = table.Columns["id"];

            Assert.IsNotNull(column);
            Assert.AreEqual(typeof(int?), column.DataType);

            void PrintBuffer(ArraySegment<byte> buffer)
            {
                var text = Encoding.UTF8.GetString(buffer);
                using (var reader = new StringReader(text))
                {
                    for (; ; )
                    {
                        var line = reader.ReadLine();
                        if (line != null)
                            System.Diagnostics.Debug.WriteLine(line);
                        else
                            break;
                    }
                }
            }
        }
    }
}
