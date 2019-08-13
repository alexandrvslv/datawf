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
using DataWF.Common;
using System;
using System.Reflection;

namespace DataWF.Data
{
    public class ReferenceGenerator
    {
        private ColumnGenerator cacheColumn;
        private DBForeignKey cacheKey;

        public ReferenceGenerator(TableGenerator table, PropertyInfo property, ReferenceAttribute referenceAttribute)
        {
            Attribute = referenceAttribute;
            Table = table;
            PropertyInfo = property;
            ReferenceType = property.PropertyType;
            Column.DisplayName = property.Name;
            Column.Attribute.Keys |= DBColumnKeys.Reference;
            Column.ReferencePropertyInfo = property;
            GenerateName();
        }

        public ReferenceAttribute Attribute { get; set; }

        public Type ReferenceType { get; internal set; }

        public TableGenerator Table { get; internal set; }

        public ColumnGenerator Column
        {
            get { return cacheColumn ?? (cacheColumn = Table?.GetColumnByProperty(Attribute.ColumnProperty)); }
        }

        public DBForeignKey ForeignKey
        {
            get { return cacheKey ?? (cacheKey = Table?.Table?.Foreigns[Attribute.Name]); }
            internal set { cacheKey = value; }
        }

        public PropertyInfo PropertyInfo { get; set; }

        public string PropertyName { get { return PropertyInfo?.Name; } }


        public void GenerateName()
        {
            if (string.IsNullOrEmpty(Attribute.Name) && Table != null && Column != null)
            {
                Attribute.Name = $"fk_{Table.Attribute.TableName}_{Column.ColumnName}";
            }
        }

        public DBTable CheckReference()
        {
            var referenceTable = DBTable.GetTable(ReferenceType, Table.Schema, true);
            if (referenceTable == null)
            {
                throw new Exception($"{nameof(ReferenceType)}({Attribute.ColumnProperty} - {ReferenceType}) Table not found! Target table: {Table.Table}");
            }
            if (referenceTable.PrimaryKey == null)
            {
                throw new Exception($"{nameof(ReferenceType)}({Attribute.ColumnProperty} - {ReferenceType}) Primary key not found! Target table: {Table.Table}");
            }
            return referenceTable;
        }

        public virtual DBForeignKey Generate()
        {
            if (ReferenceType == null
                || Table == null || Table.Schema == null
                || Column == null || Column.Column == null)
            {
                throw new Exception($"{nameof(ReferenceAttribute)} is not initialized!");
            }
            if (ForeignKey == null)
            {
                var referenceTable = CheckReference();

                ForeignKey = new DBForeignKey()
                {
                    Table = Table.Table,
                    Column = Column.Column,
                    Reference = referenceTable.PrimaryKey,
                    Name = Attribute.Name,
                };
                Table.Table.Foreigns.Add(ForeignKey);
            }
            Column.Column.IsReference = true;
            ForeignKey.Property = PropertyInfo.Name;
            ForeignKey.PropertyInfo = PropertyInfo;
            if (ForeignKey.PropertyInvoker == null)
            {
                ForeignKey.PropertyInvoker = EmitInvoker.Initialize(PropertyInfo, true);
            }
            return ForeignKey;
        }
    }

    [Invoker(typeof(ReferenceGenerator), nameof(ReferenceGenerator.PropertyName))]
    public class ReferenceGeneratorPropertyNameInvoker : Invoker<ReferenceGenerator, string>
    {
        public static readonly ReferenceGeneratorPropertyNameInvoker Instance = new ReferenceGeneratorPropertyNameInvoker();
        public override string Name => nameof(ReferenceGenerator.PropertyName);

        public override bool CanWrite => false;

        public override string GetValue(ReferenceGenerator target) => target.PropertyName;

        public override void SetValue(ReferenceGenerator target, string value) { }
    }
}