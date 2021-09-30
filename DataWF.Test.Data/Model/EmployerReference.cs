using DataWF.Data;

namespace DataWF.Test.Data
{
    [Table(EmployerReference.TableName, "Default")]
    public sealed partial class EmployerReference
    {
        public const string TableName = "tb_employer_ref";
        private Employer employer;
        private Employer reference;

        [Column("id", Keys = DBColumnKeys.Primary)]
        public long Id
        {
            get => GetValue(Table.IdKey);
            set => SetValue(value, Table.IdKey);
        }

        [Column("employer_id")]
        public int? EmployerId
        {
            get => GetValue(Table.EmployerIdKey);
            set => SetValue(value, Table.EmployerIdKey);
        }

        [Reference(nameof(EmployerId))]
        public Employer Employer
        {
            get => GetReference(Table.EmployerIdKey, ref employer);
            set => SetReference(employer = value, Table.EmployerIdKey);
        }

        [Column("reference_id")]
        public int? ReferenceId
        {
            get => GetValue(Table.ReferenceIdKey);
            set => SetValue(value, Table.ReferenceIdKey);
        }

        [Reference(nameof(ReferenceId))]
        public Employer Reference
        {
            get => GetReference(Table.ReferenceIdKey, ref reference);
            set => SetReference(reference = value, Table.ReferenceIdKey);
        }
    }
}
