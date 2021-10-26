using DataWF.Data;
using System.ComponentModel;
using DataWF.Common;

namespace DataWF.Module.Counterpart
{
    [ItemType((int)Counterpart.LocationType.Country)]
    public class Country : Location
    {
        public Country()
        {
            ItemType = (int)LocationType.Country;
        }

        public static DBTable<Country> VTTable => GetTable<Country>();

        public Continent Continent
        {
            get => Parent as Continent;
            set => Parent = value;
        }
    }
}
