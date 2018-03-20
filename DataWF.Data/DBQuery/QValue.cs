/*
 QColumn.cs
 
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
using System.Globalization;

namespace DataWF.Data
{
    public class QValue : QColumn
    {
        [DefaultValue(null)]
        protected object value = null;

        public QValue()
        { }

        public QValue(object value, DBColumn column = null)
        {
            Column = column;
            Value = value;
        }

        public object Value
        {
            get
            {
                if (value == null && text != null)
                    value = Column != null ? DBService.ParseValue(Column, text) : text;
                return value;
            }
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
                    value = DBService.ParseValue(Column, value);
                if (this.value != value)
                {
                    this.value = value;
                    this.text = DBService.FormatToSqlText(value);
                    OnPropertyChanged(nameof(Value));
                }
            }
        }

        public override string Text
        {
            get { return base.Text; }
            set
            {
                base.Text = value;
                this.value = null;
            }
        }

        public override string Format(IDbCommand command = null)
        {
            return (command == null || Value == DBNull.Value) ? DBService.FormatToSqlText(Value) : CreateParameter(command, Value);
        }

        public override object GetValue(DBItem row)
        {
            return Value;
        }

        public string CreateParameter(IDbCommand command, object value)
        {
            string name = (Table?.System?.ParameterPrefix ?? "@") + (Column?.Name ?? "param");

            //TODO optimise contains/duplicate
            int i = 0;
            string param = name + i;
            while (command.Parameters.Contains(param))
                param = name + ++i;

            var parameter = command.CreateParameter();
            //parameter.DbType = DbType.String;
            parameter.Direction = ParameterDirection.Input;
            parameter.ParameterName = param;
            parameter.Value = value;
            command.Parameters.Add(parameter);
            return param;
        }
    }
}