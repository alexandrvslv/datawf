/*
 Group.cs
 
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
using System.Collections.Generic;
using System.ComponentModel;

namespace DataWF.Module.Common
{
    public class UserGroupList : DBTableView<UserGroup>
    {
        public UserGroupList(string filter = "", DBViewKeys mode = DBViewKeys.None, DBStatus status = DBStatus.Empty)
            : base(UserGroup.DBTable, filter, mode, status)
        {
            ApplySort((IComparer<UserGroup>)new DBComparer<UserGroup>(UserGroup.DBTable.GroupKey, ListSortDirection.Ascending));
        }

        public UserGroup GetCurrent(IUserIdentity user)
        {
            foreach (var item in this)
            {
                if (item.ContainsUser(user))
                    return item;
            }

            return null;
        }
    }
}
