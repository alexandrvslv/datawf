using DataWF.Data;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace DataWF.Module.Counterpart
{
    public class CustomerAddressList : DBTableView<CustomerAddress>
    {
        public CustomerAddressList(DBTable<CustomerAddress> table) : base(table)
        {
        }

    }

    [Table("dcustomer_address", "Customer", BlockSize = 100, Type = typeof(CustomerAddressTable))]
    public sealed partial class CustomerAddress : DBItem
    {
        private Address address;
        private Customer customer;

        public CustomerAddress(DBTable table) : base(table)
        {
        }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int Id
        {
            get => GetValue(Table.IdKey);
            set => SetValue(value, Table.IdKey);
        }

        [Browsable(false)]
        [Column("customer_id")]
        public int? CustomerId
        {
            get => GetValue(Table.CustomerIdKey);
            set => SetValue(value, Table.CustomerIdKey);
        }

        [Reference(nameof(CustomerId))]
        public Customer Customer
        {
            get => GetReference((DBColumn)Table.CustomerIdKey, ref customer);
            set => SetReference(customer = value, Table.CustomerIdKey);
        }

        [Browsable(false)]
        [Column("address_id")]
        public int? AddressId
        {
            get => GetValue(Table.AddressIdKey);
            set => SetValue(value, Table.AddressIdKey);
        }

        [Reference(nameof(AddressId))]
        public Address Address
        {
            get => GetReference((DBColumn)Table.AddressIdKey, ref address);
            set => SetReference(address = value, Table.AddressIdKey);
        }
    }
}
