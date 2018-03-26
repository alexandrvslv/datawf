using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using DataWF.Data;
using DataWF.Module.Common;

namespace DataWF.Module.Counterpart
{
    public class CustomerAddressList : DBTableView<CustomerAddress>
    {
        public CustomerAddressList()
            : base(CustomerAddress.DBTable)
        {
        }

    }

    [Table("wf_customer", "dcustomer_address", "Customer", BlockSize = 2000)]
    public class CustomerAddress : DBItem
    {
        public static DBTable<CustomerAddress> DBTable
        {
            get { return DBService.GetTable<CustomerAddress>(); }
        }

        public CustomerAddress()
        {
            Build(DBTable);
        }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get { return GetValue<int?>(Table.PrimaryKey); }
            set { this[Table.PrimaryKey] = value; }
        }

        [Browsable(false)]
        [Column("customer_id")]
        public int? CustomerId
        {
            get { return GetValue<int?>(Table.ParseColumn(nameof(CustomerId))); }
            set { SetProperty(value, nameof(CustomerId)); }
        }

        [Reference("fk_dcustomer_address_customer_id", nameof(CustomerId))]
        public Customer Customer
        {
            get { return GetPropertyReference<Customer>(nameof(CustomerId)); }
            set { SetPropertyReference(value, nameof(CustomerId)); }
        }

        [Browsable(false)]
        [Column("address_id")]
        public int? AddressId
        {
            get { return GetProperty<int?>(nameof(AddressId)); }
            set { SetProperty(value, nameof(AddressId)); }
        }

        [Reference("fk_dcustomer_address_address_id", nameof(CustomerId))]
        public Address Address
        {
            get { return GetPropertyReference<Address>(nameof(AddressId)); }
            set { SetPropertyReference(value, nameof(AddressId)); }
        }
    }
}
