using DataWF.Data;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace DataWF.Module.Counterpart
{
    [Table("dcustomer_reference", "Customer")]
    public class CustomerReference : DBItem
    {
        public static readonly DBTable<CustomerReference> DBTable = GetTable<CustomerReference>();
        public static readonly DBColumn CompanyKey = DBTable.ParseProperty(nameof(CompanyId));
        public static readonly DBColumn PersoneKey = DBTable.ParseProperty(nameof(PersoneId));
        public static readonly DBColumn EMailKey = DBTable.ParseProperty(nameof(EMail));
        public static readonly DBColumn PhoneKey = DBTable.ParseProperty(nameof(Phone));
        public static readonly DBColumn ExternalIdKey = DBTable.ParseProperty(nameof(ExternalId));

        private Company company;
        private Persone persone;

        public CustomerReference()
        {
        }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(Table.PrimaryKey);
            set => SetValue(value, Table.PrimaryKey);
        }

        [Browsable(false)]
        [Column("company_id")]
        public int? CompanyId
        {
            get => GetValue<int?>(CompanyKey);
            set => SetValue(value, CompanyKey);
        }

        [Reference(nameof(CompanyId))]
        public Company Company
        {
            get => GetReference(CompanyKey, ref company);
            set => SetReference(company = value, CompanyKey);
        }

        [Browsable(false)]
        [Column("persone_id")]
        public int? PersoneId
        {
            get => GetValue<int?>(PersoneKey);
            set => SetValue(value, PersoneKey);
        }

        [Reference(nameof(PersoneId))]
        public Persone Persone
        {
            get => GetReference(PersoneKey, ref persone);
            set
            {
                SetReference(persone = value, PersoneKey);
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
            get => GetValue<string>(EMailKey);
            set => SetValue(value, EMailKey);
        }

        [Column("phone", 1024)]
        public string Phone
        {
            get => GetValue<string>(PhoneKey);
            set => SetValue(value, PhoneKey);
        }

        [Column("ext_id")]
        public int? ExternalId
        {
            get => GetValue<int?>(ExternalIdKey);
            set => SetValue(value, ExternalIdKey);
        }
    }

    public class CustomerReferenceList : DBTableView<CustomerReference>
    {
        public CustomerReferenceList() : base()
        {
        }
    }


}

