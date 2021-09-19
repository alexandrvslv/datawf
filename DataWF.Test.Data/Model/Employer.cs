using DataWF.Data;
using System;
using System.ComponentModel;

namespace DataWF.Test.Data
{
    [Table(Employer.TableName, "Default")]
    public partial class Employer : DBItem
    {
        public const string TableName = "tb_employer";

        private Position position;

        [Column("id", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>();
            set => SetValue(value);
        }

        [Column("identifier", 20, Keys = DBColumnKeys.Code), Index("employeridentifier", true)]
        public string Identifier
        {
            get => GetValue<string>();
            set => SetValue(value);
        }

        [Column("positionid")]
        public int? PositionId
        {
            get => GetValue<int?>();
            set => SetValue(value);
        }

        [Reference(nameof(PositionId))]
        public Position Position
        {
            get => GetReference<Position>(ref position);
            set => SetReference(position = value);
        }

        [Column("typeid", Keys = DBColumnKeys.ElementType), DefaultValue(EmployerType.Type2)]
        public EmployerType? Type
        {
            get => GetValue<EmployerType?>();
            set => SetValue(value);
        }

        [Column("longid")]
        public long? LongId
        {
            get => GetValue<long?>();
            set => SetValue(value);
        }

        [Column("salary", 23, 3)]
        public decimal? Salary
        {
            get => GetValue<decimal?>();
            set => SetValue(value);
        }

        [Column("age")]
        public byte? Age
        {
            get => GetValue<byte?>();
            set => SetValue(value);
        }

        [Column("is_active")]
        public bool? IsActive
        {
            get => GetValue<bool?>();
            set => SetValue(value);
        }

        [Column("name", 20, Keys = DBColumnKeys.Culture)]
        public string Name
        {
            get => GetName();
            set => SetName(value);
        }
    }
}
