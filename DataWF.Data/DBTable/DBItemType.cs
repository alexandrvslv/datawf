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
using System;
using System.ComponentModel;
using DataWF.Common;
using System.Xml.Serialization;
using System.Globalization;
using Newtonsoft.Json;

namespace DataWF.Data
{
    [TypeConverter(typeof(DBItemTypeConverter))]
    public class DBItemType
    {
        private Type type;

        public DBItemType()
        { }

        public Type Type
        {
            get { return type; }
            set
            {
                type = value;
                Constructor = EmitInvoker.Initialize(type, Type.EmptyTypes, true);
            }
        }

        [XmlIgnore, JsonIgnore]
        public EmitConstructor Constructor { get; set; }
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
            return TypeHelper.BinaryFormatType(((DBItemType)value).Type);
        }
    }
}
