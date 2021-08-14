using DataWF.Data;
using System;

namespace DataWF.Test.Data
{
    [Table(TestORM.PositionTableName, "Default")]
    public sealed partial class Position : DBItem
    {
        private Position parent;

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
            get => GetValue<string>();
            set => SetValue(value);
        }
    }

    public partial class PositionTable  
    {
        public Position GeneratePositions()
        {
            Add(new Position(this) { Code = "1", Name = "First Position" });
            Add(new Position(this) { Code = "2", Name = "Second Position" });
            var position = new Position(this) { Id = 0, Code = "3", Name = "Group Position" };
            position.Attach();
            var sposition = new Position(this) { Code = "4", Parent = position, Name = "Sub Group Position" };
            sposition.Attach();

            //Select from internal Index
            Add(new Position(this) { Code = "t1", Name = "Null Index" });
            Add(new Position(this) { Code = "t2", Name = "Null Index" });
            Add(new Position(this) { Code = "t3", Name = "Null Index" });
            return position;
        }
    }
}
