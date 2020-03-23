/*
 GroupPermission.cs
 
 Author:
      Alexandr <alexandr_vslv@mail.ru>

 This program is free software: you can redistribute it and/or modify
 it under the terms of the GNU Lesser General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 This program is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 GNU Lesser General Public License for more details.

 You should have received a copy of the GNU Lesser General Public License
 along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
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
    public enum PermissionType
    {
        GSchema,
        GBlock,
        GTable,
        GColumn,
        GType,
        GTypeMember
    }

    [Table("rgroup_permission", "User", BlockSize = 500)]
    public class GroupPermission : DBGroupItem
    {
        private object target;
        public static readonly DBTable<GroupPermission> DBTable = GetTable<GroupPermission>();
        public static readonly DBColumn ObjectNameKey = DBTable.ParseProperty(nameof(ObjectName));
        public static readonly DBColumn TypeKey = DBTable.ParseProperty(nameof(Type));

        public static PermissionType GetPermissionType(object value, out string key, out string name)
        {
            key = null;
            name = string.Empty;
            PermissionType type = PermissionType.GTable;
            if (value is DBSchemaItem)
            {
                key = ((DBSchemaItem)value).FullName;
                if (value is DBSchema)
                {
                    type = PermissionType.GSchema;
                    name = value.ToString();
                }
                else if (value is DBTableGroup)
                {
                    type = PermissionType.GBlock;
                    name = value.ToString();
                }
                else if (value is DBTable table)
                {
                    type = PermissionType.GTable;
                    name = table.ItemType?.Type.Name ?? value.ToString();
                }
                else if (value is DBColumn column)
                {
                    type = PermissionType.GColumn;
                    name = column.Property ?? value.ToString();
                }
            }
            else if (value is Type valueType)
            {
                key = Helper.TextBinaryFormat(value);
                type = PermissionType.GType;
                name = valueType.Name;
            }
            else if (value is System.Reflection.MemberInfo member)
            {
                key = Helper.TextBinaryFormat(value);
                type = PermissionType.GTypeMember;
                name = member.Name;
            }
            return type;
        }

        public static async Task<GroupPermission> Get(GroupPermission group, DBSchemaItem item)
        {
            PermissionType type = GetPermissionType(item, out string code, out string name);

            var list = DBTable.Select(DBTable.CodeKey, CompareType.Equal, code).ToList();

            var permission = list.FirstOrDefault();
            if (list.Count > 1)
            {
                await permission.Merge(list);
            }

            if (permission == null)
            {
                permission = new GroupPermission()
                {
                    Type = type,
                    Code = code
                };
                permission.Attach();
            }
            if (string.IsNullOrEmpty(permission.ObjectName))
            {
                permission.ObjectName = name;
            }
            item.Access = permission.Access;

            if (group != null)
            {
                permission.Parent = group;
            }

            return permission;
        }

        public static async Task CachePermissionTableGroup(GroupPermission parent, DBTableGroup group)
        {
            var permission = await Get(parent, group);

            foreach (var subGroup in group.GetChilds())
            {
                await CachePermissionTableGroup(permission, subGroup);
            }
            var tables = group.GetTables();
            foreach (DBTable table in tables)
            {
                await CachePermissionTable(permission, table);
            }
        }

        public static async Task CachePermissionTable(GroupPermission parent, DBTable table)
        {
            if (table is IDBLogTable)
                return;
            var permission = await Get(parent, table);

            foreach (DBColumn column in table.Columns)
                await Get(permission, column);
        }

        public static async Task CachePermission()
        {
            using (var transaction = new DBTransaction(DBTable.Connection))
            {
                try
                {
                    await CachePermission(transaction);
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    Helper.OnException(ex);
                    transaction.Rollback();
                    throw ex;
                }
            }
        }

        public static async Task CachePermission(DBTransaction transaction)
        {
            if (AccessValue.Groups == null || AccessValue.Groups.Count() == 0)
                return;

            foreach (DBSchema schema in DBService.Schems)
            {
                var permission = await Get(null, schema);
                var groups = schema.TableGroups.GetTopParents();

                foreach (DBTableGroup group in groups)
                {
                    await CachePermissionTableGroup(permission, group);
                }
                foreach (DBTable table in schema.Tables)
                {
                    if (table.Group == null)
                        await CachePermissionTable(permission, table);
                }
            }
            await DBTable.Save(transaction);
        }

        public static GroupPermission Find(GroupPermission parent, object obj, bool generate)
        {
            var type = GetPermissionType(obj, out string code, out string name);

            string filter = $"{ DBTable.CodeKey.Name}='{code}' and {DBTable.ElementTypeKey.Name}={type}";

            GroupPermission permission = DBTable.Select(filter).FirstOrDefault();

            if (permission == null && generate)
            {
                permission = new GroupPermission()
                {
                    Parent = parent,
                    Target = obj
                };
                permission.Attach();
            }
            permission.ObjectName = name;
            return permission;
        }

        [ControllerMethod]
        public static GroupPermission GetByName(string name)
        {
            return GetByName(name, PermissionType.GTable);
        }

        public static GroupPermission GetByName(string name, PermissionType type)
        {
            using (var query = new QQuery(DBTable))
            {
                query.BuildParam(ObjectNameKey, name);
                query.BuildParam(TypeKey, type);
                return DBTable.Select(query).FirstOrDefault();
            }
        }

        [ControllerMethod]
        public static IEnumerable<GroupPermission> GetGroupByName(string name)
        {
            return GetGroupByName(name, PermissionType.GTable);
        }
        public static IEnumerable<GroupPermission> GetGroupByName(string name, PermissionType type)
        {
            var item = GetByName(name, type);
            return item?.GetSubGroups<GroupPermission>(DBLoadParam.None) ?? Enumerable.Empty<GroupPermission>();
        }

        public static void BeginHandleSchema()
        {
            DBService.DBSchemaChanged += OnDBSchemaChanged;
        }

        private static async void OnDBSchemaChanged(object sender, DBSchemaChangedArgs e)
        {
            try
            {
                if (e.Type == DDLType.Create)
                {
                    //List<int> groups = FlowEnvir.GetGroups(FlowEnvir.Personal.User);

                    if (e.Item is DBTable && e.Item.Containers.Any())
                    {
                        var sgroup = await Get(null, e.Item.Schema);
                        var tgroup = await Get(sgroup, e.Item);

                        foreach (DBColumn column in ((DBTable)e.Item).Columns)
                        {
                            await Get(tgroup, column);
                        }
                    }
                    if (e.Item is DBColumn && e.Item.Containers.Any() && ((DBColumn)e.Item).Table.Containers.Any())
                    {
                        var sgroup = await Get(null, e.Item.Schema);
                        var tgroup = await Get(sgroup, ((DBColumn)e.Item).Table);
                        await Get(tgroup, e.Item);
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.OnException(ex);
            }
        }

        public GroupPermission()
        {
        }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(Table.PrimaryKey);
            set => SetValue(value, Table.PrimaryKey);
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
            get => GetValue<PermissionType?>(Table.ElementTypeKey);
            set => SetValue(value, Table.ElementTypeKey);
        }

        [Column("code", 512, Keys = DBColumnKeys.Code | DBColumnKeys.View | DBColumnKeys.Indexing)]
        [Index("rgroup_permission_code", true)]
        public string Code
        {
            get => GetValue<string>(Table.CodeKey);
            set => SetValue(value, Table.CodeKey);
        }

        [Column("object_name", 1024, Keys = DBColumnKeys.Indexing)]
        public string ObjectName
        {
            get => GetValue<string>(ObjectNameKey);
            set => SetValue(value, ObjectNameKey);
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
                Type = GetPermissionType(value, out var code, out var name);
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
