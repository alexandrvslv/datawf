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
using System.Runtime.Serialization;

namespace DataWF.Module.Common
{

    [DataContract, Table("rgroup", "User", BlockSize = 10)]
    public class UserGroup : DBItem, IDisposable, IAccessGroup
    {
        internal static void SetCurrent()
        {
            AccessValue.Groups = UserGroup.DBTable;
        }

        public static DBTable<UserGroup> DBTable
        {
            get { return GetTable<UserGroup>(); }
        }

        public void AddUser(User user)
        {
            user.Access.SetFlag(this, AccessType.Create);
        }

        //[NonSerialized()]
        //private GroupPermissionList _permission;
        //[NonSerialized()]
        //private GroupUserList _gusers;

        public UserGroup()
        {
            Build(DBTable);
        }

        [DataMember, Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get { return GetValue<int?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }

        int IAccessGroup.Id { get { return Id ?? -1; } }

        [DataMember, Column("group_number", 512, Keys = DBColumnKeys.Code), Index("rgroup_group_number")]
        public string Number
        {
            get { return GetValue<string>(Table.CodeKey); }
            set { SetValue(value, Table.CodeKey); }
        }

        [DataMember, Column("name", Keys = DBColumnKeys.Culture | DBColumnKeys.View)]
        public override string Name
        {
            get { return GetName(nameof(Name)); }
            set { SetName(nameof(Name), value); }
        }

        [ControllerMethod]
        public IEnumerable<User> GetUsers()
        {
            foreach (User user in User.DBTable)
                if (user.Access.Get(this).Create)
                    yield return user;
        }

        [Browsable(false)]
        public bool IsCurrent
        {
            get
            {
                return ((User.CurrentUser?.Super.Value ?? false))//Status == DBStatus.Actual ||
                    && (User.CurrentUser?.Access.Get(this).Create ?? false);
            }
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
