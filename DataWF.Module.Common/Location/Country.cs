using DataWF.Data;
using System.ComponentModel;
using DataWF.Common;

namespace DataWF.Module.Counterpart
{
    [VirtualTable((int)Counterpart.LocationType.Country)]
    public sealed partial class Country : Location
    {
        public Country(DBTable table) : base(table)
        {
            ItemType = (int)LocationType.Country;
        }

        public Continent Continent
        {
            get => Parent as Continent;
            set => Parent = value;
        }
    }
}
