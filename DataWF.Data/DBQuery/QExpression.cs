/*
 Query.cs
 
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
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace DataWF.Data
{
    public class QExpression : QEnum
    {
        private List<QMathType> types = new List<QMathType>();

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

        public void Parse(string query, DBTable table)
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
