using DataWF.Data;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataWF.Test.Data
{
    [TestFixture]
    public class TestReplication
    {
        [Test]
        public async Task Synchronisation()
        {
            var schema1 = new TestSchema()
            {
                Name = "test_schema1",
                Connection = new DBConnection
                {
                    System = DBSystem.SQLite,
                    DataBase = "replicant1",
                    DataBaseId = 1
                }
            };
            schema1.Generate("test_schema1");
            DBService.Schems.Add(schema1);
            schema1.Position.GeneratePositions();
            await schema1.Position.Save();

            var rService1 = new ReplicationService(new ReplicationSettings
            {
                Instance = new RSInstance
                {
                    Host = "localhost",
                    Port = 51001
                },
                Instances = new List<RSInstance>
                {
                    new RSInstance
                    {
                        Host = "localhost",
                        Port = 51002,
                    }
                },
                Schems = new List<RSSchema>(new[] { new RSSchema { SchemaName = schema1.Name } })
            });
            rService1.Start();

            var schema2 = new TestSchema()
            {
                Name = "test_schema2",
                Connection = new DBConnection
                {
                    System = DBSystem.SQLite,
                    DataBase = "replicant2",
                    DataBaseId = 2
                }
            };
            schema2.Generate("test_schema2");
            DBService.Schems.Add(schema2);

            var rService2 = new ReplicationService(new ReplicationSettings
            {
                Instance = new RSInstance
                {
                    Host = "localhost",
                    Port = 51002
                },
                Instances = new List<RSInstance>
                {
                    new RSInstance
                    {
                        Host = "localhost",
                        Port = 51001,
                    }
                },
                Schems = new List<RSSchema>(new[] { new RSSchema { SchemaName = schema2.Name } })
            });

            await rService1.SignIn();
            await rService1.Synch();

            Assert.AreEqual(6, schema2.Position.Count, "Fail Synch");
        }
    }
}
