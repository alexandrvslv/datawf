/*
 Customer.cs
 
 Author:
      Alexandr <alexandr_vslv@mail.ru>

 This program is free software: you can redistribute it and/or modify
 it under the terms of the GNU Lesser General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 This program is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 GNU Lesser General Public License for more details.

 You should have received a copy of the GNU Lesser General Public License
 along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
using System;
using System.ComponentModel;
using DataWF.Data;
using DataWF.Common;
using DataWF.Module.Common;

namespace DataWF.Module.Customer
{
    public class CustomerList : DBTableView<Customer>
    {
        public CustomerList()
            : base(Customer.DBTable)
        {
        }
    }

    [Table("wf_customer", "dcustomer", BlockSize = 5000)]
    public class Customer : DBItem, IDisposable
    {
        public static DBTable<Customer> DBTable
        {
            get { return DBService.GetTable<Customer>(); }
        }

        public Customer()
        {
            Build(DBTable);
        }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get { return GetValue<int?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }

        [Column("innr", 40, Keys = DBColumnKeys.Code)]
        [Index("dcustomer_inn", true)]
        public string INN
        {
            get { return GetValue<string>(Table.CodeKey); }
            set { SetValue(value, Table.CodeKey); }
        }

        [Browsable(false)]
        [Column("typeid", Keys = DBColumnKeys.Type)]
        [Index("dcustomer_typeid")]
        public int? TypeId
        {
            get { return GetValue<int?>(Table.TypeKey); }
            set { SetValue(value, Table.TypeKey); }
        }

        [Reference("fk_dcustomer_typeid", nameof(TypeId))]
        public Book Type
        {
            get { return GetReference<Book>(Table.TypeKey); }
            set { SetReference(value, Table.TypeKey); }
        }

        [Column("name", 512, Keys = DBColumnKeys.View | DBColumnKeys.Culture)]
        public override string Name
        {
            get { return GetName("name"); }
            set { SetName("name", value); }
        }

        [Column("login", 200)]
        [Index("dcustomer_login", true)]
        public string Login
        {
            get { return GetProperty<string>(nameof(Login)); }
            set { SetProperty(value, nameof(Login)); }
        }

        [PasswordPropertyText(true)]
        [Column("password", 200, Keys = DBColumnKeys.Password)]
        public string Password
        {
            get { return GetProperty<string>(nameof(Password)); }
            set { SetProperty(value, nameof(Password)); }
        }

        [Column("key", 1024, Keys = DBColumnKeys.Password)]
        public string Key
        {
            get { return GetProperty<string>(nameof(Key)); }
            set { SetProperty(value, nameof(Key)); }
        }

        [Column("email", 1024)]
        [Index("dcustomer_email", true)]
        public string EMail
        {
            get { return GetProperty<string>(nameof(EMail)); }
            set { SetProperty(value, nameof(EMail)); }
        }

        [Browsable(false)]
        [Column("countryid")]
        public int? CountryId
        {
            get { return GetProperty<int?>(nameof(CountryId)); }
            set { SetProperty(value, nameof(CountryId)); }
        }

        [Reference("fk_dcustomer_countryid", nameof(CountryId))]
        public Country Country
        {
            get { return GetPropertyReference<Country>(nameof(CountryId)); }
            set { SetPropertyReference(value, nameof(CountryId)); }
        }

        [Browsable(false)]
        [Column("addressid")]
        public int? AddressId
        {
            get { return GetValue<int?>(Table.ParseColumn(nameof(AddressId))); }
            set { this[Table.ParseColumn(nameof(AddressId))] = value; }
        }

        [Reference("dcustomer_addressid", nameof(AddressId))]
        public Address Address
        {
            get { return GetPropertyReference<Address>(nameof(AddressId)); }
            set { SetPropertyReference(value, nameof(AddressId)); }
        }


    }

}
