using DataWF.Common;
using DataWF.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DataWF.Module.Counterpart
{
    public class ModuleInitialize : IModuleInitialize
    {
        public void Initialize()
        {
            Generate();
        }

        public void Generate()
        {
            var euas = new Location { LocationType = LocationType.Continent, Code = "EUAS", Name = "Eurasia" }; euas.SaveOrUpdate();
            new Location { LocationType = LocationType.Continent, Code = "AF", CodeI = "", Name = "Africa" }.SaveOrUpdate();
            new Location { LocationType = LocationType.Continent, Code = "AN", Name = "Antarctica" }.SaveOrUpdate();
            new Location { LocationType = LocationType.Continent, Code = "AS", Name = "Asia" }.SaveOrUpdate();
            new Location { LocationType = LocationType.Continent, Code = "EU", Name = "Europa" }.SaveOrUpdate();
            new Location { LocationType = LocationType.Continent, Code = "NA", Name = "North america" }.SaveOrUpdate();
            new Location { LocationType = LocationType.Continent, Code = "OC", Name = "Oceania" }.SaveOrUpdate();
            new Location { LocationType = LocationType.Continent, Code = "SA", Name = "South america" }.SaveOrUpdate();

            CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ru-RU");
            var cultures = CultureInfo.GetCultures(CultureTypes.AllCultures & ~CultureTypes.NeutralCultures);
            var cultureList = new List<string>();
            foreach (CultureInfo culture in cultures)
            {
                //pass the current culture's Locale ID (http://msdn.microsoft.com/en-us/library/0h88fahh.aspx)
                //to the RegionInfo contructor to gain access to the information for that culture
                try
                {
                    RegionInfo region = new RegionInfo(culture.LCID);

                    //make sure out generic list doesnt already
                    //contain this country
                    if (!(cultureList.Contains(region.EnglishName)))
                    {
                        //not there so add the EnglishName (http://msdn.microsoft.com/en-us/library/system.globalization.regioninfo.englishname.aspx)
                        //value to our generic list
                        cultureList.Add(region.EnglishName);
                        var country = new Location
                        {
                            LocationType = Counterpart.LocationType.Country,
                            Code = region.TwoLetterISORegionName,
                            CodeI = region.ThreeLetterISORegionName
                        };
                        country["name_en"] = region.EnglishName;
                        country["name_ru"] = region.DisplayName;
                        country.SaveOrUpdate();
                        var currency = Location.DBTable.LoadByCode(region.ISOCurrencySymbol);
                        if (currency == null)
                        {
                            currency = new Location
                            {
                                LocationType = Counterpart.LocationType.Currency,
                                Parent = country,
                                Code = region.ISOCurrencySymbol,
                                CodeI = region.CurrencySymbol
                            };
                            currency["name_en"] = region.CurrencyEnglishName;
                            //currency["name_ru"] = region.CurrencyNativeName;
                            currency.SaveOrUpdate();
                        }
                    }
                }
                catch { }
            }

            var russia = Location.DBTable.LoadByCode("RU");
            russia.Parent = euas;

            var kazakh = Location.DBTable.LoadByCode("KZ");
            kazakh.Parent = euas;

            new Location { LocationType = LocationType.City, Parent = russia, Code = "495", Name = "Moskow" }.SaveOrUpdate();
            new Location { LocationType = LocationType.City, Parent = kazakh, Code = "727", Name = "Almaty" }.SaveOrUpdate();
            new Location { LocationType = LocationType.City, Parent = kazakh, Code = "7172", Name = "Astana" }.SaveOrUpdate();
            new Location { LocationType = LocationType.City, Parent = kazakh, Code = "7122", Name = "Atyrau" }.SaveOrUpdate();
            new Location { LocationType = LocationType.City, Parent = kazakh, Code = "7292", Name = "Aktau" }.SaveOrUpdate();

            Location.DBTable.Trunc();
        }
    }
}
