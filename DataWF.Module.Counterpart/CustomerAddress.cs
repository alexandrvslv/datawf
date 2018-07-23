using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using DataWF.Data;
using DataWF.Module.Common;

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
        public static DBTable<CustomerAddress> DBTable
        {
            get { return GetTable<CustomerAddress>(); }
        }

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
            get { return GetProperty<int?>(); }
            set { SetProperty(value); }
        }

        [Reference(nameof(CustomerId))]
        public Customer Customer
        {
            get { return GetPropertyReference<Customer>(); }
            set { SetPropertyReference(value); }
        }

        [Browsable(false)]
        [DataMember, Column("address_id")]
        public int? AddressId
        {
            get { return GetProperty<int?>(); }
            set { SetProperty(value); }
        }

        [Reference(nameof(AddressId))]
        public Address Address
        {
            get { return GetPropertyReference<Address>(); }
            set { SetPropertyReference(value); }
        }
    }
}
