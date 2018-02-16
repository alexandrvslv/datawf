/*
 QItem.cs
 
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
using System.Data;

namespace DataWF.Data
{
    public class QType : QItem
    {
        public static readonly QType None = new QType(DBDataType.None);
        // public static readonly QType Date = new QType(DBDataType.Date);
        // public static readonly QType String = new QType(DBDataType.String);
        // public static readonly QType Number = new QType(DBDataType.Number);
        // public static readonly QType Integer = new QType(DBDataType.Integer);
        // public static readonly QType DateTime = new QType(DBDataType.DateTime);
        [DefaultValue(DBDataType.String)]
        DBDataType type = DBDataType.String;
        [DefaultValue(0)]
        decimal size = 0;

        public QType()
        { }

        public QType(DBDataType type)
        {
            // TODO: Complete member initialization
            this.type = type;
        }

        public DBDataType Type
        {
            get { return type; }
            set { type = value; }
        }

        public decimal Size
        {
            get { return size; }
            set { size = value; }
        }

        public override object GetValue(DBItem row = null)
        {
            return DBNull.Value;
        }

        public override string Format(IDbCommand command = null)
        {
            string rez = "";
            switch (type)
            {
                case DBDataType.String:
                    rez = "varchar";
                    break;
                case DBDataType.Date:
                    rez = "date";
                    break;
                case DBDataType.Decimal:
                    rez = "number";
                    break;
                case DBDataType.DateTime:
                    rez = "datetime";
                    break;
            }
            if (size > 0)
                rez += '(' + size.ToString() + ')';
            return rez;
        }

        internal static QType ParseType(string word)
        {
            if (word.Equals("date", StringComparison.OrdinalIgnoreCase))
                return new QType(DBDataType.Date);
            else if (word.Equals("datetime", StringComparison.OrdinalIgnoreCase))
                return new QType(DBDataType.DateTime);
            else if (word.Equals("varchar", StringComparison.OrdinalIgnoreCase))
                return new QType(DBDataType.String);
            else if (word.Equals("number", StringComparison.OrdinalIgnoreCase))
                return new QType(DBDataType.Decimal);
            else
                return None;
        }

    }
}
