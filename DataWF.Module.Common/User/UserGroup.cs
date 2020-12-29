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
        internal void SetCurrent()
        {
            AccessValue.Groups = new IdCollectionView<IGroupIdentity, UserGroup>(this);
        }
    }

    [Table("rgroup", "User", BlockSize = 10), InvokerGenerator]
    public sealed partial class UserGroup : DBGroupItem, IDisposable, IGroupIdentity
    {
        private Company company;

        //[NonSerialized()]
        //private GroupPermissionList _permission;
        //[NonSerialized()]
        //private GroupUserList _gusers;

        public UserGroup(DBTable table) : base(table)
        { }

        public UserGroupTable UserGroupTable => (UserGroupTable)Table;

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public int Id
        {
            get => GetValue<int>(UserGroupTable.IdKey);
            set => SetValue(value, UserGroupTable.IdKey);
        }

        [Column("parent_id", Keys = DBColumnKeys.Group)]
        public int? ParentId
        {
            get => GetValue<int?>(UserGroupTable.ParentIdKey);
            set => SetValue<int?>(value, UserGroupTable.ParentIdKey);
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
            get => GetValue<int?>(UserGroupTable.CompanyIdKey);
            set => SetValue(value, UserGroupTable.CompanyIdKey);
        }

        [Reference(nameof(CompanyId))]
        public Company Company
        {
            get => GetReference(UserGroupTable.CompanyIdKey, ref company);
            set => SetReference(company = value, UserGroupTable.CompanyIdKey);
        }

        [Column("group_number", 512, Keys = DBColumnKeys.Code), Index("rgroup_group_number")]
        public string Number
        {
            get => GetValue<string>(UserGroupTable.CodeKey);
            set => SetValue(value, UserGroupTable.CodeKey);
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
            get => GetValue<string>(UserGroupTable.NameENKey);
            set => SetValue(value, UserGroupTable.NameENKey);
        }

        [CultureKey(nameof(Name))]
        public string NameRU
        {
            get => GetValue<string>(UserGroupTable.NameRUKey);
            set => SetValue(value, UserGroupTable.NameRUKey);
        }

        string IIdentity.AuthenticationType => NameEN;

        bool IIdentity.IsAuthenticated => true;

        [ControllerMethod]
        public IEnumerable<User> GetUsers(DBTransaction transaction)
        {
            var userTable = Schema.GetTable<User>();
            foreach (User user in userTable)
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
