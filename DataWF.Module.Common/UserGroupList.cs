using DataWF.Common;
using DataWF.Data;
using System.Collections.Generic;
using System.ComponentModel;

namespace DataWF.Module.Common
{
    public class UserGroupList : DBTableView<UserGroup>
    {
        public UserGroupList(string filter = "", DBViewKeys mode = DBViewKeys.None, DBStatus status = DBStatus.Empty)
            : base(UserGroup.DBTable, filter, mode, status)
        {
            ApplySortInternal(new DBComparer<UserGroup, int?>(UserGroup.DBTable.GroupKey, ListSortDirection.Ascending));
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
