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
using DataWF.Module.Counterpart;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace DataWF.Module.Common
{
    [DataContract, Table("rgroup", "User", BlockSize = 10)]
    public class UserGroup : DBGroupItem, IDisposable, IAccessGroup
    {
        public static readonly DBTable<UserGroup> DBTable = GetTable<UserGroup>();
        public static readonly DBColumn NameENKey = DBTable.ParseProperty(nameof(NameEN));
        public static readonly DBColumn NameRUKey = DBTable.ParseProperty(nameof(NameRU));
        public static readonly DBColumn CompanyKey = DBTable.ParseProperty(nameof(Company));

        private Company company;

        internal static void SetCurrent()
        {
            AccessValue.Groups = UserGroup.DBTable;
        }

        //[NonSerialized()]
        //private GroupPermissionList _permission;
        //[NonSerialized()]
        //private GroupUserList _gusers;

        public UserGroup()
        { }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int? Id
        {
            get => GetValue<int?>(Table.PrimaryKey);
            set => SetValue(value, Table.PrimaryKey);
        }

        [Column("parent_id", Keys = DBColumnKeys.Group)]
        public int? ParentId
        {
            get => GetValue<int?>(Table.GroupKey);
            set => SetValue<int?>(value, Table.GroupKey);
        }

        [Reference(nameof(ParentId))]
        public UserGroup Parent
        {
            get => GetGroupReference<UserGroup>();
            set => SetGroupReference(value);
        }

        int IAccessGroup.Id { get { return Id ?? -1; } }

        [Column("company_id"), Browsable(false)]
        public int? CompanyId
        {
            get => GetValue<int?>(CompanyKey);
            set => SetValue(value, CompanyKey);
        }

        [Reference(nameof(CompanyId))]
        public Company Company
        {
            get => GetReference(CompanyKey, ref company);
            set => SetReference(company = value, CompanyKey);
        }

        [Column("group_number", 512, Keys = DBColumnKeys.Code), Index("rgroup_group_number")]
        public string Number
        {
            get => GetValue<string>(Table.CodeKey);
            set => SetValue(value, Table.CodeKey);
        }

        [Column("name", Keys = DBColumnKeys.Culture | DBColumnKeys.View)]
        public string Name
        {
            get => GetName();
            set => SetName(value);
        }

        public string NameEN
        {
            get => GetValue<string>(NameENKey);
            set => SetValue(value, NameENKey);
        }

        public string NameRU
        {
            get => GetValue<string>(NameRUKey);
            set => SetValue(value, NameRUKey);
        }

        [ControllerMethod]
        public IEnumerable<User> GetUsers(DBTransaction transaction)
        {
            foreach (User user in User.DBTable)
            {
                if (user.Access.Get(this, false).Create
                    && user.Access.GetFlag(AccessType.Read, transaction.Caller))
                {
                    yield return user;
                }
            }
        }

        public bool ContainsUser(IUserIdentity user)
        {
            return user is User currentUser
                // && currentUser.Status == DBStatus.Actual
                && currentUser.Access.Get(this).Create;
        }

        public void AddUser(User user)
        {
            user.Access.Add(this, AccessType.Create | AccessType.Read);
        }

        public override void Dispose()
        {
            base.Dispose();
        }


    }
}
