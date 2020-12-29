using DataWF.Common;
using DataWF.Data;
using System.Collections.Generic;
using System.ComponentModel;

namespace DataWF.Module.Common
{
    public class UserGroupList : DBTableView<UserGroup>
    {
        public UserGroupList(UserGroupTable table,string filter = "", DBViewKeys mode = DBViewKeys.None, DBStatus status = DBStatus.Empty)
            : base(table, filter, mode, status)
        {
            ApplySortInternal(new DBComparer<UserGroup, int?>(table.ParentIdKey, ListSortDirection.Ascending));
        }

        public UserGroup GetCurrent(IUserIdentity user)
        {
            foreach (var item in this)
            {
                if (item.ContainsIdentity(user))
                    return item;
            }

            return null;
        }
    }
}
