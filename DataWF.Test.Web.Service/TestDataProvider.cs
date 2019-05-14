using DataWF.Common;
using DataWF.Data;
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
                typeof(Module.Common.User).Assembly,
                typeof(Module.Counterpart.Customer).Assembly,
                typeof(Module.Messanger.Message).Assembly,
                typeof(Module.Flow.Document).Assembly
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
                initializer.Initialize();
            }
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
    }
}
