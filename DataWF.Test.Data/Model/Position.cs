using DataWF.Data;
using System;

namespace DataWF.Test.Data
{
    [Table(TestORM.PositionTableName, "Default")]
    public class Position : DBItem
    {
        public static readonly DBTable<Position> DBTable = GetTable<Position>();
        public static readonly DBColumn IdKey = DBTable.ParseProperty(nameof(Id));
        public static readonly DBColumn CodeKey = DBTable.ParseProperty(nameof(Code));
        private Position parent;

        public Position()
        {
        }

        [Column("id", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValueNullable<int>(IdKey);
            set => SetValueNullable(value, IdKey);
        }

        [Column("code", 20, Keys = DBColumnKeys.Code | DBColumnKeys.Unique | DBColumnKeys.Indexing)]
        [Index("positioncode", true)]
        public string Code
        {
            get => GetValue<string>(CodeKey);
            set => SetValue(value, CodeKey);
        }

        [Column("parentid", Keys = DBColumnKeys.Group)]
        public int? ParentId
        {
            get => GetProperty<int?>();
            set => SetProperty(value);
        }

        [Reference(nameof(ParentId))]
        public Position Parent
        {
            get => GetPropertyReference<Position>(ref parent);
            set => parent = SetPropertyReference(value);
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
