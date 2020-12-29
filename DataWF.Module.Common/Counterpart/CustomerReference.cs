using DataWF.Common;
using DataWF.Data;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace DataWF.Module.Counterpart
{
    [Table("dcustomer_reference", "Customer", Type = typeof(CustomerReferenceTable)), InvokerGenerator]
    public sealed partial class CustomerReference : DBItem
    {
        private Company company;
        private Persone persone;

        public CustomerReference(DBTable table) : base(table)
        {
        }
        public ICustomerReferenceTable CustomerReferenceTable => (ICustomerReferenceTable)Table;

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
            get => GetValue<int?>(CustomerReferenceTable.CompanyIdKey);
            set => SetValue(value, CustomerReferenceTable.CompanyIdKey);
        }

        [Reference(nameof(CompanyId))]
        public Company Company
        {
            get => GetReference(CustomerReferenceTable.CompanyIdKey, ref company);
            set => SetReference(company = value, CustomerReferenceTable.CompanyIdKey);
        }

        [Browsable(false)]
        [Column("persone_id")]
        public int? PersoneId
        {
            get => GetValue<int?>(CustomerReferenceTable.PersoneIdKey);
            set => SetValue(value, CustomerReferenceTable.PersoneIdKey);
        }

        [Reference(nameof(PersoneId))]
        public Persone Persone
        {
            get => GetReference(CustomerReferenceTable.PersoneIdKey, ref persone);
            set
            {
                SetReference(persone = value, CustomerReferenceTable.PersoneIdKey);
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
            get => GetValue<int?>(CustomerReferenceTable.ExternalIdKey);
            set => SetValue(value, CustomerReferenceTable.ExternalIdKey);
        }
    }

    public class CustomerReferenceList : DBTableView<CustomerReference>
    {
        public CustomerReferenceList(DBTable<CustomerReference> table) : base(table)
        {
        }
    }


}

