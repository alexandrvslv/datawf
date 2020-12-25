using DataWF.Data;
using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace DataWF.Module.Counterpart
{
    public class CustomerList : DBTableView<Customer>
    {
        public CustomerList(DBTable<Customer> table) : base(table)
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
        private Address address;
        private Country country;

        public Customer()
        { }

        public CustomerTable CustomerTable => (CustomerTable)Table;

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(CustomerTable.IdKey);
            set => SetValue(value, CustomerTable.IdKey);
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
            get => GetValue<string>(CustomerTable.INNKey);
            set => SetValue(value, CustomerTable.INNKey);
        }

        [Column("code", 40)]
        [Index("dcustomer_code", true)]
        public string Code
        {
            get => GetValue<string>(CustomerTable.CodeKey);
            set => SetValue(value, CustomerTable.CodeKey);
        }

        [Column("shortname", 512, Keys = DBColumnKeys.View | DBColumnKeys.Culture)]
        public string ShortName
        {
            get => GetName();
            set => SetName(value);
        }

        [CultureKey]
        public string ShortNameEN
        {
            get => GetValue<string>(CustomerTable.ShortNameENKey);
            set => SetValue(value, CustomerTable.ShortNameENKey);
        }

        [CultureKey]
        public string ShortNameRU
        {
            get => GetValue<string>(CustomerTable.ShortNameRUKey);
            set => SetValue(value, CustomerTable.ShortNameRUKey);
        }

        [Column("name", 1024, Keys = DBColumnKeys.Culture)]
        public string Name
        {
            get => GetName();
            set => SetName(value);
        }

        [CultureKey]
        public string NameEN
        {
            get => GetValue<string>(CustomerTable.NameENKey);
            set => SetValue(value, CustomerTable.NameENKey);
        }

        [CultureKey]
        public string NameRU
        {
            get => GetValue<string>(CustomerTable.NameRUKey);
            set => SetValue(value, CustomerTable.NameRUKey);
        }

        [Column("email", 1024), Index("dcustomer_email")]
        public string EMail
        {
            get => GetValue<string>(CustomerTable.EMailKey);
            set => SetValue(value, CustomerTable.EMailKey);
        }

        [Column("phone", 1024)]
        public string Phone
        {
            get => GetValue<string>(CustomerTable.PhoneKey);
            set => SetValue(value, CustomerTable.PhoneKey);
        }

        [Browsable(false)]
        [Column("country_id")]
        public int? CountryId
        {
            get => GetValue<int?>(CustomerTable.CountryKey);
            set => SetValue(value, CustomerTable.CountryKey);
        }

        [Reference(nameof(CountryId))]
        public Country Country
        {
            get => GetReference(CustomerTable.CountryKey, ref country);
            set => SetReference(country = value, CustomerTable.CountryKey);
        }

        [Browsable(false)]
        [Column("address_id")]
        public int? AddressId
        {
            get => GetValue<int?>(CustomerTable.AddressKey);
            set => SetValue(value, CustomerTable.AddressKey);
        }

        [Reference(nameof(AddressId))]
        public Address Address
        {
            get => GetReference(CustomerTable.AddressKey, ref address);
            set => SetReference(address = value, CustomerTable.AddressKey);
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
        public string Sign
        {
            get => GetValue<string>(CustomerTable.SignKey);
            set => SetValue(value, CustomerTable.SignKey);
        }

        [Column("ext_id")]
        public int? ExternalId
        {
            get => GetValue<int?>(CustomerTable.ExternalKey);
            set => SetValue(value, CustomerTable.ExternalKey);
        }
    }

}
