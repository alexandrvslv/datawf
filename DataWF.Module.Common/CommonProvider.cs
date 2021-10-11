using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Counterpart;
using System.Collections.Generic;
using System.Linq;

namespace DataWF.Module.Common
{
    [Provider]
    public partial class CommonProvider : DBProvider
    {
        public ICommonSchema Schema => Schems.OfType<ICommonSchema>().FirstOrDefault();

        public override IUserIdentity GetUser(int id) => Schema.User.LoadById<int>(id);

        public override IUserIdentity GetUser(string id) => Schema.User.GetByEmail(id);

        public override IUserGroupIdentity GetUserGroup(int id) => Schema.UserGroup.LoadById<int>(id);

        public override ICompanyIdentity GetCompany(int id) => Schema.Company.LoadById<int>(id);

        public override IEnumerable<IAccessIdentity> GetGroups() => Schema.UserGroup;

        public override IEnumerable<IAccessIdentity> GetSpecialUserGroups(IUserIdentity user)
        {
            return Enumerable.Empty<IAccessIdentity>();
        }

    }

}
