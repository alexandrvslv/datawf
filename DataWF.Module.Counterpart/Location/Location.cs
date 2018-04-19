/*
 Location.cs
 
 Author:
      Alexandr <alexandr_vslv@mail.ru>

 This program is free software: you can redistribute it and/or modify
 it under the terms of the GNU Lesser General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 This program is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 GNU Lesser General Public License for more details.

 You should have received a copy of the GNU Lesser General Public License
 along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using DataWF.Data;
using System.ComponentModel;
using DataWF.Common;
using System.Runtime.Serialization;
using System.Globalization;
using System.Collections.Generic;

namespace DataWF.Module.Counterpart
{
    public enum LocationType
    {
        None = 0,
        Continent = 1,
        Country = 2,
        Currency = 3,
        Region = 4,
        City = 5,
        Vilage = 6
    }

    public class LocationList : DBTableView<Location>
    {
        public LocationList() : base()
        { }
    }

    [DataContract, Table("wf_customer", "rlocation", "Address", BlockSize = 100)]
    public class Location : DBItem
    {
        public static void Generate()
        {
            var euas = new Location { LocationType = Counterpart.LocationType.Continent, Code = "EUAS", Name = "Eurasia" }; euas.Attach();
            new Location { LocationType = Counterpart.LocationType.Continent, Code = "AF", CodeI = "", Name = "Africa" }.Attach();
            new Location { LocationType = Counterpart.LocationType.Continent, Code = "AN", Name = "Antarctica" }.Attach();
            new Location { LocationType = Counterpart.LocationType.Continent, Code = "AS", Name = "Asia" }.Attach();
            new Location { LocationType = Counterpart.LocationType.Continent, Code = "EU", Name = "Europa" }.Attach();
            new Location { LocationType = Counterpart.LocationType.Continent, Code = "NA", Name = "North america" }.Attach();
            new Location { LocationType = Counterpart.LocationType.Continent, Code = "OC", Name = "Oceania" }.Attach();
            new Location { LocationType = Counterpart.LocationType.Continent, Code = "SA", Name = "South america" }.Attach();

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
                        country.Attach();
                        var currency = DBTable.LoadByCode(region.ISOCurrencySymbol);
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
                            currency.Attach();
                        }
                    }
                }
                catch (Exception ex)
                { }
            }

            var russia = Location.DBTable.LoadByCode("RU");
            russia.Parent = euas;

            var kazakh = Location.DBTable.LoadByCode("KZ");
            kazakh.Parent = euas;

            new Location { LocationType = Counterpart.LocationType.City, Parent = russia, Code = "495", Name = "Moskow" }.Attach();
            new Location { LocationType = Counterpart.LocationType.City, Parent = kazakh, Code = "727", Name = "Almaty" }.Attach();
            new Location { LocationType = Counterpart.LocationType.City, Parent = kazakh, Code = "7172", Name = "Astana" }.Attach();
            new Location { LocationType = Counterpart.LocationType.City, Parent = kazakh, Code = "7122", Name = "Atyrau" }.Attach();
            new Location { LocationType = Counterpart.LocationType.City, Parent = kazakh, Code = "7292", Name = "Aktau" }.Attach();
            Location.DBTable.Save();
        }

        public static DBTable<Location> DBTable
        {
            get { return DBService.GetTable<Location>(); }
        }

        public Location()
        {
            Build(DBTable);
        }

        [DataMember, Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get { return GetValue<int?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }

        [DataMember, Column("typeid", Keys = DBColumnKeys.ElementType), Index("rlocation_typeid_code", true)]
        public LocationType? LocationType
        {
            get { return GetValue<LocationType?>(Table.ElementTypeKey); }
            set { SetValue(value, Table.ElementTypeKey); }
        }

        [DataMember, Column("code", 40, Keys = DBColumnKeys.Code | DBColumnKeys.Indexing), Index("rlocation_typeid_code", true)]
        public string Code
        {
            get { return GetValue<string>(Table.CodeKey); }
            set { SetValue(value, Table.CodeKey); }
        }

        [DataMember, Column("codei", 40)]
        [Index("rlocation_codei")]
        public string CodeI
        {
            get { return GetProperty<string>(nameof(CodeI)); }
            set { SetProperty(value, nameof(CodeI)); }
        }

        [Browsable(false)]
        [DataMember, Column("parent_id", Keys = DBColumnKeys.Group)]
        [Index("rlocation_parentid")]
        public int? ParentId
        {
            get { return GetValue<int?>(Table.GroupKey); }
            set { SetValue(value, Table.GroupKey); }
        }

        [Reference(nameof(ParentId))]
        public Location Parent
        {
            get { return GetReference<Location>(Table.GroupKey); }
            set { SetReference(value, Table.GroupKey); }
        }

        [DataMember, Column("name", 512, Keys = DBColumnKeys.View | DBColumnKeys.Culture)]
        public override string Name
        {
            get { return GetName(nameof(Name)); }
            set { SetName(nameof(Name), value); }
        }

        public Location GetParent(LocationType parenttype)
        {
            if (LocationType == parenttype)
                return this;
            Location parent = Parent;
            while (parent != null)
            {
                if (parent.LocationType == parenttype)
                    break;
                parent = parent.Parent;
            }
            return parent;
        }
    }
}
