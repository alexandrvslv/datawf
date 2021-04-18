using DataWF.Data;
using System.ComponentModel;
using DataWF.Common;

namespace DataWF.Module.Counterpart
{
    [ItemType((int)Counterpart.LocationType.Currency)]
    public sealed partial class Currency : Location
    {
        public Currency(DBTable table) : base(table)
        {
            ItemType = (int)Counterpart.LocationType.Currency;
        }

        [Browsable(false)]
        public int? CountryId
        {
            get => ParentId;
            set => ParentId = value;
        }

        public Country Country
        {
            get => Parent as Country;
            set => Parent = value;
        }

    }
}
