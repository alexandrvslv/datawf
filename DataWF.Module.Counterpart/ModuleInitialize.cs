using DataWF.Common;
using DataWF.Module.Counterpart;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

[assembly: ModuleInitialize(typeof(ModuleInitialize))]
namespace DataWF.Module.Counterpart
{
    public class ModuleInitialize : IModuleInitialize
    {
        public Task Initialize()
        {
            return null;
        }

        public static Task GenerateLocations()
        {
            Location.DBTable.Load();
            var euas = new Continent { Code = "EUAS", Name = "Eurasia" }; euas.Attach();
            new Continent { Code = "AF", CodeI = "", Name = "Africa" }.Attach();
            new Continent { Code = "AN", Name = "Antarctica" }.Attach();
            new Continent { Code = "AS", Name = "Asia" }.Attach();
            new Continent { Code = "EU", Name = "Europa" }.Attach();
            new Continent { Code = "NA", Name = "North america" }.Attach();
            new Continent { Code = "OC", Name = "Oceania" }.Attach();
            new Continent { Code = "SA", Name = "South america" }.Attach();

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
                    if (Location.DBTable.Select(Location.DBTable.Columns["name_en"], CompareType.Equal, region.EnglishName).Count() == 0)
                    {
                        //not there so add the EnglishName (http://msdn.microsoft.com/en-us/library/system.globalization.regioninfo.englishname.aspx)
                        var country = new Country
                        {
                            Code = region.TwoLetterISORegionName,
                            CodeI = region.ThreeLetterISORegionName,
                            NameEN = region.EnglishName,
                            NameRU = region.DisplayName
                        };
                        country.Attach();
                        var currency = Location.DBTable.LoadByCode(region.ISOCurrencySymbol);
                        if (currency == null)
                        {
                            currency = new Currency
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

            var russia = Location.DBTable.LoadByCode("RU");
            russia.Parent = euas;

            var kazakh = Location.DBTable.LoadByCode("EUAS");
            kazakh.Parent = euas;

            new City { Parent = russia, Code = "495", Name = "Moskow" }.Attach();
            new City { Parent = kazakh, Code = "727", Name = "Almaty" }.Attach();
            new City { Parent = kazakh, Code = "7172", Name = "Astana" }.Attach();
            new City { Parent = kazakh, Code = "7122", Name = "Atyrau" }.Attach();
            new City { Parent = kazakh, Code = "7292", Name = "Aktau" }.Attach();

            return Location.DBTable.Save();
        }
    }
}
