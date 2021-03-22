using DataWF.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataWF.Test.Data
{
    public class AccessGroupBung : IGroupIdentity, IPrimaryKey
    {
        public int? Id { get; set; }

        public string Name { get; set; }
        public bool Expand { get; set; }
        public IGroup Group { get; set; }

        public bool IsCompaund => false;

        public bool IsExpanded => true;

        public string AuthenticationType => Name;

        public bool IsAuthenticated => true;

        public object PrimaryKey { get => Id; set => Id = (int)value; }

        public bool Required => true;

        public int CompareTo(object obj)
        {
            throw new NotImplementedException();
        }

        public bool ContainsIdentity(IUserIdentity user)
        {
            return true;
        }

        public IEnumerable<IGroup> GetGroups()
        {
            return Enumerable.Empty<IGroup>();
        }
    }
}
