/*
 ColumnConfig.cs
 
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

namespace DataWF.Data
{
    public class IndexAttributeCache
    {
        DBIndex cacheIndex;

        public IndexAttribute Attribute { get; set; }

        public string IndexName { get { return Attribute?.IndexName; } }

        public List<ColumnAttributeCache> Columns { get; } = new List<ColumnAttributeCache>();

        public DBIndex Index
        {
            get { return cacheIndex ?? (cacheIndex = Table?.Table?.Indexes[Attribute.IndexName]); }
            set { cacheIndex = value; }
        }

        public TableAttributeCache Table { get; set; }

        public DBIndex Generate()
        {
            if (Index != null)
                return Index;
            Index = new DBIndex()
            {
                Name = Attribute.IndexName,
                Unique = Attribute.Unique,
                Table = Table.Table
            };
            foreach (var column in Columns)
            {
                Index.Columns.Add(column.Column);
            }
            Table.Table.Indexes.Add(Index);
            return Index;
        }
    }
}
