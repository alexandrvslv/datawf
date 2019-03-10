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
using DataWF.Data;
using System;
using System.ComponentModel;
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
        private static DBTable<Customer> dbTable;
        private static DBColumn nameENKey = DBColumn.EmptyKey;
        private static DBColumn nameRUKey = DBColumn.EmptyKey;
        private static DBColumn shortNameENKey = DBColumn.EmptyKey;
        private static DBColumn shortNameRUKey = DBColumn.EmptyKey;
        private static DBColumn emailKey = DBColumn.EmptyKey;
        private static DBColumn phoneKey = DBColumn.EmptyKey;
        private static DBColumn countryKey = DBColumn.EmptyKey;
        private static DBColumn addressKey = DBColumn.EmptyKey;
        private Address address;
        private Country country;

        //private static DBColumn userKey = DBColumn.EmptyKey;

        public static DBColumn NameENKey => DBTable.ParseProperty(nameof(NameEN), ref nameENKey);
        public static DBColumn NameRUKey => DBTable.ParseProperty(nameof(NameRU), ref nameRUKey);
        public static DBColumn ShortNameENKey => DBTable.ParseProperty(nameof(ShortNameEN), ref shortNameENKey);
        public static DBColumn ShortNameRUKey => DBTable.ParseProperty(nameof(ShortNameRU), ref shortNameRUKey);
        public static DBColumn EMailKey => DBTable.ParseProperty(nameof(EMail), ref emailKey);
        public static DBColumn PhoneKey => DBTable.ParseProperty(nameof(Phone), ref phoneKey);
        public static DBColumn CountryKey => DBTable.ParseProperty(nameof(CountryId), ref countryKey);
        public static DBColumn AddressKey => DBTable.ParseProperty(nameof(AddressId), ref addressKey);
        //public static DBColumn UserKey => DBTable.ParseProperty(nameof(UserId), ref userKey);
        public static DBTable<Customer> DBTable => dbTable ?? (dbTable = GetTable<Customer>());

        public Customer()
        { }

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
            get { return GetName(); }
            set { SetName(value); }
        }

        public string ShortNameEN
        {
            get { return GetValue<string>(ShortNameENKey); }
            set { SetValue(value, ShortNameENKey); }
        }

        public string ShortNameRU
        {
            get { return GetValue<string>(ShortNameRUKey); }
            set { SetValue(value, ShortNameRUKey); }
        }

        [DataMember, Column("name", 1024, Keys = DBColumnKeys.Culture)]
        public string Name
        {
            get { return GetName(); }
            set { SetName(value); }
        }

        public string NameEN
        {
            get { return GetValue<string>(NameENKey); }
            set { SetValue(value, NameENKey); }
        }

        public string NameRU
        {
            get { return GetValue<string>(NameRUKey); }
            set { SetValue(value, NameRUKey); }
        }

        [DataMember, Column("email", 1024), Index("dcustomer_email")]
        public string EMail
        {
            get { return GetValue<string>(EMailKey); }
            set { SetValue(value, EMailKey); }
        }

        [DataMember, Column("phone", 1024)]
        public string Phone
        {
            get { return GetValue<string>(PhoneKey); }
            set { SetValue(value, PhoneKey); }
        }

        [Browsable(false)]
        [DataMember, Column("country_id")]
        public int? CountryId
        {
            get { return GetValue<int?>(CountryKey); }
            set { SetValue(value, CountryKey); }
        }

        [Reference(nameof(CountryId))]
        public Country Country
        {
            get { return GetReference(CountryKey, ref country); }
            set { country = SetReference(value, CountryKey); }
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
            get { return GetReference(AddressKey, ref address); }
            set { address = SetReference(value, AddressKey); }
        }

        //[Browsable(false)]
        //[DataMember, Column("user_id")]
        //public int? UserId
        //{
        //    get { return GetValue<int?>(UserKey); }
        //    set { SetValue(value, UserKey); }
        //}

        //[Browsable(false)]
        //[Reference(nameof(UserId))]
        //public User User
        //{
        //    get { return GetReference<User>(UserKey); }
        //    set { SetReference(value, UserKey); }
        //}

        [Browsable(false)]
        [DataMember, Column("sign_key", 1024, Keys = DBColumnKeys.Password | DBColumnKeys.System)]
        public string Key
        {
            get { return GetProperty<string>(nameof(Key)); }
            set { SetProperty(value, nameof(Key)); }
        }


    }

}
