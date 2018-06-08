/*
 * 
 QParamList.cs
 
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
using System.Data;
using System.Text;

namespace DataWF.Data
{
    public class QFunc : QEnum
    {
        protected QFunctionType type;

        public QFunc()
        { }

        public QFunc(QFunctionType type)
        {
            this.type = type;
        }

        public QFunctionType Type
        {
            get { return type; }
            set { type = value; }
        }

        public override object GetValue(DBItem row = null)
        {
            QItem item = items.Count == 0 ? null : items[0];
            if (type == QFunctionType.lower)
                return item.GetValue(row).ToString().ToLowerInvariant();
            if (type == QFunctionType.upper)
                return item.GetValue(row).ToString().ToUpperInvariant();
            if (type == QFunctionType.getdate)
                return DateTime.Now;
            if (type == QFunctionType.convert)
            {
                var param = items[0] as QType;
                var column = ((QColumn)items[1]).Column;
                var val = row[column];

                if (param.Type == DBDataType.Date)
                {
                    if (val == DBNull.Value)
                        return val;
                    else if (!(val is DateTime))
                        val = DateTime.Parse(val.ToString());
                    return ((DateTime)val).Date;
                }
                else if (param.Type == DBDataType.String)
                {
                    string temp = string.Empty;
                    if (val == DBNull.Value)
                        return string.Empty;
                    else if (val is DateTime)
                    {
                        string format = "yyyy-MM-dd HH:mm:ss.fff";
                        if (items.Count == 3)
                        {
                            string f = items[2].Text;
                            if (f == "112")
                                format = "yyyyMMdd";
                            else if (f == "12")
                                format = "yyMMdd";
                            else if (f == "6")
                                format = "dd MMM yyyy";
                        }
                        temp = ((DateTime)val).ToString(format);
                    }
                    else
                        temp = DBSystem.FormatText(val);
                    if (param.Size > 0 && temp.Length > param.Size)
                        temp = temp.Substring(0, (int)param.Size);
                    return temp;
                }
                else
                {
                }
            }
            if (type == QFunctionType.parse)
            {
                var param = items[0].Text.Trim('\'');
                var local = items.Count > 3 ? System.Globalization.CultureInfo.GetCultureInfo(items[4].Text.Trim('\'')) : System.Globalization.CultureInfo.CurrentCulture;
                var t = items[2] as QType;
                if (t.Type == DBDataType.Date || t.Type == DBDataType.DateTime)
                {
                    return DateTime.Parse(param, local);
                }
                else if (t.Type == DBDataType.Decimal)
                {
                    return decimal.Parse(param, local);
                }

            }
            if (type == QFunctionType.datename)
            {
                var param = items[0].Text;
                var column = ((QColumn)items[1]).Column;
                var val = row[column];
                if (!(val is DateTime))
                    val = DateTime.Parse(val.ToString());
                return ((DateTime)val).ToString(param);
            }
            return null;
        }

        public override string Format(IDbCommand command = null)
        {
            var rez = new StringBuilder();
            rez.Append(type.ToString());
            rez.Append("(");
            QItem prev = null;
            foreach (var item in items)
            {
                if (!items.IsFirst(item) && !(item is QComparer) && !(prev is QComparer))
                    rez.Append(", ");
                rez.Append(item.Format(command));
                prev = item;
            }
            rez.Append(")");
            return rez.ToString();
        }

        public static QFunctionType ParseFunction(string val)
        {
            QFunctionType f;
            if (!Enum.TryParse<QFunctionType>(val, true, out f))
                f = QFunctionType.none;
            return f;
        }
    }
}

