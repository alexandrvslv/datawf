using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Counterpart;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Security.Principal;

namespace DataWF.Module.Common
{
    public partial class UserGroupTable
    {
    }

    [Table("rgroup", "User", BlockSize = 10)]
    public sealed partial class UserGroup : DBGroupItem, IDisposable, IGroupIdentity
    {
        private Company company;

        //[NonSerialized()]
        //private GroupPermissionList _permission;
        //[NonSerialized()]
        //private GroupUserList _gusers;

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int Id
        {
            get => GetValue(Table.IdKey);
            set => SetValue(value, Table.IdKey);
        }

        [Column("parent_id", Keys = DBColumnKeys.Group)]
        public int? ParentId
        {
            get => GetValue(Table.ParentIdKey);
            set => SetValue(value, Table.ParentIdKey);
        }

        [Reference(nameof(ParentId))]
        public UserGroup Parent
        {
            get => GetGroupReference<UserGroup>();
            set => SetGroupReference(value);
        }

        [Column("company_id"), Browsable(false)]
        public int? CompanyId
        {
            get => GetValue(Table.CompanyIdKey);
            set => SetValue(value, Table.CompanyIdKey);
        }

        [Reference(nameof(CompanyId))]
        public Company Company
        {
            get => GetReference(Table.CompanyIdKey, ref company);
            set => SetReference(company = value, Table.CompanyIdKey);
        }

        [Column("group_number", 512, Keys = DBColumnKeys.Code), Index("rgroup_group_number")]
        public string Number
        {
            get => GetValue(Table.CodeKey);
            set => SetValue(value, Table.CodeKey);
        }

        [Column("name", Keys = DBColumnKeys.Culture | DBColumnKeys.View)]
        public string Name
        {
            get => GetName();
            set => SetName(value);
        }

        [CultureKey(nameof(Name))]
        public string NameEN
        {
            get => GetValue(Table.NameENKey);
            set => SetValue(value, Table.NameENKey);
        }

        [CultureKey(nameof(Name))]
        public string NameRU
        {
            get => GetValue(Table.NameRUKey);
            set => SetValue(value, Table.NameRUKey);
        }

        string IIdentity.AuthenticationType => NameEN;

        bool IIdentity.IsAuthenticated => true;

        bool IGroupIdentity.Required => true;

        [ControllerMethod]
        public IEnumerable<User> GetUsers(DBTransaction transaction)
        {
            foreach (User user in Schema.User)
            {
                if (user.Access.Get(this, false).Create
                    && user.Access.GetFlag(AccessType.Read, transaction.Caller))
                {
                    yield return user;
                }
            }
        }

        public bool ContainsIdentity(IUserIdentity user)
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
