using DataWF.Common;
using DataWF.Data;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace DataWF.Module.Counterpart
{
    [Table("dcustomer_reference", "Customer", Type = typeof(CustomerReferenceTable))]
    public sealed partial class CustomerReference : DBItem
    {
        private Company company;
        private Persone persone;

        public CustomerReference(DBTable table) : base(table)
        {
        }
        
        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int Id
        {
            get => GetValue(Table.IdKey);
            set => SetValue(value, Table.IdKey);
        }

        [Browsable(false)]
        [Column("company_id")]
        public int? CompanyId
        {
            get => GetValue(Table.CompanyIdKey);
            set => SetValue(value, Table.CompanyIdKey);
        }

        [Reference(nameof(CompanyId))]
        public Company Company
        {
            get => GetReference(Table.CompanyIdKey, ref company);
            set => SetReference(company = value, Table.CompanyIdKey);
        }

        [Browsable(false)]
        [Column("persone_id")]
        public int? PersoneId
        {
            get => GetValue(Table.PersoneIdKey);
            set => SetValue(value, Table.PersoneIdKey);
        }

        [Reference(nameof(PersoneId))]
        public Persone Persone
        {
            get => GetReference(Table.PersoneIdKey, ref persone);
            set
            {
                SetReference(persone = value, Table.PersoneIdKey);
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
            get => GetValue(Table.EMailKey);
            set => SetValue(value, Table.EMailKey);
        }

        [Column("phone", 1024)]
        public string Phone
        {
            get => GetValue(Table.PhoneKey);
            set => SetValue(value, Table.PhoneKey);
        }

        [Column("ext_id")]
        public int? ExternalId
        {
            get => GetValue(Table.ExternalIdKey);
            set => SetValue(value, Table.ExternalIdKey);
        }
    }

    public class CustomerReferenceList : DBTableView<CustomerReference>
    {
        public CustomerReferenceList(DBTable<CustomerReference> table) : base(table)
        {
        }
    }


}

