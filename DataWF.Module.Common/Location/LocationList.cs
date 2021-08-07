using DataWF.Data;

namespace DataWF.Module.Counterpart
{
    public class LocationList : DBTableView<Location>
    {
        public LocationList(LocationTable<Location> table) : base(table)
        { }
    }
}
