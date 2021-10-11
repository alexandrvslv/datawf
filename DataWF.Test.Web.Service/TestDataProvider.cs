using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Common;
using DataWF.Module.Flow;
using System;
using System.Threading.Tasks;

namespace DataWF.Test.Web.Service
{
    [Provider]
    [SchemaEntry(typeof(FlowSchema))]
    [SchemaEntry(typeof(FlowSchemaLog))]
    public partial class TestDataProvider : DBProvider
    {
        public FlowSchema Schema => FlowSchema;

        public override void Load()
        {
            base.Load("data.xml");

            if (Schema.Connection == null)
            {
                Schema.Connection = new DBConnection
                {
                    Name = Schema.Name,
                    System = DBSystem.SQLite,
                    DataBase = $"{Schema.Name}.sqlite"
                };
            }
            Schema.ExecuteDropDatabase();
            Generate();
            Schema.ExecuteCreateDatabase();
            Save();
            if (!Schema.Connection.CheckConnection())
            {
                throw new Exception("Check Connection FAIL!");
            }
            Generate();
            CommitChanges();

            Helper.Log(this, "Generate Data");

            foreach (var initializer in Helper.Modules)
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

        public UserTable Users => Schema.User;

        public override IUserIdentity GetUser(string email)
        {
            return Users.GetByEmail(email);
        }
    }


}
