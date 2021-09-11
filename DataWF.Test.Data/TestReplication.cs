using DataWF.Common;
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
        private TestSchema schema1;
        private TestSchema schema2;
        private ReplicationService rService1;
        private ReplicationService rService2;
        private TestProvider provider;

        [SetUp]
        public void SetUp()
        {
            provider = new TestProvider();
            schema1 = GenerateSchema(provider, "test_schema1");
            schema2 = GenerateSchema(provider, "test_schema2");
            rService1 = new ReplicationService(new ReplicationSettings
            {
                Instance = new RSInstance
                {
                    Url = "tcp://localhost:51001"
                },
                Instances = new SelectableList<RSInstance>
                {
                    new RSInstance
                    {
                        Url = "tcp://localhost:51002"
                    }
                },
                Schems = new SelectableList<RSSchema>(new[] { new RSSchema { SchemaName = schema1.Name } })
            },
            new TcpSocketService { },
            provider);

            rService2 = new ReplicationService(new ReplicationSettings
            {
                Instance = new RSInstance
                {
                    Url = "tcp://localhost:51002"
                },
                Instances = new SelectableList<RSInstance>
                {
                    new RSInstance
                    {
                         Url = "tcp://localhost:51001"
                    }
                },
                Schems = new SelectableList<RSSchema>(new[] { new RSSchema { SchemaName = schema2.Name } })
            },
            new TcpSocketService { },
            provider);
        }

        [Test]
        public async Task Synchronisation()
        {
            schema1.Position.GeneratePositions();
            await schema1.Position.Save();

            rService1.Start();
            rService2.Start();

            await rService1.SignIn();
            await rService2.SignIn();

            await rService2.Synch();

            Assert.AreEqual(6, schema2.Position.Count, "Fail Synch");
        }

        private static TestSchema GenerateSchema(TestProvider provider, string name)
        {
            var schema = new TestSchema()
            {
                Name = name,
                Provider = provider,
                Connection = new DBConnection
                {
                    System = DBSystem.SQLite,
                    DataBase = name + ".db",
                    DataBaseId = 1
                }
            };
            schema.Generate(name);
            provider.Schems.Add(schema);
            try { schema.ExecuteDropDatabase(); } catch { }
            schema.ExecuteCreateDatabase();
            return schema;
        }
    }
}
