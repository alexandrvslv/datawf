using DataWF.Data;

namespace DataWF.Module.Counterpart
{
    [ItemType((int)Counterpart.LocationType.City)]
    public class City : Location
    {
        public static readonly DBTable<City> VTTable = GetTable<City>();
        public City()
        {
            ItemType = (int)LocationType.City;
        }
    }
}
