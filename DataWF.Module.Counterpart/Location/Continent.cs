using DataWF.Data;

namespace DataWF.Module.Counterpart
{
    [ItemType((int)Counterpart.LocationType.Continent)]
    public class Continent : Location
    {
        public static readonly DBTable<Continent> VTTable = GetTable<Continent>();
        public Continent()
        {
            ItemType = (int)Counterpart.LocationType.Continent;
        }
    }
}
