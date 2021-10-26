using DataWF.Data;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace DataWF.Module.Counterpart
{
    public class CustomerAddressList : DBTableView<CustomerAddress>
    {
        public CustomerAddressList() : base()
        {
        }

    }

    [Table("dcustomer_address", "Customer", BlockSize = 100)]
    public class CustomerAddress : DBItem
    {
        public static readonly DBTable<CustomerAddress> DBTable = GetTable<CustomerAddress>();
        public static readonly DBColumn CustomerKey = DBTable.ParseProperty(nameof(CustomerId));
        public static readonly DBColumn AddressKey = DBTable.ParseProperty(nameof(AddressId));

        private Address address;
        private Customer customer;

        public CustomerAddress()
        {
        }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(Table.PrimaryKey);
            set => this[Table.PrimaryKey] = value;
        }

        [Browsable(false)]
        [Column("customer_id")]
        public int? CustomerId
        {
            get => GetValue<int?>(CustomerKey);
            set => SetValue(value, CustomerKey);
        }

        [Reference(nameof(CustomerId))]
        public Customer Customer
        {
            get => GetReference(CustomerKey, ref customer);
            set => SetReference(customer = value, CustomerKey);
        }

        [Browsable(false)]
        [Column("address_id")]
        public int? AddressId
        {
            get => GetValue<int?>(AddressKey);
            set => SetValue(value, AddressKey);
        }

        [Reference(nameof(AddressId))]
        public Address Address
        {
            get => GetReference(AddressKey, ref address);
            set => SetReference(address = value, AddressKey);
        }
    }
}
