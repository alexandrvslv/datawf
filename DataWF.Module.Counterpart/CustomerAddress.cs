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

    [DataContract, Table("dcustomer_address", "Customer", BlockSize = 100)]
    public class CustomerAddress : DBItem
    {
        private static DBColumn customerKey = DBColumn.EmptyKey;
        private static DBColumn addressKey = DBColumn.EmptyKey;
        private static DBTable<CustomerAddress> dbTable;
        private Address address;
        private Customer customer;

        public static DBColumn CustomerKey => DBTable.ParseProperty(nameof(CustomerId), ref customerKey);
        public static DBColumn AddressKey => DBTable.ParseProperty(nameof(AddressId), ref addressKey);
        public static DBTable<CustomerAddress> DBTable => dbTable ?? (dbTable = GetTable<CustomerAddress>());

        public CustomerAddress()
        {
        }

        [DataMember, Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get { return GetValue<int?>(Table.PrimaryKey); }
            set { this[Table.PrimaryKey] = value; }
        }

        [Browsable(false)]
        [DataMember, Column("customer_id")]
        public int? CustomerId
        {
            get { return GetValue<int?>(CustomerKey); }
            set { SetValue(value, CustomerKey); }
        }

        [Reference(nameof(CustomerId))]
        public Customer Customer
        {
            get { return GetReference(CustomerKey, ref customer); }
            set { customer = SetReference(value, CustomerKey); }
        }

        [Browsable(false)]
        [DataMember, Column("address_id")]
        public int? AddressId
        {
            get { return GetValue<int?>(AddressKey); }
            set { SetValue(value, AddressKey); }
        }

        [Reference(nameof(AddressId))]
        public Address Address
        {
            get { return GetReference(AddressKey,ref address); }
            set { address = SetReference(value, AddressKey); }
        }
    }
}
