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

    [Table("dcustomer", "Customer", BlockSize = 100)]
    public class Customer : DBItem, IDisposable
    {
        public static readonly DBTable<Customer> DBTable = GetTable<Customer>();
        public static readonly DBColumn CodeKey = DBTable.ParseProperty(nameof(Code));
        public static readonly DBColumn NameENKey = DBTable.ParseProperty(nameof(NameEN));
        public static readonly DBColumn NameRUKey = DBTable.ParseProperty(nameof(NameRU));
        public static readonly DBColumn ShortNameENKey = DBTable.ParseProperty(nameof(ShortNameEN));
        public static readonly DBColumn ShortNameRUKey = DBTable.ParseProperty(nameof(ShortNameRU));
        public static readonly DBColumn EMailKey = DBTable.ParseProperty(nameof(EMail));
        public static readonly DBColumn PhoneKey = DBTable.ParseProperty(nameof(Phone));
        public static readonly DBColumn CountryKey = DBTable.ParseProperty(nameof(CountryId));
        public static readonly DBColumn AddressKey = DBTable.ParseProperty(nameof(AddressId));
        public static readonly DBColumn KeyKey = DBTable.ParseProperty(nameof(Key));
        public static readonly DBColumn ExternalIdKey = DBTable.ParseProperty(nameof(ExternalId));
        //public static DBColumn UserKey => DBTable.ParseProperty(nameof(UserId), ref userKey);

        private Address address;
        private Country country;

        public Customer()
        { }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(Table.PrimaryKey);
            set => SetValue(value, Table.PrimaryKey);
        }

        //[Column("typeid", Keys = DBColumnKeys.ElementType), Index("dcustomer_typeid")]
        //public CustomerType? CustomerType
        //{
        //    get { return GetValue<CustomerType?>(Table.ElementTypeKey); }
        //    set { SetValue(value, Table.ElementTypeKey); }
        //}

        [Column("innr", 40, Keys = DBColumnKeys.Code)]
        [Index("dcustomer_inn", true)]
        public string INN
        {
            get => GetValue<string>(Table.CodeKey);
            set => SetValue(value, Table.CodeKey);
        }

        [Column("code", 40)]
        [Index("dcustomer_code", true)]
        public string Code
        {
            get => GetValue<string>(CodeKey);
            set => SetValue(value, CodeKey);
        }

        [Column("shortname", 512, Keys = DBColumnKeys.View | DBColumnKeys.Culture)]
        public string ShortName
        {
            get => GetName();
            set => SetName(value);
        }

        public string ShortNameEN
        {
            get => GetValue<string>(ShortNameENKey);
            set => SetValue(value, ShortNameENKey);
        }

        public string ShortNameRU
        {
            get => GetValue<string>(ShortNameRUKey);
            set => SetValue(value, ShortNameRUKey);
        }

        [Column("name", 1024, Keys = DBColumnKeys.Culture)]
        public string Name
        {
            get => GetName();
            set => SetName(value);
        }

        public string NameEN
        {
            get => GetValue<string>(NameENKey);
            set => SetValue(value, NameENKey);
        }

        public string NameRU
        {
            get => GetValue<string>(NameRUKey);
            set => SetValue(value, NameRUKey);
        }

        [Column("email", 1024), Index("dcustomer_email")]
        public string EMail
        {
            get => GetValue<string>(EMailKey);
            set => SetValue(value, EMailKey);
        }

        [Column("phone", 1024)]
        public string Phone
        {
            get => GetValue<string>(PhoneKey);
            set => SetValue(value, PhoneKey);
        }

        [Browsable(false)]
        [Column("country_id")]
        public int? CountryId
        {
            get => GetValue<int?>(CountryKey);
            set => SetValue(value, CountryKey);
        }

        [Reference(nameof(CountryId))]
        public Country Country
        {
            get => GetReference(CountryKey, ref country);
            set => SetReference(country = value, CountryKey);
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

        //[Browsable(false)]
        //[Column("user_id")]
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
        [Column("sign_key", 1024, Keys = DBColumnKeys.Password | DBColumnKeys.System)]
        public string Key
        {
            get => GetValue<string>(KeyKey);
            set => SetValue(value, KeyKey);
        }

        [Column("ext_id")]
        public int? ExternalId
        {
            get => GetValue<int?>(ExternalIdKey);
            set => SetValue(value, ExternalIdKey);
        }
    }

}
