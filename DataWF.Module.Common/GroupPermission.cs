/*
 GroupBase.cs
 
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
using System;
using System.ComponentModel;
using System.Linq;
using DataWF.Data;
using DataWF.Common;

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

	[Table("wf_common", "rgroup_permission", "Reference Book", BlockSize = 2000)]
	public class GroupPermission : DBItem
	{
		public static DBTable<GroupPermission> DBTable
		{
			get { return DBService.GetTable<GroupPermission>(); }
		}

		public static void BeginHandleSchema()
		{
			DBService.DBSchemaChanged += OnDBSchemaChanged;
		}

		private static void OnDBSchemaChanged(object sender, DBSchemaChangedArgs e)
		{
			try
			{
				if (e.Type == DDLType.Create)
				{
					//List<int> groups = FlowEnvir.GetGroups(FlowEnvir.Personal.User);

					if (e.Item is DBTable && e.Item.Container != null)
					{
						var sgroup = GroupPermission.Get(null, e.Item.Schema);
						var tgroup = GroupPermission.Get(sgroup, e.Item);

						foreach (DBColumn column in ((DBTable)e.Item).Columns)
						{
							GroupPermission.Get(tgroup, column);
						}
					}
					if (e.Item is DBColumn && e.Item.Container != null && ((DBColumn)e.Item).Table.Container != null)
					{
						var sgroup = GroupPermission.Get(null, e.Item.Schema);
						var tgroup = GroupPermission.Get(sgroup, ((DBColumn)e.Item).Table);
						GroupPermission.Get(tgroup, e.Item);
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
			Build(DBTable);
			Type = PermissionType.GTable;
		}

		[Column("unid", Keys = DBColumnKeys.Primary)]
		public int? Id
		{
			get { return GetValue<int?>(Table.PrimaryKey); }
			set { SetValue(value, Table.PrimaryKey); }
		}

		[Column("parent_id", Keys = DBColumnKeys.Group), Index("rgroup_permission_parent_id"), Browsable(false)]
		public int? ParentId
		{
			get { return GetValue<int?>(Table.GroupKey); }
			set { SetValue(value, Table.GroupKey); }
		}

		[Reference("fk_rgroup_permission_parent_id", nameof(ParentId))]
		public GroupPermission Parent
		{
			get { return GetReference<GroupPermission>(Table.GroupKey); }
			set { SetReference(value, Table.GroupKey); }
		}

		[Column("type_id", Keys = DBColumnKeys.ElementType)]
		public PermissionType? Type
		{
			get { return GetValue<PermissionType?>(Table.ElementTypeKey); }
			set { SetValue(value, Table.ElementTypeKey); }
		}

		[Column("code", 512, Keys = DBColumnKeys.Code | DBColumnKeys.View | DBColumnKeys.Indexing)]
		[Index("rgroup_permission_code", true)]
		public string Code
		{
			get { return GetValue<string>(Table.CodeKey); }
			set { SetValue(value, Table.CodeKey); }
		}

		public string PermissionName
		{
			get
			{
				object data = Permission;
				string per = string.Empty;
				if (data is DBColumn)
					per = $"{((DBColumn)data).Table} {data}";
				else if (data != null)
					per = data.ToString();
				return per;
			}
		}

		public object Permission
		{
			get
			{
				if (Code == null)
					return null;
				object view = GetCache(Table.CodeKey);
				if (view == null)
				{
					var type = Type;
					if (type == PermissionType.GSchema)
						view = DBService.Schems[Code];
					else if (type == PermissionType.GColumn)
						view = DBService.ParseColumn(Code);
					else if (type == PermissionType.GTable)
						view = DBService.ParseTable(Code);
					else if (type == PermissionType.GBlock)
						view = DBService.ParseTableGroup(Code);
					else if (type == PermissionType.GType)
						view = GetClass();
					else if (type == PermissionType.GTypeMember)
						view = GetClassMember();
					SetCache(Table.CodeKey, view);
				}
				return view;
			}
			set
			{
				Type = GetPermissionType(value, out string code);
				PrimaryCode = code;
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
			return DBService.ParseTable(Code);
		}

		public DBColumn GetColumn()
		{
			return DBService.ParseColumn(Code);
		}

		public override string ToString()
		{
			return PermissionName;
		}

		public static PermissionType GetPermissionType(object value, out string key)
		{
			key = null;
			PermissionType type = PermissionType.GTable;
			if (value is DBSchemaItem)
			{
				key = ((DBSchemaItem)value).FullName;
				if (value is DBSchema)
					type = PermissionType.GSchema;
				else if (value is DBTableGroup)
					type = PermissionType.GBlock;
				else if (value is DBTable)
					type = PermissionType.GTable;
				else if (value is DBColumn)
					type = PermissionType.GColumn;
			}
			else if (value is Type)
			{
				key = Helper.TextBinaryFormat(((Type)value).FullName);
				type = PermissionType.GType;
			}
			else if (value is System.Reflection.MemberInfo)
			{
				key = Helper.TextBinaryFormat(((Type)value).FullName);
				type = PermissionType.GTypeMember;
			}
			return type;
		}

		public static GroupPermission Get(GroupPermission group, DBSchemaItem item)
		{
			string code = null;
			PermissionType type = GetPermissionType(item, out code);

			var query = new QQuery(string.Empty, GroupPermission.DBTable);
			//object typeid = FlowEnvir.Config.Permission.GetTypeId(type);
			//query.BuildParam(FlowEnvir.Config.GroupPermission.Type.Column, CompareType.Equal, typeid);
			query.BuildParam(DBTable.CodeKey, CompareType.Equal, code);

			var list = DBTable.Select(query).ToList();

			var permission = list.FirstOrDefault();
			if (list.Count > 1)
			{
				DBService.Merge(list, permission);
			}

			if (permission == null)
			{
				permission = new GroupPermission()
				{
					Type = type,
					Code = code
				};
				//permission.Access.Fill();
			}
			item.Access = permission.Access;

			if (group != null)
				permission.Parent = group;
			permission.Save();

			return permission;
		}

		public static void CachePermissionTableGroup(GroupPermission parent, DBTableGroup group)
		{
			var permission = Get(parent, group);

			foreach (var subGroup in group.GetChilds())
			{
				CachePermissionTableGroup(permission, subGroup);
			}
			var tables = group.GetTables();
			foreach (DBTable table in tables)
			{
				CachePermissionTable(permission, table);
			}
		}

		public static void CachePermissionTable(GroupPermission parent, DBTable table)
		{
			if (table is DBLogTable)
				return;
			var permission = Get(parent, table);

			foreach (DBColumn column in table.Columns)
				Get(permission, column);
		}

		public static void CachePermission()
		{
			foreach (DBSchema schema in DBService.Schems)
			{
				var permission = Get(null, schema);
				var groups = schema.TableGroups.GetTopParents();

				foreach (DBTableGroup group in groups)
				{
					CachePermissionTableGroup(permission, group);
				}
				foreach (DBTable table in schema.Tables)
				{
					if (table.Group == null)
						CachePermissionTable(permission, table);
				}
			}
		}
	}
}
