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
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace DataWF.Data
{
    public class QExpression : QEnum
    {
        private readonly List<QMathType> types = new List<QMathType>();

        public QExpression()
        {
        }

        public List<QMathType> Types
        {
            get { return types; }
        }

        public override string Format(IDbCommand command)
        {
            StringBuilder rez = new StringBuilder();
            for (int i = 0; i < types.Count; i++)
            {
                QMathType type = types[i];
                string mark = " + ";
                if (type == QMathType.Minus)
                    mark = " - ";
                if (type == QMathType.Multiply)
                    mark = " * ";
                if (type == QMathType.Devide)
                    mark = " / ";
                if (i == 0)
                    rez.Append(Items[i].Format(command));
                rez.AppendFormat("{0}{1}", mark, Items[i + 1].Format(command));
            }
            return base.Format(command);
        }

        public override object GetValue(DBItem row)
        {
            object value = null;
            for (int i = 0; i < types.Count; i++)
            {
                QMathType type = types[i];
                object val1 = (i == 0) ? items[i].GetValue(row) : value;
                object val2 = items[i + 1].GetValue(row);
                value = Calc(type, val1, val2);
            }
            return value;
        }

        public object Calc(QMathType type, object val1, object val2)
        {
            if (val1 is string || val2 is string)
            {
                return "" + val1 + val2;
            }
            else
            {
                decimal obj1 = GetDecimal(val1);
                decimal obj2 = GetDecimal(val2);

                if (type == QMathType.Plus)
                    return obj1 + obj2;
                else if (type == QMathType.Minus)
                    return obj1 - obj2;
                else if (type == QMathType.Multiply)
                    return obj1 * obj2;
                else // if (type == QExpressionMathType.Devide)
                    return obj2 == 0M ? 0M : obj1 / obj2;
            }
        }

        public decimal GetDecimal(object obj)
        {
            return obj is decimal ? (decimal)obj : obj == null || obj == DBNull.Value ? 0M : decimal.Parse(obj.ToString());
        }

        public void Parse(string query, IDBTable table)
        {
            QQuery q = new QQuery("", table);
            q.ParseExpression(this, query);
            //Regex exp = new Regex(@"\*|\+|\-|\/", RegexOptions.IgnoreCase);
            //string q = exp.Replace(query, ch => @" " + ch.Value + " ");
            //string[] split = q.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            //if (split.Length >= 3)
            //{
            //    switch (split[1])
            //    {
            //        case "+": type = QMathType.Plus; break;
            //        case "-": type = QMathType.Minus; break;
            //        case "*": type = QMathType.Multiply; break;
            //        case "/": type = QMathType.Devide; break;
            //    }
            //    Items.Add(split[0]);

            //    if (split.Length > 3)
            //    {
            //        QExpression sub = new QExpression();
            //        sub.Parse(split[2], table);
            //        Items.Add(sub);
            //    }
            //    else
            //        Items.Add(split[2]);
            //}
        }
    }

}
