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
        private DBSchema schema;

        [SetUp]
        public async Task Setup()
        {
            Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            DBService.Schems.Clear();
            DBTable.ClearGeneratorCache();

            if (DBService.Connections.Count == 0)
                Serialization.Deserialize("connections.xml", DBService.Connections);

            AccessValue.Groups = new IdCollection<IGroupIdentity> {
                new AccessGroupBung() { Id = 1, Name = "Group1"},
                new AccessGroupBung() { Id = 2, Name = "Group2"},
                new AccessGroupBung() { Id = 3, Name = "Group3"}
            };

            schema = DBSchema.Generate(SchemaName, typeof(FileData), typeof(FileStore), typeof(Employer), typeof(Position), typeof(Figure));
            schema.Connection = DBService.Connections["TestSqlLite"];
            schema.DropDatabase();
            schema.CreateDatabase();

            new Position() { Id = 1, Code = "1", Name = "First Position" }.Attach();
            new Position() { Id = 2, Code = "2", Name = "Second Position" }.Attach();
            new Position() { Id = 3, Code = "3", Name = "Third Position" }.Attach();
            new Position() { Id = 4, Code = "4", Name = "Sub Position", ParentId = 3 }.Attach();
            await Position.DBTable.Save();

            var random = new Random();
            for (var i = 1; i < 100; i++)
            {
                new Employer()
                {
                    Id = i,
                    Identifier = $"{i:8}",
                    PositionId = random.Next(1, 4),
                    IsActive = true,
                    Age = (byte)random.Next(18, 60),
                    Days = (short)random.Next(1, 16000),
                    LongId = 120321312321L,
                    Weight = 123.12333F,
                    DWeight = 123.1233433424434D,
                    Salary = 231323.32M,
                    Name = $"Ivan{i:3}",
                    Access = new AccessValue(new[]
                   {
                    new AccessItem(AccessValue.Groups.GetById(1), AccessType.Read | AccessType.Download),
                    new AccessItem(AccessValue.Groups.GetById(2), AccessType.Admin),
                    new AccessItem(AccessValue.Groups.GetById(3), AccessType.Read | AccessType.Create | AccessType.Update)
                })
                }.Attach();
            }
            await Employer.DBTable.Save();
        }

        [Test]
        public void BinarySerialize()
        {
            var positions = ((IEnumerable<Position>)Position.DBTable).ToList();
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
    }
}
