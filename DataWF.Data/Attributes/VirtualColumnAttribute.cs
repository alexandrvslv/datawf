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
using System;
using System.Globalization;

namespace DataWF.Data
{
    public class VirtualColumnAttribute : ColumnAttribute
    {
        private ColumnAttribute cacheBaseColumn;

        public VirtualColumnAttribute()
        {
        }

        public VirtualColumnAttribute(string baseName)
        {
            ColumnName = BaseName = baseName;
        }

        public string BaseName { get; private set; }

        public ColumnAttribute BaseColumn
        {
            get { return cacheBaseColumn ?? (cacheBaseColumn = TableView?.BaseTable?.GetColumn(BaseName)); }
            set { cacheBaseColumn = value; }
        }

        public VirtualTableAttribute TableView
        {
            get { return (VirtualTableAttribute)Table; }
        }

        public override DBColumn CreateColumn(string name)
        {
            return new DBVirtualColumn() { Table = Table.Table, };
        }

        public override void GenerateCultureColumn(DBTable table, string groupName, CultureInfo culture)
        {
            base.GenerateCultureColumn(table, groupName, culture);

            if (TableView?.BaseTable?.Table == null)
            {
                TableView?.BaseTable?.Generate(Table.Schema);
            }

            string name = culture == null ? BaseName : $"{BaseName}_{culture.TwoLetterISOLanguageName}";
            var baseColumn = TableView?.BaseTable?.Table?.ParseColumn(name);
            if (baseColumn == null)
                throw new InvalidOperationException("BaseColumn must be availible!");

            ((DBVirtualColumn)Column).BaseColumn = baseColumn;
        }
    }
}
