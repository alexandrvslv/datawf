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
    public sealed class CustomerAddress : DBItem
    {
        private Address address;
        private Customer customer;

        public CustomerAddress(DBTable table) : base(table)
        {
        }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(CustomerAddressTable.IdKey);
            set => SetValue(value, CustomerAddressTable.IdKey);
        }

        [Browsable(false)]
        [Column("customer_id")]
        public int? CustomerId
        {
            get => GetValue<int?>(CustomerAddressTable.CustomerIdKey);
            set => SetValue(value, CustomerAddressTable.CustomerIdKey);
        }

        [Reference(nameof(CustomerId))]
        public Customer Customer
        {
            get => GetReference(CustomerAddressTable.CustomerIdKey, ref customer);
            set => SetReference(customer = value, CustomerAddressTable.CustomerIdKey);
        }

        [Browsable(false)]
        [Column("address_id")]
        public int? AddressId
        {
            get => GetValue<int?>(CustomerAddressTable.AddressIdKey);
            set => SetValue(value, CustomerAddressTable.AddressIdKey);
        }

        [Reference(nameof(AddressId))]
        public Address Address
        {
            get => GetReference(CustomerAddressTable.AddressIdKey, ref address);
            set => SetReference(address = value, CustomerAddressTable.AddressIdKey);
        }
    }
}
