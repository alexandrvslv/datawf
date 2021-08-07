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
using System.Data;

namespace DataWF.Data
{
    public class QBetween : QItem, IBetween
    {
        private QItem min;
        private QItem max;

        public QBetween(object val1 = null, object val2 = null, DBColumn column = null)
        {
            if (val1 is QItem)
                min = (QItem)val1;
            else if (val1 is DBColumn)
                min = new QColumn((DBColumn)val1);
            else if (val1 != null)
                min = new QValue(val1, column);

            if (val2 is QItem)
                max = (QItem)val1;
            else if (val2 is DBColumn)
                max = new QColumn((DBColumn)val2);
            else if (val2 != null)
                max = new QValue(val2, column);
        }

        public override object GetValue(DBItem row = null)
        {
            return this;
        }

        public override string ToString()
        {
            return base.ToString();// string.Format("{0} {1}", value1, value2);
        }

        public override string Format(IDbCommand command = null)
        {
            string f1 = min?.Format(command);
            string f2 = max?.Format(command);
            if (string.IsNullOrEmpty(f1) || string.IsNullOrEmpty(f2))
                return string.Empty;

            var system = Query?.Table?.Schema?.System;
            if (system == DBSystem.MSSql
                || system == DBSystem.Postgres
                || system == DBSystem.SQLite)
                return f1 + " and " + f2;
            else
                return string.Format("({0}, {1})", f1, f2);
        }

        public object MaxValue()
        {
            return Max.GetValue();
        }

        public object MinValue()
        {
            return Min.GetValue();
        }

        public QItem Min
        {
            get => min;
            set => min = value;
        }

        public QItem Max
        {
            get => max;
            set => max = value;
        }

    }
}