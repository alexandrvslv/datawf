using DataWF.Data;
using System;

namespace DataWF.Test.Data
{
    [Table(TestORM.PositionTableName, "Default")]
    public partial class Position : DBItem
    {
        private Position parent;

        public Position(DBTable table) : base(table)
        {
        }

        [Column("id", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(Table.IdKey);
            set => SetValue(value, Table.IdKey);
        }

        [Column("code", 20, Keys = DBColumnKeys.Code | DBColumnKeys.Unique | DBColumnKeys.Indexing)]
        [Index("positioncode", true)]
        public string Code
        {
            get => GetValue<string>(Table.CodeKey);
            set => SetValue(value, Table.CodeKey);
        }

        [Column("parentid", Keys = DBColumnKeys.Group)]
        public int? ParentId
        {
            get => GetValue<int?>(Table.ParentIdKey);
            set => SetValue(value, Table.ParentIdKey);
        }

        [Reference(nameof(ParentId))]
        public Position Parent
        {
            get => GetReference<Position>(Table.ParentIdKey, ref parent);
            set => parent = SetReference(value, Table.ParentIdKey);
        }

        [Column("name", 200, Keys = DBColumnKeys.Culture)]
        public string Name
        {
            get => GetName();
            set => SetName(value);
        }

        [Column("description")]
        public string Description
        {
            get => GetProperty<string>();
            set => SetProperty(value);
        }
    }
}
