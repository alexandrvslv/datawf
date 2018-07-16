using DataWF.Common;
using DataWF.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace DataWF.Module.Counterpart
{
    public class ModuleInitialize : IModuleInitialize
    {
        public void Initialize()
        {
        }

        public static void GenerateLocations()
        {
            Location.DBTable.Load();
            var euas = new Location { LocationType = LocationType.Continent, Code = "EUAS", Name = "Eurasia" }; euas.Attach();
            new Location { LocationType = LocationType.Continent, Code = "AF", CodeI = "", Name = "Africa" }.Attach();
            new Location { LocationType = LocationType.Continent, Code = "AN", Name = "Antarctica" }.Attach();
            new Location { LocationType = LocationType.Continent, Code = "AS", Name = "Asia" }.Attach();
            new Location { LocationType = LocationType.Continent, Code = "EU", Name = "Europa" }.Attach();
            new Location { LocationType = LocationType.Continent, Code = "NA", Name = "North america" }.Attach();
            new Location { LocationType = LocationType.Continent, Code = "OC", Name = "Oceania" }.Attach();
            new Location { LocationType = LocationType.Continent, Code = "SA", Name = "South america" }.Attach();

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
                        var country = new Location
                        {
                            LocationType = LocationType.Country,
                            Code = region.TwoLetterISORegionName,
                            CodeI = region.ThreeLetterISORegionName
                        };
                        country["name_en"] = region.EnglishName;
                        country["name_ru"] = region.DisplayName;
                        country.Attach();
                        var currency = Location.DBTable.LoadByCode(region.ISOCurrencySymbol);
                        if (currency == null)
                        {
                            currency = new Location
                            {
                                LocationType = LocationType.Currency,
                                Parent = Location.DBTable.Select(Location.DBTable.Columns["name_en"], CompareType.Equal, region.EnglishName).First(),
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

            new Location { LocationType = LocationType.City, Parent = russia, Code = "495", Name = "Moskow" }.Attach();
            new Location { LocationType = LocationType.City, Parent = kazakh, Code = "727", Name = "Almaty" }.Attach();
            new Location { LocationType = LocationType.City, Parent = kazakh, Code = "7172", Name = "Astana" }.Attach();
            new Location { LocationType = LocationType.City, Parent = kazakh, Code = "7122", Name = "Atyrau" }.Attach();
            new Location { LocationType = LocationType.City, Parent = kazakh, Code = "7292", Name = "Aktau" }.Attach();

            Location.DBTable.Save();
        }
    }
}
