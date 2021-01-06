using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Common;
using DataWF.Module.Counterpart;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

[assembly: ModuleInitialize(typeof(ModuleInitialize))]
namespace DataWF.Module.Common
{
    public class ModuleInitialize : IModuleInitialize
    {
        public Task Initialize(object[] args)
        {
            var schema = args.FirstOrDefault() as DBSchema;

            schema.GetTable<Book>().Load();

            schema.GetTable<Department>().Load();
            schema.GetTable<Position>().Load();

            var userGoups = (UserGroupTable)schema.GetTable<UserGroup>();
            userGoups.Load();
            userGoups.SetCurrent();

            var users = (UserTable)schema.GetTable<User>();
            users.DefaultComparer = new DBComparer<User, string>(users.LoginKey) { Hash = true };
            users.Load();

            var userRegs = (UserRegTable)schema.GetTable<UserReg>();
            userRegs.DefaultComparer = new DBComparer<UserReg, long?>(userRegs.IdKey) { Hash = true };

            DBLogItem.UserRegTable = userRegs;
            DBService.AddItemLoging(userRegs.OnDBItemLoging);

            var perissions = (GroupPermissionTable)schema.GetTable<GroupPermission>();
            perissions.Load();
            return perissions.CachePermission();
        }

        public static Task GenerateLocations(DBSchema schema)
        {
            var locationTable = schema.GetTable<Location>();
            locationTable.Load();
            var euas = new Continent(locationTable) { Code = "EUAS", Name = "Eurasia" }; euas.Attach();
            new Continent(locationTable) { Code = "AF", CodeI = "", Name = "Africa" }.Attach();
            new Continent(locationTable) { Code = "AN", Name = "Antarctica" }.Attach();
            new Continent(locationTable) { Code = "AS", Name = "Asia" }.Attach();
            new Continent(locationTable) { Code = "EU", Name = "Europa" }.Attach();
            new Continent(locationTable) { Code = "NA", Name = "North america" }.Attach();
            new Continent(locationTable) { Code = "OC", Name = "Oceania" }.Attach();
            new Continent(locationTable) { Code = "SA", Name = "South america" }.Attach();

            CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ru-RU");
            var cultures = CultureInfo.GetCultures(CultureTypes.AllCultures & ~CultureTypes.NeutralCultures);
            foreach (CultureInfo culture in cultures)
            {
                //pass the current culture's Locale ID (http://msdn.microsoft.com/en-us/library/0h88fahh.aspx)
                //to the RegionInfo contructor to gain access to the information for that culture
                if (culture.Parent == CultureInfo.InvariantCulture
                    || culture.IsNeutralCulture)
                    continue;
                try
                {
                    RegionInfo region = new RegionInfo(culture.LCID);

                    //make sure out generic list doesnt already
                    //contain this country
                    if (locationTable.Select(locationTable.Columns["name_en"], CompareType.Equal, region.EnglishName).Count() == 0)
                    {
                        //not there so add the EnglishName (http://msdn.microsoft.com/en-us/library/system.globalization.regioninfo.englishname.aspx)
                        var country = new Country(locationTable)
                        {
                            Code = region.TwoLetterISORegionName,
                            CodeI = region.ThreeLetterISORegionName,
                            NameEN = region.EnglishName,
                            NameRU = region.DisplayName
                        };
                        country.Attach();
                        var currency = locationTable.LoadByCode(region.ISOCurrencySymbol);
                        if (currency == null)
                        {
                            currency = new Currency(locationTable)
                            {
                                Country = country,
                                Code = region.ISOCurrencySymbol,
                                CodeI = region.CurrencySymbol
                            };
                            currency["name_en"] = region.CurrencyEnglishName;
                            //currency["name_ru"] = region.CurrencyNativeName;
                            currency.Attach();
                        }
                    }
                }
                catch { }
            }

            var russia = locationTable.LoadByCode("RU");
            russia.Parent = euas;

            var kazakh = locationTable.LoadByCode("EUAS");
            kazakh.Parent = euas;

            new City(locationTable) { Parent = russia, Code = "495", Name = "Moskow" }.Attach();
            new City(locationTable) { Parent = kazakh, Code = "727", Name = "Almaty" }.Attach();
            new City(locationTable) { Parent = kazakh, Code = "7172", Name = "Astana" }.Attach();
            new City(locationTable) { Parent = kazakh, Code = "7122", Name = "Atyrau" }.Attach();
            new City(locationTable) { Parent = kazakh, Code = "7292", Name = "Aktau" }.Attach();

            return locationTable.Save();
        }
    }
}
