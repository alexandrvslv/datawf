using NUnit.Framework;
using System;
using DataWF.Module.Common;
using DataWF.Module.Counterpart;
using DataWF.Data;

namespace DataWf.Test.Module.Counterparty
{
    [TestFixture()]
    public class Generate
    {
        [Test()]
        public void Initialize()
        {
            Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var schema = new DBSchema("wf_customer");
            DBService.Generate(new[] { typeof(User).Assembly, typeof(Customer).Assembly }, schema);
            Assert.IsNotNull(Location.DBTable);
            Assert.IsNotNull(Address.DBTable);
            Assert.IsNotNull(Customer.DBTable);
            Assert.IsNotNull(CustomerAddress.DBTable);
            Assert.IsNotNull(PersoneIdentify.DBTable);
            Assert.IsNotNull(CustomerReference.DBTable);
            DBService.Save();
            schema.Connection = new DBConnection
            {
                Name = "test.common",
                DataBase = "test.common",
                System = DBSystem.SQLite
            };

            schema.CreateDatabase();

            Location.DBTable.Add(new Location { LocationType = LocationType.Continent, Code = "AF", Name = "Africa" });
            Location.DBTable.Add(new Location { LocationType = LocationType.Continent, Code = "AN", Name = "Antarctica" });
            Location.DBTable.Add(new Location { LocationType = LocationType.Continent, Code = "AS", Name = "Asia" });
            Location.DBTable.Add(new Location { LocationType = LocationType.Continent, Code = "EU", Name = "Europa" });
            Location.DBTable.Add(new Location { LocationType = LocationType.Continent, Code = "EUAS", Name = "Eurasia" });
            Location.DBTable.Add(new Location { LocationType = LocationType.Continent, Code = "NA", Name = "North america" });
            Location.DBTable.Add(new Location { LocationType = LocationType.Continent, Code = "OC", Name = "Oceania" });
            Location.DBTable.Add(new Location { LocationType = LocationType.Continent, Code = "SA", Name = "South america" });

            var russia = new Location
            {
                LocationType = LocationType.Country,
                Parent = Location.DBTable.LoadByCode("EUAS"),
                Code = "RU",
                Name = "Russia"
            };
            Location.DBTable.Add(russia);
            var ruble = new Location
            {
                LocationType = LocationType.Currency,
                Parent = russia,
                Code = "RUB",
                Name = "Ruble"
            };
            Location.DBTable.Add(ruble);

            Assert.AreEqual(1, Country.DBTable.Count);
            Assert.AreEqual(1, Currency.DBTable.Count);

            Location.DBTable.Save();

            Assert.AreEqual(1, Country.DBTable.Count);
            Assert.AreEqual(1, Currency.DBTable.Count);

            Location.DBTable.Clear();
            Location.DBTable.Load();

            Assert.AreEqual(1, Country.DBTable.Count);
            Assert.AreEqual(1, Currency.DBTable.Count);



        }
    }
}
