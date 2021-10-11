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
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace DataWF.Data
{
    public class QTable : QItem
    {
        protected JoinType join = JoinType.Undefined;
        protected DBTable table;
        private QParam onParam;
        private bool? parametrized;
        private IEnumerable<QParam> parameters;

        public QTable()
        { }

        public QTable(IDBTable table, string alias = null)
        {
            Table = table;
            TableAlias = alias;
        }

        public override string Name
        {
            get => Table?.Name;
            set { }
        }

        public JoinType Join
        {
            get => join;
            set
            {
                if (join != value)
                {
                    join = value;
                    //OnPropertyChanged();
                }
            }
        }

        public override IDBTable Table
        {
            get => table;
            set
            {
                if (table != value)
                {
                    table = (DBTable)value;
                    //OnPropertyChanged();
                }
            }
        }

        public QParam On
        {
            get => onParam;
            set
            {
                onParam = value;
                onParam.Holder = this;
                //OnPropertyChanged();
            }
        }

        public bool HasParameters => GetParameters().Any();

        public override string Format(IDbCommand command = null)
        {
            return $"{Join.Format()} {System.FormatQTable(Table, TableAlias)} {(On != null ? $" on {On.Format(command)}" : string.Empty)}";
        }

        public override object GetValue(DBItem row)
        {
            return Name;
        }

        public IEnumerable<QParam> GetParameters()
        {
            return parameters ??=  Query.GetAllParameters(p => p.GetAllQItems<QColumn>(c => c.QTable == this).Any()).ToList();
        }
    }
}
