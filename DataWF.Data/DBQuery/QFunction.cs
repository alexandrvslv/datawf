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
using System.Globalization;
using System.Text;

namespace DataWF.Data
{
    public class QFunction : QArray
    {
        protected QFunctionType type;

        public QFunction()
        { }

        public QFunction(QFunctionType type)
        {
            Type = type;
        }

        public QFunction(QFunctionType type, IEnumerable parameters) : base(parameters)
        {
            Type = type;
        }

        public QFunctionType Type
        {
            get => type;
            set => type = value;
        }

        public override object GetValue(DBItem item)
        {
            QItem qItem = items.Count == 0 ? null : items[0];
            switch (type)
            {
                case QFunctionType.lower:
                    return qItem.GetValue(item)?.ToString().ToLowerInvariant();
                case QFunctionType.upper:
                    return qItem.GetValue(item)?.ToString().ToUpperInvariant();
                case QFunctionType.initcap:
                    return Helper.ToInitcap(qItem.GetValue(item)?.ToString(), ' ');
                case QFunctionType.trim:
                    return qItem.GetValue(item)?.ToString().Trim();
                case QFunctionType.ltrim:
                    return qItem.GetValue(item)?.ToString().TrimStart();
                case QFunctionType.rtrim:
                    return qItem.GetValue(item)?.ToString().TrimEnd();
                case QFunctionType.getdate:
                    return DateTime.Now;
                case QFunctionType.concat:
                    var sb = new StringBuilder();
                    foreach (var parameter in items)
                    {
                        sb.Append(parameter.GetValue(item));
                    }
                    return sb.ToString();
                case QFunctionType.coalesce:
                    foreach (var parameter in items)
                    {
                        var value = parameter.GetValue(item);
                        if (value != null)
                            return value;
                    }
                    return null;
                case QFunctionType.convert:
                    {
                        var paramType = items[0] as QType;
                        var val = items[1].GetValue(item);

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
                                    string f = items[2].GetValue(item)?.ToString();
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
                                temp = System.FormatQuery(val);
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
                        var param = items[0].GetValue(item)?.ToString().Trim('\'');
                        var local = items.Count > 3
                            ? CultureInfo.GetCultureInfo(items[4].GetValue(item)?.ToString().Trim('\''))
                            : CultureInfo.CurrentCulture;
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
                        var param = items[0].GetValue(item)?.ToString();
                        var val = items[1].GetValue(item);
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

        public static QFunctionType ParseFunction(ReadOnlySpan<char> val)
        {
            if (MemoryExtensions.Equals(val, nameof(QFunctionType.avg).AsSpan(), StringComparison.OrdinalIgnoreCase))
                return QFunctionType.avg;
            else if (MemoryExtensions.Equals(val, nameof(QFunctionType.cast).AsSpan(), StringComparison.OrdinalIgnoreCase))
                return QFunctionType.cast;
            else if (MemoryExtensions.Equals(val, nameof(QFunctionType.coalesce).AsSpan(), StringComparison.OrdinalIgnoreCase))
                return QFunctionType.coalesce;
            else if (MemoryExtensions.Equals(val, nameof(QFunctionType.concat).AsSpan(), StringComparison.OrdinalIgnoreCase))
                return QFunctionType.concat;
            else if (MemoryExtensions.Equals(val, nameof(QFunctionType.convert).AsSpan(), StringComparison.OrdinalIgnoreCase))
                return QFunctionType.convert;
            else if (MemoryExtensions.Equals(val, nameof(QFunctionType.count).AsSpan(), StringComparison.OrdinalIgnoreCase))
                return QFunctionType.count;
            else if (MemoryExtensions.Equals(val, nameof(QFunctionType.datename).AsSpan(), StringComparison.OrdinalIgnoreCase))
                return QFunctionType.datename;
            else if (MemoryExtensions.Equals(val, nameof(QFunctionType.distinct).AsSpan(), StringComparison.OrdinalIgnoreCase))
                return QFunctionType.distinct;
            else if (MemoryExtensions.Equals(val, nameof(QFunctionType.format).AsSpan(), StringComparison.OrdinalIgnoreCase))
                return QFunctionType.format;
            else if (MemoryExtensions.Equals(val, nameof(QFunctionType.getdate).AsSpan(), StringComparison.OrdinalIgnoreCase))
                return QFunctionType.getdate;
            else if (MemoryExtensions.Equals(val, nameof(QFunctionType.group).AsSpan(), StringComparison.OrdinalIgnoreCase))
                return QFunctionType.group;
            else if (MemoryExtensions.Equals(val, nameof(QFunctionType.initcap).AsSpan(), StringComparison.OrdinalIgnoreCase))
                return QFunctionType.initcap;
            else if (MemoryExtensions.Equals(val, nameof(QFunctionType.lower).AsSpan(), StringComparison.OrdinalIgnoreCase))
                return QFunctionType.lower;
            else if (MemoryExtensions.Equals(val, nameof(QFunctionType.ltrim).AsSpan(), StringComparison.OrdinalIgnoreCase))
                return QFunctionType.ltrim;
            else if (MemoryExtensions.Equals(val, nameof(QFunctionType.max).AsSpan(), StringComparison.OrdinalIgnoreCase))
                return QFunctionType.max;
            else if (MemoryExtensions.Equals(val, nameof(QFunctionType.min).AsSpan(), StringComparison.OrdinalIgnoreCase))
                return QFunctionType.min;
            else if (MemoryExtensions.Equals(val, nameof(QFunctionType.parse).AsSpan(), StringComparison.OrdinalIgnoreCase))
                return QFunctionType.parse;
            else if (MemoryExtensions.Equals(val, nameof(QFunctionType.rtrim).AsSpan(), StringComparison.OrdinalIgnoreCase))
                return QFunctionType.rtrim;
            else if (MemoryExtensions.Equals(val, nameof(QFunctionType.sum).AsSpan(), StringComparison.OrdinalIgnoreCase))
                return QFunctionType.sum;
            else if (MemoryExtensions.Equals(val, nameof(QFunctionType.to_char).AsSpan(), StringComparison.OrdinalIgnoreCase))
                return QFunctionType.to_char;
            else if (MemoryExtensions.Equals(val, nameof(QFunctionType.to_date).AsSpan(), StringComparison.OrdinalIgnoreCase))
                return QFunctionType.to_date;
            else if (MemoryExtensions.Equals(val, nameof(QFunctionType.trim).AsSpan(), StringComparison.OrdinalIgnoreCase))
                return QFunctionType.trim;
            else if (MemoryExtensions.Equals(val, nameof(QFunctionType.upper).AsSpan(), StringComparison.OrdinalIgnoreCase))
                return QFunctionType.upper;

            return QFunctionType.none;

        }
    }
}

