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
        private DBTable<Position> positionTable;
        private DBTable<Employer> employerTable;

        [SetUp]
        public async Task Setup()
        {
            Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            DBService.Schems.Clear();
            TableGenerator.ClearGeneratorCache();

            if (DBService.Connections.Count == 0)
                Serialization.Deserialize("connections.xml", DBService.Connections);

            AccessValue.Provider = new AccessProviderStub
            {
                Groups = new IdCollection<IGroupIdentity>
                {
                    new AccessGroupBung() { Id = 1, Name = "Group1"},
                    new AccessGroupBung() { Id = 2, Name = "Group2"},
                    new AccessGroupBung() { Id = 3, Name = "Group3"}
                }
            };

            schema = new TestSchema();
            schema.Generate("");
            schema.Connection = DBService.Connections["TestSqlLite"];
            schema.DropDatabase();
            schema.CreateDatabase();

            positionTable = schema.Position;

            new Position(positionTable) { Id = 1, Code = "1", Name = "First Position" }.Attach();
            new Position(positionTable) { Id = 2, Code = "2", Name = "Second Position" }.Attach();
            new Position(positionTable) { Id = 3, Code = "3", Name = "Third Position" }.Attach();
            new Position(positionTable) { Id = 4, Code = "4", Name = "Sub Position", ParentId = 3 }.Attach();
            await positionTable.Save();

            employerTable = schema.Employer;
            var random = new Random();
            for (var i = 1; i < 100; i++)
            {
                new Employer(employerTable)
                {
                    Id = i,
                    Identifier = $"{i,8:0}",
                    PositionId = random.Next(1, 4),
                    IsActive = true,
                    Age = (byte)random.Next(18, 60),
                    LongId = 120321312321L,
                    Salary = 231323.32M,
                    Name = $"Ivan{i,3:0}",
                    Access = new AccessValue(new[]
                   {
                    new AccessItem(AccessValue.Provider.GetAccessIdentity(1, IdentityType.Group), AccessType.Read | AccessType.Download),
                    new AccessItem(AccessValue.Provider.GetAccessIdentity(2, IdentityType.Group), AccessType.Admin),
                    new AccessItem(AccessValue.Provider.GetAccessIdentity(3, IdentityType.Group), AccessType.Read | AccessType.Create | AccessType.Update)
                })
                }.Attach();
            }
            await employerTable.Save();
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
