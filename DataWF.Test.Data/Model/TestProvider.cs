using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataWF.Data;

namespace DataWF.Test.Data.Model
{
    public class TestProvider : DBProvider
    {
        public new TestSchema Schema
        {
            get => (TestSchema)base.Schema;
            set => base.Schema = value;
        }

        public override async Task CreateNew()
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

            Schema = new TestSchema();
            Schema.Generate("");
            Schema.Connection = DBService.Connections["TestSqlLite"];
            Schema.ExecuteDropDatabase();
            Schema.ExecuteCreateDatabase();

            positionTable = Schema.Position;

            new Position(positionTable) { Id = 1, Code = "1", Name = "First Position" }.Attach();
            new Position(positionTable) { Id = 2, Code = "2", Name = "Second Position" }.Attach();
            new Position(positionTable) { Id = 3, Code = "3", Name = "Third Position" }.Attach();
            new Position(positionTable) { Id = 4, Code = "4", Name = "Sub Position", ParentId = 3 }.Attach();
            await positionTable.Save();

            employerTable = Schema.Employer;
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
    }
}
