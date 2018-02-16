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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using DataWF.Data;
using DataWF.Common;

namespace DataWF.Module.Common
{

    [Table("system", "rgroup", BlockSize = 10)]
    public class UserGroup : DBItem, IDisposable, IAccessGroup
    {
        public static DBTable<UserGroup> DBTable
        {
            get { return DBService.GetTable<UserGroup>(); }
        }
        //[NonSerialized()]
        //private GroupPermissionList _permission;
        //[NonSerialized()]
        //private GroupUserList _gusers;

        public UserGroup()
        {
            Build(DBTable);
        }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get { return GetValue<int?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }

        int IAccessGroup.Id { get { return Id.Value; } }

        [Column("groupnumber", Keys = DBColumnKeys.Code), Index("rgroup_number")]
        public string Number
        {
            get { return GetValue<string>(Table.CodeKey); }
            set { SetValue(value, Table.CodeKey); }
        }

        [Column("name", Keys = DBColumnKeys.Culture)]
        public override string Name
        {
            get { return GetName("name"); }
            set { SetName("name", value); }
        }

        public IEnumerable<User> GetUsers()
        {
            foreach (User user in User.DBTable)
                if (user.Access.GetCreate(this))
                    yield return user;
        }

        [Browsable(false)]
        public bool IsCurrent
        {
            get { return (Status == DBStatus.Actual || User.CurrentUser.Super.Value) && User.CurrentUser.Access.GetCreate(this); }
        }

        //public GroupPermissionList GroupPermissions
        //{
        //    get
        //    {
        //        if (_permission == null && Id != DBNull.Value)
        //            _permission = new GroupPermissionList(this);
        //        return _permission;
        //    }
        //}

        public override void Dispose()
        {
            base.Dispose();
            // if (_users != null)
            //     _users.Dispose();            
        }
    }
}
