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
using System.Globalization;

namespace DataWF.Data
{
    public class QValue : QColumn
    {
        [DefaultValue(null)]
        protected object _value = null;

        public QValue()
        { }

        public QValue(object value, DBColumn column = null)
        {
            Column = column;
            Value = value;
        }

        public object Value
        {
            get => _value;
            set
            {
                if (Column == null)
                {
                    if (value is string)
                    {
                        DateTime date;
                        if (DateTime.TryParse((string)value, out date))
                            value = date;
                        else if (DateTime.TryParseExact((string)value, new string[] { "yyyyMMdd" }, CultureInfo.InvariantCulture.DateTimeFormat, DateTimeStyles.None, out date))
                            value = date;
                    }

                }
                else
                    value = Column.ParseValue(value);
                if (_value != value)
                {
                    _value = value;
                    OnPropertyChanged(nameof(Value));
                }
            }
        }

        public override DBColumn Column
        {
            get => base.Column;
            set
            {
                if (Column != value)
                {
                    base.Column = value;
                    if (Column != null)
                    {
                        _value = Column.ParseValue(_value);
                    }
                }
            }
        }

        public override string Format(IDbCommand command = null)
        {
            return command == null
                ? ToString()
                : CreateCommandParameter(command, Column != null ? Column.GetParameterValue(Value) : Value, Column);
        }

        public override string ToString()
        {
            return Column != null ? Column.FormatQuery(Value) : DBSystem.FormatQuery(Value);
        }

        public override object GetValue(DBItem row)
        {
            return Value;
        }


    }
}