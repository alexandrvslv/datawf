using NUnit.Framework;
using System;
using DataWF.Module.Common;
using DataWF.Module.Customer;
using DataWF.Data;

namespace DataWf.Test.Module.Customer
{
    [TestFixture()]
    public class Generate
    {
        [Test()]
        public void Initialize()
        {
            Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var schema = new DBSchema("wf_customer");
            DBService.Generate(typeof(User).Assembly, schema);
            DBService.Generate(typeof(Address).Assembly, schema);
            Assert.IsNotNull(DataWF.Module.Customer.Location.DBTable);
            Assert.IsNotNull(DataWF.Module.Customer.Address.DBTable);
            Assert.IsNotNull(DataWF.Module.Customer.Customer.DBTable);
            Assert.IsNotNull(DataWF.Module.Customer.CustomerAddress.DBTable);
            DBService.Save();
            schema.Connection = new DBConnection
            {
                Name = "test.common",
                Host = "localhost",
                Port = 5432,
                User = "test",
                Password = "test",
                System = DBSystem.Postgres,
                Schema = "public"
            };

            schema.CreateDatabase();
        }
    }
}
