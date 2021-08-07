using DataWF.Common;
using DataWF.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataWF.Module.Common
{
    public partial class GroupPermissionTable
    {
        public PermissionType GetPermission(object value, out string key, out string name)
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
                    name = column.PropertyName ?? value.ToString();
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

        public async Task<GroupPermission> Get(GroupPermission group, IDBSchemaItem item)
        {
            PermissionType type = GetPermission(item, out string code, out string name);

            var list = Select(CodeKey, CompareType.Equal, code).ToList();

            var permission = list.FirstOrDefault();
            if (list.Count > 1)
            {
                await permission.Merge(list);
            }

            if (permission == null)
            {
                permission = new GroupPermission(this)
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

        public async Task CachePermission(GroupPermission parent, DBTableGroup group)
        {
            var permission = await Get(parent, group);

            foreach (var subGroup in group.GetChilds())
            {
                await CachePermission(permission, subGroup);
            }
            var tables = group.GetTables();
            foreach (DBTable table in tables)
            {
                await CachePermission(permission, table);
            }
        }

        public async Task CachePermission(GroupPermission parent, DBTable table)
        {
            if (table is IDBLogTable)
                return;
            var permission = await Get(parent, table);

            foreach (DBColumn column in table.Columns)
                await Get(permission, column);
        }

        public async Task CachePermission()
        {
            using (var transaction = new DBTransaction(this))
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

        public async Task CachePermission(DBTransaction transaction)
        {
            if (AccessValue.Provider == null || !AccessValue.Provider.GetGroups().Any())
                return;

            foreach (DBSchema schema in DBService.Schems)
            {
                var permission = await Get(null, schema);
                var groups = schema.TableGroups.GetTopParents();

                foreach (DBTableGroup group in groups)
                {
                    await CachePermission(permission, group);
                }
                foreach (DBTable table in schema.Tables)
                {
                    if (table.Group == null)
                        await CachePermission(permission, table);
                }
            }
            await Save(transaction);
        }

        public GroupPermission Find(GroupPermission parent, object obj, bool generate)
        {
            var type = GetPermission(obj, out string code, out string name);

            string filter = $"{ CodeKey.SqlName}='{code}' and {ElementTypeKey.SqlName}={type}";

            GroupPermission permission = Select(filter).FirstOrDefault();

            if (permission == null && generate)
            {
                permission = new GroupPermission(this)
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
        public GroupPermission GetByName(string name)
        {
            return GetByName(name, PermissionType.GTable);
        }

        public GroupPermission GetByName(string name, PermissionType type)
        {
            using (var query = new QQuery(this))
            {
                query.BuildParam(ObjectNameKey, name);
                query.BuildParam(TypeKey, type);
                return Select(query).FirstOrDefault();
            }
        }

        [ControllerMethod]
        public IEnumerable<GroupPermission> GetGroupByName(string name)
        {
            return GetGroupByName(name, PermissionType.GTable);
        }
        public IEnumerable<GroupPermission> GetGroupByName(string name, PermissionType type)
        {
            var item = GetByName(name, type);
            return item?.GetSubGroups<GroupPermission>(DBLoadParam.None) ?? Enumerable.Empty<GroupPermission>();
        }

        public void BeginHandleSchema()
        {
            DBService.DBSchemaChanged += OnDBSchemaChanged;
        }

        private async void OnDBSchemaChanged(object sender, DBSchemaChangedArgs e)
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


    }
}
