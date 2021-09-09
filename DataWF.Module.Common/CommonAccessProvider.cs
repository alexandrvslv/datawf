using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Counterpart;
using System.Collections.Generic;
using System.Linq;

namespace DataWF.Module.Common
{
    public class CommonAccessProvider : IAccessProvider
    {
        public CommonAccessProvider(ICommonSchema schema)
        {
            Schema = schema;
            UserGroupTable = Schema.UserGroup;
            UserTable = Schema.User;
            CompanyTable = Schema.Company;
        }

        public ICommonSchema Schema { get; }
        public DBTable<UserGroup> UserGroupTable { get; }
        public DBTable<User> UserTable { get; }
        public DBTable<Company> CompanyTable { get; }

        public virtual IAccessIdentity GetAccessIdentity(int identityId, IdentityType identityType)
        {
            switch (identityType)
            {
                case IdentityType.Group:
                    return UserGroupTable.GetById(identityId);
                case IdentityType.User:
                    return UserTable.GetById(identityId);
                case IdentityType.Company:
                    return CompanyTable.GetById(identityId);
                case IdentityType.Project:
                    return null;
                default:
                    return null;
            }
        }

        public virtual IEnumerable<IAccessIdentity> GetGroups()
        {
            return UserGroupTable;
        }

        public virtual IEnumerable<IAccessIdentity> GetSpecialUserGroups(IUserIdentity user)
        {
            return GetSpecialUserGroups((User)user);
        }

        public virtual IEnumerable<IAccessIdentity> GetSpecialUserGroups(User arg)
        {
            return Enumerable.Empty<IAccessIdentity>();
        }
    }

}
