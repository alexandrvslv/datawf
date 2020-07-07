//  The MIT License (MIT)
//
// Copyright © 2020 Vassilyev Alexandr
//
//   email:alexandr_vslv@mail.ru
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the “Software”), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
// and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of 
// the Software.
//
// THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
// THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
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
