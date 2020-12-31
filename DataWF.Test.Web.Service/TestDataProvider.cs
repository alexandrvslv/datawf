using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Common;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace DataWF.Test.Web.Service
{
    public class TestDataProvider : DataProviderBase
    {
        public TestDataProvider()
        {
            SchemaName = "test";
        }

        public override void Generate()
        {
            Schema.Generate(new[] {
                typeof(DataWF.Data.Instance).Assembly,
                typeof(Module.Common.User).Assembly,
                typeof(Module.Counterpart.Customer).Assembly,
                typeof(Module.Messanger.Message).Assembly,
                typeof(Module.Flow.Document).Assembly,
                typeof(TestDataProvider).Assembly,
            });
        }

        public override void Load()
        {
            DBService.Load();

            if (Schema == null || Schema.Connection == null)
            {
                CreateNew();
            }
            if (!Schema.Connection.CheckConnection())
            {
                throw new Exception("Check Connection FAIL!");
            }
            Generate();
            DBService.CommitChanges();

            Helper.Logs.Add(new StateInfo("Load", "Database", "Generate Data"));

            foreach (var initializer in Helper.ModuleInitializer)
            {
                initializer.Initialize(new[] { Schema });
            }

            _ = new UserGroup(Schema.GetTable<UserGroup>())
            {
                Id = 1,
                NameEN = "admin",
            }.SaveOrUpdate((IUserIdentity)null);

            _ = new User(Schema.GetTable<User>())
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
            Schema = new DBSchema()
            {
                Name = schemaName,
                Connection = new DBConnection
                {
                    Name = schemaName,
                    System = DBSystem.SQLite,
                    DataBase = $"{schemaName}.sqlite"
                }
            };

            Generate();
            Schema.DropDatabase();
            Schema.CreateDatabase();
            Save();
            return Task.CompletedTask;
        }

        public UserTable Users => (UserTable)Schema.GetTable<User>();

        public override DBUser FindUser(string email)
        {
            return Users.GetByEmail(email);
        }
    }
    public class Class1
    {
        public int ItemType { get; set; }
    }

    public class Class2 : Class1
    {
        public string Class2Property { get; set; }
    }

    public abstract class Class3 : Class2
    {
        public string Class3Property { get; set; }
    }

    public class Class4 : Class3
    {
        public string Class4Property { get; set; }
    }

    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        [HttpGet]
        public Class4 Get()
        {
            return new Class4();
        }
    }
}
