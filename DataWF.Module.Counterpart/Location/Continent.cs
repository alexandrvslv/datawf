using DataWF.Data;

namespace DataWF.Module.Counterpart
{
    [ItemType((int)Counterpart.LocationType.Continent)]
    public class Continent : Location
    {
        public Continent()
        {
            ItemType = (int)Counterpart.LocationType.Continent;
        }
    }
}
