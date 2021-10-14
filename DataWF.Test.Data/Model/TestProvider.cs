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
    [Provider]
    [SchemaEntry(typeof(TestSchema))]
    [SchemaEntry(typeof(TestSchemaLog))]
    public partial class TestProvider : DBProvider
    {
        private IdCollection<IUserGroupIdentity> Groups = new IdCollection<IUserGroupIdentity>
        {
            new AccessGroupBung() { Id = 1, Name = "Group1"},
            new AccessGroupBung() { Id = 2, Name = "Group2"},
            new AccessGroupBung() { Id = 3, Name = "Group3"}
        };

        public override IUserGroupIdentity GetUserGroup(int id) => Groups.GetById(id);

        public override IEnumerable<IAccessIdentity> GetGroups() => Groups;

        public async Task GenerateAsync()
        {
            LoadConnections();

            TableGenerator.ClearGeneratorCache();
            Generate();

            TestSchema.Connection = Connections["TestSqlLite"];
            TestSchema.ExecuteDropDatabase();

            TestSchema.ExecuteCreateDatabase();

            var positions = TestSchema.Position;

            new Position(positions) { Id = 1, Code = "1", Name = "First Position" }.Attach();
            new Position(positions) { Id = 2, Code = "2", Name = "Second Position" }.Attach();
            new Position(positions) { Id = 3, Code = "3", Name = "Third Position" }.Attach();
            new Position(positions) { Id = 4, Code = "4", Name = "Sub Position", ParentId = 3 }.Attach();
            new Position(positions) { Id = 5, Code = "5", Name = "Sub Sub Position", ParentId = 4 }.Attach();
            await positions.Save();

            var employers = TestSchema.Employer;
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
                    Salary = 231323.32M / random.Next(1, 5),
                    Name = $"Name{i,3:0}",
                    Access = new AccessValue(new[]
                    {
                        new AccessItem(GetAccessIdentity(1, IdentityType.Group), AccessType.Read | AccessType.Download),
                        new AccessItem(GetAccessIdentity(2, IdentityType.Group), AccessType.Admin),
                        new AccessItem(GetAccessIdentity(3, IdentityType.Group), AccessType.Read | AccessType.Create | AccessType.Update)
                    }, this)
                }.Attach();
            }
            await employers.Save();

            var employerReferences = TestSchema.EmployerReference;
            foreach (var employer in employers)
            {
                employer.SubEmployers = new[] {
                    new EmployerReference(employerReferences)
                    {
                        Employer = employer,
                        Reference = employers.GetById(random.Next(1, 99))
                    },
                    new EmployerReference(employerReferences)
                    {
                        Employer = employer,
                        Reference = employers.GetById(random.Next(1, 99))
                    },
                    new EmployerReference(employerReferences)
                    {
                        Employer = employer,
                        Reference = employers.GetById(random.Next(1, 99))
                    }
                };
            }
            await employerReferences.Save();
        }

        public void LoadConnections()
        {
            Locale.Instance.Culture = CultureInfo.GetCultureInfo("en-US");
            Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;

            if (Connections.Count == 0)
                Serialization.Deserialize("connections.xml", Connections);
        }
    }
}
