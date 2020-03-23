/*
 DBTable.cs
 
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace DataWF.Data
{
    [TypeConverter(typeof(DBItemTypeConverter))]
    public class DBItemType
    {
        private Type type;
        private DBTable table;
        private List<IInvoker> invokers;

        public DBItemType()
        { }

        public Type Type
        {
            get { return type; }
            set { type = value; }
        }

        [XmlIgnore, JsonIgnore]
        public DBTable Table => table ?? (table = DBTable.GetTable(Type));

        [XmlIgnore, JsonIgnore]
        public virtual List<IInvoker> Invokers
        {
            get
            {
                if (invokers == null)
                {
                    var table = Table;
                    invokers = new List<IInvoker>(table.Columns.Count + (table.Generator?.Referencings.Count() ?? 0));
                    foreach (var column in table.Columns)
                    {
                        if (!table.IsSerializeableColumn(column, Type))
                            continue;

                        invokers.Add(column.PropertyInvoker);

                        //if ((column.Keys & DBColumnKeys.Group) == DBColumnKeys.Group)
                        //    continue;

                        if (column.ReferencePropertyInvoker != null)
                        {
                            invokers.Add(column.ReferencePropertyInvoker);
                        }
                    }
                    if (table.Generator != null)
                    {
                        foreach (var refing in table.Generator.Referencings)
                        {
                            if (!refing.PropertyInvoker.TargetType.IsAssignableFrom(Type))
                                continue;
                            invokers.Add(refing.PropertyInvoker);
                        }
                    }
                }
                return invokers;
            }
        }

        public DBItem Create()
        {
            return Table.NewItem(DBUpdateState.Insert, true, Type);
        }
    }

    public class DBItemTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var type = TypeHelper.ParseType(value.ToString());
            return type == null ? null : new DBItemType { Type = type };
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            return TypeHelper.FormatBinary(((DBItemType)value).Type);
        }
    }
}
