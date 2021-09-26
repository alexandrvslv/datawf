using DataWF.Common;
using DataWF.Data;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace DataWF.Module.Flow
{
    [Table("ddocument_reference", "Document", BlockSize = 400)]
    public partial class DocumentReference : DocumentItem
    {
        private Document reference;

        [Browsable(false)]
        [Column("unid", Keys = DBColumnKeys.Primary)]
        public long Id
        {
            get => GetValue(Table.IdKey);
            set => SetValue(value, Table.IdKey);
        }

        [Index("ddocument_reference_unique")]
        public override long? DocumentId
        {
            get => base.DocumentId;
            set => base.DocumentId = value;
        }

        [Browsable(false)]
        [Column("reference_id", Keys = DBColumnKeys.View)]
        [Index("ddocument_reference_unique", true)]
        public long? ReferenceId
        {
            get => GetValue(Table.ReferenceIdKey);
            set => SetValue(value, Table.ReferenceIdKey);
        }

        [Reference(nameof(ReferenceId))]
        public Document Reference
        {
            get => GetReference(Table.ReferenceIdKey, ref reference);
            set => SetReference(reference = value, Table.ReferenceIdKey);
        }

        protected override void RaisePropertyChanged(string property)
        {
            base.RaisePropertyChanged(property);

            if (Attached)
            {
                reference?.OnReferenceChanged(this);
            }
        }

    }
}
