﻿using DataWF.Common;
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
            var schema1 = GenerateSchema("test_schema1");
            schema1.Position.GeneratePositions();
            await schema1.Position.Save();

            var schema2 = GenerateSchema("test_schema2");

            var rService1 = new ReplicationService(new ReplicationSettings
            {
                Instance = new RSInstance
                {
                    Url = "tcp://localhost:51001"
                },
                Instances = new List<RSInstance>
                {
                    new RSInstance
                    {
                        Url = "tcp://localhost:51002"
                    }
                },
                Schems = new List<RSSchema>(new[] { new RSSchema { SchemaName = schema1.Name } })
            },
            new TcpSocketService { });
            rService1.Start();



            var rService2 = new ReplicationService(new ReplicationSettings
            {
                Instance = new RSInstance
                {
                    Url = "tcp://localhost:51002"
                },
                Instances = new List<RSInstance>
                {
                    new RSInstance
                    {
                         Url = "tcp://localhost:51001"
                    }
                },
                Schems = new List<RSSchema>(new[] { new RSSchema { SchemaName = schema2.Name } })
            },
            new TcpSocketService { });

            await rService2.SignIn();
            await rService2.Synch();

            Assert.AreEqual(6, schema2.Position.Count, "Fail Synch");
        }

        private static TestSchema GenerateSchema(string name)
        {
            var schema1 = new TestSchema()
            {
                Name = name,
                Connection = new DBConnection
                {
                    System = DBSystem.SQLite,
                    DataBase = name + ".db",
                    DataBaseId = 1
                }
            };
            schema1.Generate(name);
            DBService.Schems.Add(schema1);
            return schema1;
        }
    }
}
