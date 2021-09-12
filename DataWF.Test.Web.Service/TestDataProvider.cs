using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Common;
using DataWF.Module.Flow;
using System;
using System.Threading.Tasks;

namespace DataWF.Test.Web.Service
{
    [DataProvider()]
    [SchemaEntry(typeof(FlowSchema))]
    public class TestDataProvider : DBProvider
    {
        public TestDataProvider()
        {
            SchemaName = "test";
        }

        public new FlowSchema Schema
        {
            get => (FlowSchema)base.Schema;
            set => base.Schema = value;
        }

        public override void Load()
        {
            base.Load("data.xml");

            if (Schema == null || Schema.Connection == null)
            {
                CreateNew();
            }
            if (!Schema.Connection.CheckConnection())
            {
                throw new Exception("Check Connection FAIL!");
            }
            Generate();
            CommitChanges();

            Helper.Log(this, "Generate Data");

            foreach (var initializer in Helper.ModuleInitializer)
            {
                initializer.Initialize(new[] { Schema });
            }

            _ = new UserGroup(Schema.UserGroup)
            {
                Id = 1,
                NameEN = "admin",
            }.SaveOrUpdate((IUserIdentity)null);

            _ = new User(Schema.User)
            {
                Id = 1,
                Login = "admin",
                EMail = "admin@domain.com",
                AuthType = UserAuthType.Internal,
                Password = "admin1!",
                Super = true,
            }.SaveOrUpdate((IUserIdentity)null);
        }

        public override Task CreateNew()
        {
            Schema = new FlowSchema()
            {
                Name = schemaName,
                Provider = this,
                Connection = new DBConnection
                {
                    Name = schemaName,
                    System = DBSystem.SQLite,
                    DataBase = $"{schemaName}.sqlite"
                }
            };

            Generate();
            Schema.ExecuteDropDatabase();
            Schema.ExecuteCreateDatabase();
            Save();
            return Task.CompletedTask;
        }

        public UserTable Users => Schema.User;

        public override DBUser FindUser(string email)
        {
            return Users.GetByEmail(email);
        }
    }

    
}
