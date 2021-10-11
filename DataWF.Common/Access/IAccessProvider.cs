using System.Collections.Generic;
using System.Linq;

namespace DataWF.Common
{
    public interface IAccessProvider
    {
        IAccessIdentity GetAccessIdentity(int id, IdentityType type);
        IEnumerable<IAccessIdentity> GetGroups();
        IEnumerable<IAccessIdentity> GetSpecialUserGroups(IUserIdentity user);
    }

    public class AccessProviderStub : IAccessProvider
    {
        public IdCollection<IGroupIdentity> Groups { get; set; }

        public IAccessIdentity GetAccessIdentity(int id, IdentityType type)
        {
            return Groups.GetById(id);
        }

        public IEnumerable<IAccessIdentity> GetGroups()
        {
            return Groups;
        }

        public IEnumerable<IAccessIdentity> GetSpecialUserGroups(IUserIdentity user)
        {
            return Enumerable.Empty<IAccessIdentity>();
        }
    }
}
