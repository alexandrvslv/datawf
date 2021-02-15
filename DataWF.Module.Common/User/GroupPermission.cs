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
        public GroupPermission(DBTable table) : base(table)
        {
        }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(GroupPermissionTable.IdKey);
            set => SetValue(value, GroupPermissionTable.IdKey);
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
            get => GetValue<PermissionType?>(GroupPermissionTable.TypeKey);
            set => SetValue(value, GroupPermissionTable.TypeKey);
        }

        [Column("code", 512, Keys = DBColumnKeys.Code | DBColumnKeys.View | DBColumnKeys.Indexing)]
        [Index("rgroup_permission_code", true)]
        public string Code
        {
            get => GetValue<string>(GroupPermissionTable.CodeKey);
            set => SetValue(value, GroupPermissionTable.CodeKey);
        }

        [Column("object_name", 1024, Keys = DBColumnKeys.Indexing)]
        public string ObjectName
        {
            get => GetValue<string>(GroupPermissionTable.ObjectNameKey);
            set => SetValue(value, GroupPermissionTable.ObjectNameKey);
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
                        target = DBService.Schems[Code];
                    else if (type == PermissionType.GColumn)
                        target = DBService.Schems.ParseColumn(Code);
                    else if (type == PermissionType.GTable)
                        target = DBService.Schems.ParseTable(Code);
                    else if (type == PermissionType.GBlock)
                        target = DBService.Schems.ParseTableGroup(Code);
                    else if (type == PermissionType.GType)
                        target = GetClass();
                    else if (type == PermissionType.GTypeMember)
                        target = GetClassMember();
                }
                return target;
            }
            set
            {
                Type = GroupPermissionTable.GetPermissionType(value, out var code, out var name);
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

        private object GetClass()
        {
            return System.Type.GetType(Code);
        }

        public DBSchema GetSchema()
        {
            return DBService.Schems[Code];
        }

        public DBTable GetTable()
        {
            return DBService.Schems.ParseTable(Code);
        }

        public DBColumn GetColumn()
        {
            return DBService.Schems.ParseColumn(Code);
        }

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
