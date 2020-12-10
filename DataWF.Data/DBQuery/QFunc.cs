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
using DataWF.Common;
using System;
using System.Collections;
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
            Type = type;
        }

        public QFunc(QFunctionType type, IEnumerable parameters) : base(parameters)
        {
            Type = type;
        }

        public QFunctionType Type
        {
            get => type;
            set => type = value;
        }

        public override object GetValue(DBItem row = null)
        {
            QItem item = items.Count == 0 ? null : items[0];
            switch (type)
            {
                case QFunctionType.lower:
                    return item.GetValue(row)?.ToString().ToLowerInvariant();
                case QFunctionType.upper:
                    return item.GetValue(row)?.ToString().ToUpperInvariant();
                case QFunctionType.initcap:
                    return Helper.ToInitcap(item.GetValue(row)?.ToString(), ' ');
                case QFunctionType.trim:
                    return item.GetValue(row)?.ToString().Trim();
                case QFunctionType.ltrim:
                    return item.GetValue(row)?.ToString().TrimStart();
                case QFunctionType.rtrim:
                    return item.GetValue(row)?.ToString().TrimEnd();
                case QFunctionType.getdate:
                    return DateTime.Now;
                case QFunctionType.concat:
                    var sb = new StringBuilder();
                    foreach (var parameter in items)
                    {
                        sb.Append(parameter.GetValue(row));
                    }
                    return sb.ToString();
                case QFunctionType.coalesce:
                    foreach (var parameter in items)
                    {
                        var value = parameter.GetValue(row);
                        if (value != null)
                            return value;
                    }
                    return null;
                case QFunctionType.convert:
                    {
                        var paramType = items[0] as QType;
                        var val = items[1].GetValue(row);

                        if (paramType.Type == DBDataType.Date)
                        {
                            if (val == DBNull.Value)
                                return val;
                            else if (!(val is DateTime))
                                val = DateTime.Parse(val.ToString());
                            return ((DateTime)val).Date;
                        }
                        else if (paramType.Type == DBDataType.String)
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
                                temp = DBSystem.FormatQuery(val);
                            if (paramType.Size > 0 && temp.Length > paramType.Size)
                                temp = temp.Substring(0, (int)paramType.Size);
                            return temp;
                        }
                        else
                        {
                        }

                        break;
                    }
                case QFunctionType.parse:
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

                        break;
                    }
                case QFunctionType.datename:
                    {
                        var param = items[0].Text;
                        var val = items[1].GetValue(row);
                        if (!(val is DateTime))
                            val = DateTime.Parse(val.ToString());
                        return ((DateTime)val).ToString(param);
                    }
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
                if (!items.IsFirst(item)
                    && !(item is QComparer)
                    && !(prev is QComparer))
                    rez.Append(", ");
                rez.Append(item.Format(command));
                prev = item;
            }
            rez.Append(")");
            return rez.ToString();
        }

        public static QFunctionType ParseFunction(string val)
        {
            if (!Enum.TryParse<QFunctionType>(val, true, out QFunctionType f))
                f = QFunctionType.none;
            return f;
        }
    }
}

