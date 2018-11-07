
using DataWF.Common;
using System;

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

        public virtual void CreateNew()
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
        }

        public abstract void Generate();

        public virtual void Load()
        {
            DBService.Load();
            if (Schema == null
                || Schema.Connection == null
                || (Schema.Connection.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                && !Schema.Connection.CheckConnection()))
            {
                CreateNew();
            }
            else
            {
                Generate();
                DBService.CommitChanges();
            }

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