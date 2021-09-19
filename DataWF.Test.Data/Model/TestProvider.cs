using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using DataWF.Common;
using DataWF.Data;

namespace DataWF.Test.Data
{
    public class TestProvider : DBProvider
    {
        public new TestSchema Schema
        {
            get => (TestSchema)base.Schema;
            set => base.Schema = value;
        }

        public override async Task<DBSchema> CreateNew()
        {
            Locale.Instance.Culture = CultureInfo.GetCultureInfo("en-US");
            Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            Schems.Clear();
            TableGenerator.ClearGeneratorCache();

            if (Connections.Count == 0)
                Serialization.Deserialize("connections.xml", Connections);

            AccessValue.Provider = new AccessProviderStub
            {
                Groups = new IdCollection<IGroupIdentity>
                {
                    new AccessGroupBung() { Id = 1, Name = "Group1"},
                    new AccessGroupBung() { Id = 2, Name = "Group2"},
                    new AccessGroupBung() { Id = 3, Name = "Group3"}
                }
            };

            Schema = await CreateNew<TestSchema>();

            Schema.Connection = Connections["TestSqlLite"];
            Schema.ExecuteDropDatabase();
            Schema.ExecuteCreateDatabase();

            var positions = Schema.Position;

            new Position(positions) { Id = 1, Code = "1", Name = "First Position" }.Attach();
            new Position(positions) { Id = 2, Code = "2", Name = "Second Position" }.Attach();
            new Position(positions) { Id = 3, Code = "3", Name = "Third Position" }.Attach();
            new Position(positions) { Id = 4, Code = "4", Name = "Sub Position", ParentId = 3 }.Attach();
            new Position(positions) { Id = 5, Code = "5", Name = "Sub Sub Position", ParentId = 4 }.Attach();
            await positions.Save();

            var employers = Schema.Employer;
            var random = new Random();
            for (var i = 1; i <= 100; i++)
            {
                new Employer(employers)
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
            await employers.Save();

            return Schema;
        }
    }
}
