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
using System.Runtime.Serialization;

namespace DataWF.Module.Counterpart
{
    public class CustomerList : DBTableView<Customer>
    {
        public CustomerList() : base()
        {
        }
    }

    public enum CustomerType
    {
        None = 0,
        Company = 1,
        Persone = 2,
    }

    [DataContract, Table("dcustomer", "Customer", BlockSize = 100)]
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

        [DataMember, Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get { return GetValue<int?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }

        //[DataMember, Column("typeid", Keys = DBColumnKeys.ElementType), Index("dcustomer_typeid")]
        //public CustomerType? CustomerType
        //{
        //    get { return GetValue<CustomerType?>(Table.ElementTypeKey); }
        //    set { SetValue(value, Table.ElementTypeKey); }
        //}

        [DataMember, Column("innr", 40, Keys = DBColumnKeys.Code)]
        [Index("dcustomer_inn", true)]
        public string INN
        {
            get { return GetValue<string>(Table.CodeKey); }
            set { SetValue(value, Table.CodeKey); }
        }

        [DataMember, Column("shortname", 512, Keys = DBColumnKeys.View | DBColumnKeys.Culture)]
        public string ShortName
        {
            get { return GetName(nameof(ShortName)); }
            set { SetName(nameof(ShortName), value); }
        }

        [DataMember, Column("name", 1024, Keys = DBColumnKeys.Culture)]
        public override string Name
        {
            get { return GetName(nameof(Name)); }
            set { SetName(nameof(Name), value); }
        }

        [Browsable(false)]
        [DataMember, Column("sign_key", 1024, Keys = DBColumnKeys.Password | DBColumnKeys.System)]
        public string Key
        {
            get { return GetProperty<string>(nameof(Key)); }
            set { SetProperty(value, nameof(Key)); }
        }

        [DataMember, Column("email", 1024), Index("dcustomer_email")]
        public string EMail
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        [DataMember, Column("phone", 1024)]
        public string Phone
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        [Browsable(false)]
        [DataMember, Column("country_id")]
        public int? CountryId
        {
            get { return GetProperty<int?>(); }
            set { SetProperty(value); }
        }

        [Reference(nameof(CountryId))]
        public Country Country
        {
            get { return GetPropertyReference<Country>(); }
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

        [Browsable(false)]
        [DataMember, Column("user_id")]
        public int? UserId
        {
            get { return GetProperty<int?>(); }
            set { SetProperty(value); }
        }

        [Browsable(false)]
        [Reference(nameof(UserId))]
        public User User
        {
            get { return GetPropertyReference<User>(); }
            set { SetPropertyReference(value); }
        }


    }

}
