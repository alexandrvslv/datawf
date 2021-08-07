using DataWF.Data;

namespace DataWF.Module.Counterpart
{
    [VirtualTable((int)Counterpart.LocationType.City)]
    public sealed partial class City : Location
    {
        public City(DBTable table) : base(table)
        {
            ItemType = (int)LocationType.City;
        }
    }
}
