//  The MIT License (MIT)
//
// Copyright © 2020 Vassilyev Alexandr
//
//   email:alexandr_vslv@mail.ru
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the “Software”), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
// and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of 
// the Software.
//
// THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
// THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.

using DataWF.Common;
using System;
using System.Threading.Tasks;

namespace DataWF.Data
{
    public abstract class DataProviderBase : IDBProvider
    {
        private DBSchema schema;
        protected string schemaName = "example";

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
                initializer.Initialize(new[] { Schema });
            }
        }

        public virtual void Save()
        {
            DBService.Save();
        }

        public abstract DBUser FindUser(string email);
    }
}