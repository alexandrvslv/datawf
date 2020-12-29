using DataWF.Data;

namespace DataWF.Module.Counterpart
{
    [ItemType((int)Counterpart.LocationType.City)]
    public sealed class City : Location
    {
        public City(DBTable table) : base(table)
        {
            ItemType = (int)LocationType.City;
        }
    }
}
