using DataWF.Data;

namespace DataWF.Module.Counterpart
{
    public class AddressList : DBTableView<Address>
    {
        public AddressList(AddressTable table) : base(table)
        { }
    }
}
