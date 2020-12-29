using DataWF.Data;

namespace DataWF.Module.Counterpart
{
    [ItemType((int)Counterpart.LocationType.Continent)]
    public sealed class Continent : Location
    {

        public Continent(DBTable table) : base(table)
        {
            ItemType = (int)Counterpart.LocationType.Continent;
        }
    }
}
