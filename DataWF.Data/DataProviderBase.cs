
using DataWF.Common;
using System;
using System.Threading.Tasks;

namespace DataWF.Data
{
    public abstract class DataProviderBase : IDataProvider
    {
        private DBSchema schema;
        private string schemaName = "example";

        public string SchemaName
        {
            get => schemaName;
            set => schemaName = value;
        }

        public DBSchema Schema
        {
            get => schema ?? (schema = DBService.Schems[schemaName]);
            set => DBService.Schems[schemaName] = schema = value;
        }

        public virtual Task CreateNew()
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

            //Schema.DropDatabase();

            Schema.CreateDatabase();
            Save();
            return null;
        }

        public abstract void Generate();

        public virtual void Load()
        {
            DBService.Load();

            if (Schema == null || Schema.Connection == null)
            {
                throw new Exception("Missing data.xml or connection.xml");
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



        public virtual void Save()
        {
            DBService.Save();
        }
    }
}