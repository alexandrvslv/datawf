using DataWF.Data;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace DataWF.Module.Counterpart
{
    [Table("dcustomer_reference", "Customer", Type = typeof(CustomerReferenceTable))]
    public class CustomerReference : DBItem
    {
        private Company company;
        private Persone persone;

        public CustomerReference()
        {
        }
        public CustomerReferenceTable CustomerReferenceTable => (CustomerReferenceTable)Table;

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(CustomerReferenceTable.IdKey);
            set => SetValue(value, CustomerReferenceTable.IdKey);
        }

        [Browsable(false)]
        [Column("company_id")]
        public int? CompanyId
        {
            get => GetValue<int?>(CustomerReferenceTable.CompanyKey);
            set => SetValue(value, CustomerReferenceTable.CompanyKey);
        }

        [Reference(nameof(CompanyId))]
        public Company Company
        {
            get => GetReference(CustomerReferenceTable.CompanyKey, ref company);
            set => SetReference(company = value, CustomerReferenceTable.CompanyKey);
        }

        [Browsable(false)]
        [Column("persone_id")]
        public int? PersoneId
        {
            get => GetValue<int?>(CustomerReferenceTable.PersoneKey);
            set => SetValue(value, CustomerReferenceTable.PersoneKey);
        }

        [Reference(nameof(PersoneId))]
        public Persone Persone
        {
            get => GetReference(CustomerReferenceTable.PersoneKey, ref persone);
            set
            {
                SetReference(persone = value, CustomerReferenceTable.PersoneKey);
                if (EMail == null)
                {
                    EMail = Persone.EMail;
                    Phone = Persone.Phone;
                }
            }
        }

        [Column("email", 1024)]
        public string EMail
        {
            get => GetValue<string>(CustomerReferenceTable.EMailKey);
            set => SetValue(value, CustomerReferenceTable.EMailKey);
        }

        [Column("phone", 1024)]
        public string Phone
        {
            get => GetValue<string>(CustomerReferenceTable.PhoneKey);
            set => SetValue(value, CustomerReferenceTable.PhoneKey);
        }

        [Column("ext_id")]
        public int? ExternalId
        {
            get => GetValue<int?>(CustomerReferenceTable.ExternalKey);
            set => SetValue(value, CustomerReferenceTable.ExternalKey);
        }
    }

    public class CustomerReferenceList : DBTableView<CustomerReference>
    {
        public CustomerReferenceList(DBTable<CustomerReference> table) : base(table)
        {
        }
    }


}

