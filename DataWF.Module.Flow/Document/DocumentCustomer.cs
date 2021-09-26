using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Counterpart;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace DataWF.Module.Flow
{
    [Table("ddocument_customer", "Document", BlockSize = 400), InvokerGenerator]
    public partial class DocumentCustomer : DocumentItem
    {
        private Customer customer;
        private Address address;

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public long Id
        {
            get => GetValue(Table.IdKey);
            set => SetValue(value, Table.IdKey);
        }

        [Browsable(false)]
        [Column("customer_id", Keys = DBColumnKeys.View)]
        public int? CustomerId
        {
            get => GetValue(Table.CustomerIdKey);
            set => SetValue(value, Table.CustomerIdKey);
        }

        [Reference(nameof(CustomerId))]
        public Customer Customer
        {
            get => GetReference(Table.CustomerIdKey, ref customer);
            set
            {
                SetReference(customer = value, Table.CustomerIdKey);
                Address = value?.Address;
                EMail = value?.EMail;
                Phone = value?.Phone;
            }
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
            get => GetReference(Table.AddressIdKey, ref address);
            set => SetReference(address = value, Table.AddressIdKey);
        }

        [Column("email", 1024)]
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

        protected override void RaisePropertyChanged(string property)
        {
            base.RaisePropertyChanged(property);
            if (Attached)
            {
                document?.OnReferenceChanged(this);
            }
        }


    }
}
