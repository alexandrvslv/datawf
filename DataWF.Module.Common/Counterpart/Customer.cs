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
    public partial class Customer : DBItem, IDisposable
    {
        private Address address;
        private Country country;

        public Customer(DBTable table) : base(table)
        { }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int Id
        {
            get => GetValue(Table.IdKey);
            set => SetValue(value, Table.IdKey);
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
            get => GetValue(Table.INNKey);
            set => SetValue(value, Table.INNKey);
        }

        [Column("code", 40)]
        [Index("dcustomer_code", true)]
        public string Code
        {
            get => GetValue(Table.CodeKey);
            set => SetValue(value, Table.CodeKey);
        }

        [Column("shortname", 512, Keys = DBColumnKeys.View | DBColumnKeys.Culture)]
        public string ShortName
        {
            get => GetName();
            set => SetName(value);
        }

        [CultureKey(nameof(ShortName))]
        public string ShortNameEN
        {
            get => GetValue(Table.ShortNameENKey);
            set => SetValue(value, Table.ShortNameENKey);
        }

        [CultureKey(nameof(ShortName))]
        public string ShortNameRU
        {
            get => GetValue(Table.ShortNameRUKey);
            set => SetValue(value, Table.ShortNameRUKey);
        }

        [Column("name", 1024, Keys = DBColumnKeys.Culture)]
        public string Name
        {
            get => GetName();
            set => SetName(value);
        }

        [CultureKey(nameof(Name))]
        public string NameEN
        {
            get => GetValue(Table.NameENKey);
            set => SetValue(value, Table.NameENKey);
        }

        [CultureKey(nameof(Name))]
        public string NameRU
        {
            get => GetValue(Table.NameRUKey);
            set => SetValue(value, Table.NameRUKey);
        }

        [Column("email", 1024), Index("dcustomer_email")]
        public string EMail
        {
            get => GetValue(Table.EMailKey);
            set => SetValue(value, Table.EMailKey);
        }

        [Column("phone", 1024)]
        public string Phone
        {
            get => GetValue(Table.PhoneKey);
            set => SetValue(value, Table.PhoneKey);
        }

        [Browsable(false)]
        [Column("country_id")]
        public int? CountryId
        {
            get => GetValue(Table.CountryIdKey);
            set => SetValue(value, Table.CountryIdKey);
        }

        [Reference(nameof(CountryId))]
        public Country Country
        {
            get => GetReference((DBColumn)Table.CountryIdKey, ref country);
            set => SetReference(country = value, Table.CountryIdKey);
        }

        [Browsable(false)]
        [Column("address_id")]
        public int? AddressId
        {
            get => GetValue(Table.AddressIdKey);
            set => SetValue(value, Table.AddressIdKey);
        }

        [Reference(nameof(AddressId))]
        public Address Address
        {
            get => GetReference((DBColumn)Table.AddressIdKey, ref address);
            set => SetReference(address = value, Table.AddressIdKey);
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
            get => GetValue(Table.SignKey);
            set => SetValue(value, Table.SignKey);
        }

        [Column("ext_id")]
        public int? ExternalId
        {
            get => GetValue(Table.ExternalIdKey);
            set => SetValue(value, Table.ExternalIdKey);
        }
    }

}
