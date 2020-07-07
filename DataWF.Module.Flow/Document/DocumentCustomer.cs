using DataWF.Data;
using DataWF.Module.Counterpart;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace DataWF.Module.Flow
{
    [Table("ddocument_customer", "Document", BlockSize = 400)]
    public class DocumentCustomer : DBItem, IDocumentDetail
    {
        public static readonly DBTable<DocumentCustomer> DBTable = GetTable<DocumentCustomer>();
        public static readonly DBColumn CustomerKey = DBTable.ParseProperty(nameof(CustomerId));
        public static readonly DBColumn AddressKey = DBTable.ParseProperty(nameof(AddressId));
        public static readonly DBColumn EMailKey = DBTable.ParseProperty(nameof(EMail));
        public static readonly DBColumn PhoneKey = DBTable.ParseProperty(nameof(Phone));
        public static readonly DBColumn DocumentKey = DBTable.ParseProperty(nameof(DocumentId));

        private Customer customer;
        private Address address;
        private Document document;

        public DocumentCustomer()
        { }

        [Browsable(false)]
        [Column("document_id"), Index("ddocument_customer_document_id")]
        public virtual long? DocumentId
        {
            get => GetValue<long?>(DocumentKey);
            set => SetValue(value, DocumentKey);
        }

        [Reference(nameof(DocumentId))]
        public Document Document
        {
            get => GetReference(DocumentKey, ref document);
            set => SetReference(document = value, DocumentKey);
        }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public long? Id
        {
            get => GetValue<long?>(Table.PrimaryKey);
            set => SetValue(value, Table.PrimaryKey);
        }

        [Browsable(false)]
        [Column("customer_id", Keys = DBColumnKeys.View)]
        public int? CustomerId
        {
            get => GetValue<int?>(CustomerKey);
            set => SetValue(value, CustomerKey);
        }

        [Reference(nameof(CustomerId))]
        public Customer Customer
        {
            get => GetReference(CustomerKey, ref customer);
            set
            {
                SetReference(customer = value, CustomerKey);
                Address = value?.Address;
                EMail = value?.EMail;
                Phone = value?.Phone;
            }
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

        [Column("email", 1024)]
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
