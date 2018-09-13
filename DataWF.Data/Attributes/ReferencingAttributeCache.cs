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
using DataWF.Common;

namespace DataWF.Data
{
    public class ReferencingAttributeCache
    {
        public ReferencingAttributeCache(TableAttributeCache table, PropertyInfo property, ReferencingAttribute referencingAttribuite)
        {
            if (!property.PropertyType.IsGenericType)
            {
                throw new InvalidOperationException($"{nameof(ReferencingAttribute)} Property type expect generic enumerable!");
            }

            var referenceTable = DBTable.GetTableAttributeInherit(property.PropertyType.GetGenericArguments()[0]);
            if (referenceTable == null)
            {
                throw new InvalidOperationException($"{nameof(ReferencingAttribute)} Property type expect {nameof(TableAttribute)}!");
            }

            var referenceColumn = referenceTable.GetColumnByProperty(referencingAttribuite.ReferenceProperty);
            if (referenceColumn == null)
            {
                throw new InvalidOperationException($"{nameof(ReferencingAttribute.ReferenceProperty)} expect {nameof(ColumnAttribute)}!");
            }
            Attribute = referencingAttribuite;
            Table = table;
            Property = property;
            ReferenceTable = referenceTable;
            ReferenceColumn = referenceColumn;
            PropertyInvoker = EmitInvoker.Initialize(property);
        }

        public ReferencingAttribute Attribute { get; set; }
        public TableAttributeCache Table { get; set; }
        public PropertyInfo Property { get; set; }
        public string PropertyName { get { return Property.Name; } }
        public TableAttributeCache ReferenceTable { get; set; }
        public ColumnAttributeCache ReferenceColumn { get; set; }
        public IInvoker PropertyInvoker { get; set; }

        public override string ToString()
        {
            return $"{PropertyName} {ReferenceTable?.Table}";
        }
    }
}