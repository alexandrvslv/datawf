using DataWF.Data;
using System;
using System.ComponentModel;

namespace DataWF.Test.Data
{
    [Table(TestORM.EmployerTableName, "Default")]
    public class Employer : DBItem
    {
        private Position position;

        public static DBTable<Employer> DBTable => GetTable<Employer>();

        public Employer()
        {
        }

        [Column("id", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetProperty<int?>();
            set => SetProperty(value);
        }

        [Column("identifier", 20, Keys = DBColumnKeys.Code), Index("employeridentifier", true)]
        public string Identifier
        {
            get => GetProperty<string>();
            set => SetProperty(value);
        }

        [Column("positionid")]
        public int? PositionId
        {
            get => GetProperty<int?>();
            set => SetProperty(value);
        }

        [Reference(nameof(PositionId))]
        public Position Position
        {
            get => GetPropertyReference<Position>(ref position);
            set => SetPropertyReference(position = value);
        }

        [Column("typeid", Keys = DBColumnKeys.ElementType), DefaultValue(EmployerType.Type2)]
        public EmployerType? Type
        {
            get => GetProperty<EmployerType?>();
            set => SetProperty(value);
        }

        [Column("longid")]
        public long? LongId
        {
            get => GetProperty<long?>();
            set => SetProperty(value);
        }

        [Column("height")]
        public short? Days
        {
            get => GetProperty<short?>();
            set => SetProperty(value);
        }

        [Column("weight")]
        public float? Weight
        {
            get => GetProperty<float?>();
            set => SetProperty(value);
        }

        [Column("dweight")]
        public double? DWeight
        {
            get => GetProperty<double?>();
            set => SetProperty(value);
        }

        [Column("salary", 23, 3)]
        public decimal? Salary
        {
            get => GetProperty<decimal?>();
            set => SetProperty(value);
        }

        [Column("age")]
        public byte? Age
        {
            get => GetProperty<byte?>();
            set => SetProperty(value);
        }

        [Column("is_active")]
        public bool? IsActive
        {
            get => GetProperty<bool?>();
            set => SetProperty(value);
        }

        [Column("name", 20, Keys = DBColumnKeys.Culture)]
        public string Name
        {
            get => GetName();
            set => SetName(value);
        }
    }
}
