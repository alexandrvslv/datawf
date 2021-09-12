using DataWF.Common;
using DataWF.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace DataWF.Module.Common
{

    [Table("rgroup_permission", "User", BlockSize = 500), InvokerGenerator]
    public sealed partial class GroupPermission : DBGroupItem
    {
        private object target;

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int Id
        {
            get => GetValue(Table.IdKey);
            set => SetValue(value, Table.IdKey);
        }

        [Column("parent_id", Keys = DBColumnKeys.Group), Index("rgroup_permission_parent_id"), Browsable(false)]
        public int? ParentId
        {
            get => GetGroupValue<int?>();
            set => SetGroupValue(value);
        }

        [Reference(nameof(ParentId))]
        public GroupPermission Parent
        {
            get => GetGroupReference<GroupPermission>();
            set => SetGroupReference(value);
        }

        [DefaultValue(PermissionType.GTable), Column("type_id", Keys = DBColumnKeys.ElementType)]
        public PermissionType? Type
        {
            get => GetValue(Table.TypeKey);
            set => SetValue(value, Table.TypeKey);
        }

        [Column("code", 512, Keys = DBColumnKeys.Code | DBColumnKeys.View | DBColumnKeys.Indexing)]
        [Index("rgroup_permission_code", true)]
        public string Code
        {
            get => GetValue(Table.CodeKey);
            set => SetValue(value, Table.CodeKey);
        }

        [Column("object_name", 1024, Keys = DBColumnKeys.Indexing)]
        public string ObjectName
        {
            get => GetValue(Table.ObjectNameKey);
            set => SetValue(value, Table.ObjectNameKey);
        }

        public object Target
        {
            get
            {
                if (Code == null)
                    return null;
                if (target == null)
                {
                    var type = Type;
                    if (type == PermissionType.GSchema)
                        target = Schema.Schems[Code];
                    else if (type == PermissionType.GColumn)
                        target = Schema.Schems.ParseColumn(Code);
                    else if (type == PermissionType.GTable)
                        target = Schema.Schems.ParseTable(Code);
                    else if (type == PermissionType.GBlock)
                        target = Schema.Schems.ParseTableGroup(Code);
                    else if (type == PermissionType.GType)
                        target = GetClass();
                    else if (type == PermissionType.GTypeMember)
                        target = GetClassMember();
                }
                return target;
            }
            set
            {
                Type = Table.GetPermission(value, out var code, out var name);
                PrimaryCode = code;
                ObjectName = name;
            }
        }

        [Column(nameof(AccessItems), ColumnType = DBColumnTypes.Code)]
        public IEnumerable<AccessItem> AccessItems
        {
            get => Access.Items;
            set => Access = new AccessValue(value);
        }

        public override AccessValue Access
        {
            get => base.Access != Table.Access ? base.Access
                  : Parent?.Access ?? Table.Access;
            set
            {
                base.Access = value;
                if (Target is IAccessable accessable)
                {
                    accessable.Access = value;
                }
            }
        }

        private object GetClassMember()
        {
            throw new NotImplementedException();
        }

        private object GetClass() => System.Type.GetType(Code);

        public DBSchema GetSchema() => Schema?.Provider?.Schems[Code];

        public DBTable GetTable() => Schema?.Schems?.ParseTable(Code);

        public DBColumn GetColumn() => Schema?.Schems?.ParseColumn(Code);

        public override string ToString()
        {
            return ObjectName;
        }

        protected override void RaisePropertyChanged(string property)
        {
            base.RaisePropertyChanged(property);
        }
    }
}
