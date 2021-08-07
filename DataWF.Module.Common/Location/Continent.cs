using DataWF.Data;

namespace DataWF.Module.Counterpart
{
    [VirtualTable((int)Counterpart.LocationType.Continent)]
    public sealed partial class Continent : Location
    {
        public Continent(DBTable table) : base(table)
        {
            ItemType = (int)Counterpart.LocationType.Continent;
        }
    }
}
