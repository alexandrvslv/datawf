using DataWF.Data;

namespace DataWF.Module.Counterpart
{
    [ItemType((int)Counterpart.LocationType.City)]
    public class City : Location
    {
        public City()
        {
            ItemType = (int)LocationType.City;
        }
    }
}
