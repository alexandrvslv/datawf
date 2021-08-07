using DataWF.Data;
using DataWF.Module.Common;
using DataWF.Module.Counterpart;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DataWf.Test.Module.Counterparty
{
    [TestFixture()]
    public class Generate
    {
        [Test()]
        public async Task Initialize()
        {
            Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var schema = new CounterpartSchema();
            schema.Generate("wf_customer");
            Assert.IsNotNull(schema.Location);
            Assert.IsNotNull(schema.Continent);
            Assert.IsNotNull(schema.Country);
            Assert.IsNotNull(schema.Address);
            Assert.IsNotNull(schema.Customer);
            Assert.IsNotNull(schema.CustomerAddress);
            Assert.IsNotNull(schema.PersoneIdentify);
            Assert.IsNotNull(schema.CustomerReference);
            DBService.Save();
            schema.Connection = new DBConnection
            {
                Name = "test.common",
                DataBase = "test.common",
                System = DBSystem.SQLite
            };
            schema.DropDatabase();
            schema.CreateDatabase();

            new Continent(schema.Location) { Code = "AF", Name = "Africa" }.Attach();
            new Continent(schema.Location) { Code = "AN", Name = "Antarctica" }.Attach();
            new Continent(schema.Location) { Code = "AS", Name = "Asia" }.Attach();
            new Continent(schema.Location) { Code = "EU", Name = "Europa" }.Attach();
            new Continent(schema.Location) { Code = "EUAS", Name = "Eurasia" }.Attach();
            new Continent(schema.Location) { Code = "NA", Name = "North america" }.Attach();
            new Continent(schema.Location) { Code = "OC", Name = "Oceania" }.Attach();
            new Continent(schema.Location) { Code = "SA", Name = "South america" }.Attach();

            var russia = new Country(schema.Country)
            {
                Parent = schema.Location.LoadByCode("EUAS"),
                Code = "RU",
                Name = "Russia"
            };
            russia.Attach();

            var ruble = new Currency(schema.Currency)
            {
                Parent = russia,
                Code = "RUB",
                Name = "Ruble"
            };
            ruble.Attach();

            Assert.AreEqual(1, schema.GetTable<Country>().Count);
            Assert.AreEqual(1, schema.GetTable<Currency>().Count);

            await schema.Location.Save();

            Assert.AreEqual(1, schema.GetTable<Country>().Count);
            Assert.AreEqual(1, schema.GetTable<Currency>().Count);

            schema.Location.Clear();
            schema.Location.Load().LastOrDefault();

            Assert.AreEqual(1, schema.GetTable<Country>().Count);
            Assert.AreEqual(1, schema.GetTable<Currency>().Count);



        }
    }
}
