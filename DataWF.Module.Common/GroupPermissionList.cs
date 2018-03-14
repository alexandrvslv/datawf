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
using System.ComponentModel;
using System.Linq;
using DataWF.Data;
using DataWF.Common;

namespace DataWF.Module.Common
{
    public class GroupPermissionList : DBTableView<GroupPermission>
    {
        public GroupPermissionList(DBTable<GroupPermission> table, string filter, DBViewKeys mode)
            : base(table, filter, mode)
        {
            ApplySort(new InvokerComparer(typeof(GroupPermission), "ToString", ListSortDirection.Ascending));
        }

        public GroupPermissionList(string filter = "", DBViewKeys mode = DBViewKeys.None)
            : this(GroupPermission.DBTable, filter, mode)
        {
        }

        public GroupPermission Find(GroupPermission parent, object obj, bool generate)
        {
            var type = GroupPermission.GetPermissionType(obj, out string code);

            string filter = $"{ GroupPermission.DBTable.CodeKey.Name}='{code}' and {GroupPermission.DBTable.ElementTypeKey.Name}={type}";

            GroupPermission permission = table.Select(filter).FirstOrDefault();

            if (permission == null && generate)
            {
                permission = new GroupPermission();
                permission.Parent = parent;
                permission.Permission = obj;
                Add(permission);
            }
            return permission;
        }
    }
}
