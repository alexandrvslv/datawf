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
    public class CustomerAddress : DBItem
    {
        private Address address;
        private Customer customer;

        public CustomerAddress()
        {
        }

        public CustomerAddressTable CustomerAddressTable => (CustomerAddressTable)Table;

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
            get => GetValue<int?>(CustomerAddressTable.CustomerKey);
            set => SetValue(value, CustomerAddressTable.CustomerKey);
        }

        [Reference(nameof(CustomerId))]
        public Customer Customer
        {
            get => GetReference(CustomerAddressTable.CustomerKey, ref customer);
            set => SetReference(customer = value, CustomerAddressTable.CustomerKey);
        }

        [Browsable(false)]
        [Column("address_id")]
        public int? AddressId
        {
            get => GetValue<int?>(CustomerAddressTable.AddressKey);
            set => SetValue(value, CustomerAddressTable.AddressKey);
        }

        [Reference(nameof(AddressId))]
        public Address Address
        {
            get => GetReference(CustomerAddressTable.AddressKey, ref address);
            set => SetReference(address = value, CustomerAddressTable.AddressKey);
        }
    }
}
