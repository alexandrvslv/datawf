using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using DataWF.Data;
using DataWF.Module.Common;

namespace DataWF.Module.Customer
{
    public class CustomerAddressList : DBTableView<CustomerAddress>
    {
        public CustomerAddressList()
            : base(CustomerAddress.DBTable)
        {
        }

    }

    [Table("wf_customer", "caddress", BlockSize = 2000)]
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

        [Column("id", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get { return GetValue<int?>(Table.PrimaryKey); }
            set { this[Table.PrimaryKey] = value; }
        }

        [Browsable(false)]
        [Column("typeid", Keys = DBColumnKeys.Type)]
        public int? TypeId
        {
            get { return GetProperty<int?>(nameof(TypeId)); }
            set { SetProperty(value, nameof(TypeId)); }
        }

        [Reference("fk_caddress_typeid", nameof(TypeId))]
        public Book Type
        {
            get { return GetPropertyReference<Book>(nameof(TypeId)); }
            set { SetPropertyReference(value, nameof(TypeId)); }
        }

        [Browsable(false)]
        [Column("customerid")]
        public int? CustomerId
        {
            get { return GetValue<int?>(Table.ParseColumn(nameof(CustomerId))); }
            set { SetProperty(value, nameof(CustomerId)); }
        }

        [Reference("dcaddress_customerid", nameof(CustomerId))]
        public Customer Customer
        {
            get { return GetPropertyReference<Customer>(nameof(CustomerId)); }
            set { SetPropertyReference(value, nameof(CustomerId)); }
        }

        [Browsable(false)]
        [Column("addressid")]
        public int? AddressId
        {
            get { return GetProperty<int?>(nameof(AddressId)); }
            set { SetProperty(value, nameof(AddressId)); }
        }

        [Reference("dcaddress_customerid", nameof(CustomerId))]
        public Address Address
        {
            get { return GetPropertyReference<Address>(nameof(AddressId)); }
            set { SetPropertyReference(value, nameof(AddressId)); }
        }
    }
}
