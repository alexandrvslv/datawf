/*
 Account.cs
 
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
using System.Xml.Serialization;

namespace DataWF.Data
{

    public class ReferenceAttribute : Attribute
    {
        private ColumnAttribute cacheColumn;
        private DBForeignKey cacheKey;
        private string name;

        public ReferenceAttribute(string property, string name = null)
        {
            ColumnProperty = property;
            this.name = name;
        }

        public string Name
        {
            get
            {
                if (string.IsNullOrEmpty(name) && Table != null && Column != null)
                {
                    name = $"fk_{Table.TableName}_{Column.ColumnName}";
                }
                return name;
            }
            set { name = value; }
        }


        public string ColumnProperty { get; set; }

        [XmlIgnore]
        public Type ReferenceType { get; internal set; }

        [XmlIgnore]
        public TableAttribute Table { get; internal set; }

        public ColumnAttribute Column
        {
            get { return cacheColumn ?? (cacheColumn = Table?.GetColumnByProperty(ColumnProperty)); }
        }

        public DBForeignKey ForeignKey
        {
            get { return cacheKey ?? (cacheKey = Table?.Table?.Foreigns[Name]); }
            internal set { cacheKey = value; }
        }

        public string Property { get; set; }

        public DBForeignKey Generate()
        {
            if (ForeignKey != null)
                return ForeignKey;
            if (ReferenceType == null
                || Table == null || Table.Schema == null
                || Column == null || Column.Column == null)
            {
                throw new Exception($"{nameof(ReferenceAttribute)} is not initialized!");
            }
            if (ForeignKey == null)
            {
                Column.Column.IsReference = true;
                var referenceTable = DBService.GetTable(ReferenceType, Table.Schema, true, true);
                if (referenceTable == null || referenceTable.PrimaryKey == null)
                {
                    throw new Exception($"{nameof(ReferenceType)} table not found!");
                }
                ForeignKey = new DBForeignKey()
                {
                    Table = Table.Table,
                    Column = Column.Column,
                    Reference = referenceTable.PrimaryKey,
                    Name = Name
                };
                Table.Table.Foreigns.Add(ForeignKey);
            }
            ForeignKey.Property = Property;

            return ForeignKey;
        }
    }
}