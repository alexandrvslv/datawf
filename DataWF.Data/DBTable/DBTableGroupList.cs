/*
 DBTableGroupList.cs
 
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
using System.Collections.Generic;
using DataWF.Common;

namespace DataWF.Data
{
    public class DBTableGroupList : DBSchemaItemList<DBTableGroup>
    {
        public DBTableGroupList(DBSchema schema) : base(schema)
        {
            Indexes.Add(DBTableGroupGroupNameInvoker.Instance);
        }

        public IEnumerable<DBTableGroup> GetTopParents()
        {
            return GetByGroup(null);
        }

        public IEnumerable<DBTableGroup> GetByGroup(string name)
        {
            return Select(nameof(DBTableGroup.GroupName), CompareType.Equal, name);
        }
    }

    [Invoker(typeof(DBTableGroup), nameof(DBTableGroup.GroupName))]
    public class DBTableGroupGroupNameInvoker : Invoker<DBTableGroup, string>
    {
        public static readonly DBTableGroupGroupNameInvoker Instance = new DBTableGroupGroupNameInvoker();
        public override string Name => nameof(DBTableGroup.GroupName);

        public override bool CanWrite => true;

        public override string GetValue(DBTableGroup target) => target.GroupName;

        public override void SetValue(DBTableGroup target, string value) => target.GroupName = value;
    }
}
