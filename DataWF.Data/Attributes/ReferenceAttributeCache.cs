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
using System.Reflection;

namespace DataWF.Data
{
    public class ReferenceAttributeCache
    {
        private ColumnAttributeCache cacheColumn;
        private DBForeignKey cacheKey;

        public ReferenceAttributeCache(TableAttributeCache table, PropertyInfo property, ReferenceAttribute referenceAttribute)
        {
            Attribute = referenceAttribute;
            Table = table;
            Property = property;
            ReferenceType = property.PropertyType;
            Column.DisplayName = property.Name;
            Column.Attribute.Keys |= DBColumnKeys.Reference;
            Column.ReferenceProperty = property;
            GenerateName();
        }

        public ReferenceAttribute Attribute { get; set; }

        public Type ReferenceType { get; internal set; }

        public TableAttributeCache Table { get; internal set; }

        public ColumnAttributeCache Column
        {
            get { return cacheColumn ?? (cacheColumn = Table?.GetColumnByProperty(Attribute.ColumnProperty)); }
        }

        public DBForeignKey ForeignKey
        {
            get { return cacheKey ?? (cacheKey = Table?.Table?.Foreigns[Attribute.Name]); }
            internal set { cacheKey = value; }
        }

        public PropertyInfo Property { get; set; }

        public string PropertyName { get { return Property?.Name; } }

        public void GenerateName()
        {
            if (string.IsNullOrEmpty(Attribute.Name) && Table != null && Column != null)
            {
                Attribute.Name = $"fk_{Table.Attribute.TableName}_{Column.ColumnName}";
            }
        }

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

                var referenceTable = DBTable.GetTable(ReferenceType, Table.Schema, true);
                if (referenceTable == null)
                {
                    throw new Exception($"{nameof(ReferenceType)}({Attribute.ColumnProperty} - {ReferenceType}) Table not found! Target table: {Table.Table}");
                }
                if (referenceTable.PrimaryKey == null)
                {
                    throw new Exception($"{nameof(ReferenceType)}({Attribute.ColumnProperty} - {ReferenceType}) Primary key not found! Target table: {Table.Table}");
                }
                ForeignKey = new DBForeignKey()
                {
                    Table = Table.Table,
                    Column = Column.Column,
                    Reference = referenceTable.PrimaryKey,
                    Name = Attribute.Name
                };
                Table.Table.Foreigns.Add(ForeignKey);
            }
            ForeignKey.Property = Property.Name;

            return ForeignKey;
        }
    }
}