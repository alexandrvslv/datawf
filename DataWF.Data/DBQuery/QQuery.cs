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
using DocumentFormat.OpenXml.Drawing.Diagrams;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;

namespace DataWF.Data
{

    public class QQuery : QItem, IQuery, IDisposable, IQItemList
    {
        static readonly char[] separator = new char[] { ',' };

        public string CacheQuery;
        protected QParamList parameters;
        protected QItemList<QItem> columns;
        protected QItemList<QOrder> orders;
        protected QItemList<QColumn> groups;
        protected QItemList<QTable> tables;
        private bool refmode;
        private QQuery baseQuery;
        private DBStatus status = DBStatus.Empty;
        private Type type;

        public QQuery()
            : base()
        {
            tables = new QItemList<QTable>(this);
            parameters = new QParamList(this);
            parameters.CollectionChanged += OnParametersListChanged;
            columns = new QItemList<QItem>(this);
            orders = new QItemList<QOrder>(this);
            groups = new QItemList<QColumn>(this);
            order = 0;
        }

        public QQuery(DBTable table) : this()
        {
            Table = table;
        }

        public QQuery(Type type) : this(DBTable.GetTable(type))
        { }

        public QQuery(string query, DBTable table = null, IEnumerable cols = null, QQuery bquery = null)
            : this()
        {
            baseQuery = bquery;
            if (bquery != null)
            {
                order = bquery.order + 1;
            }

            Table = table;
            Parse(query);
            if (cols != null)
            {
                foreach (DBColumn col in cols)
                {
                    BuildColumn(col);
                }
            }
        }

        public QTable QTable
        {
            get => tables.FirstOrDefault();
            set => tables.Add(value);
        }

        public override DBTable Table
        {
            get => QTable?.Table;
            set
            {
                if (value != Table)
                {
                    if (value != null)
                        tables.Add(new QTable(value, "a"));
                    else
                        tables.Clear();
                }
            }
        }
        public QItemList<QTable> Tables => tables;

        public QItemList<QItem> Columns => columns;

        public QItemList<QOrder> Orders => orders;

        public QParamList Parameters => parameters;

        public IQItemList Owner => baseQuery ?? this;

        public override IQuery Query => Owner as IQuery;

        public Type TypeFilter
        {
            get => type;
            set
            {
                if (Table.ItemTypeKey == null || TypeFilter == value)
                {
                    return;
                }
                type = value;
                var param = GetByColumn(Table.ItemTypeKey);
                if (param == null)
                {
                    param = Table.GetTypeParam(type);
                    if (param != null)
                    {
                        param.IsDefault = true;
                        parameters.Insert(0, param);
                    }
                }
                else
                {
                    var typeIndex = Table.GetTypeIndex(type);
                    if (typeIndex <= 0)
                    {
                        parameters.Remove(param);
                    }
                    else
                    {
                        param.RightItem = new QValue(typeIndex, Table.ItemTypeKey);
                    }
                }
            }
        }

        public DBStatus StatusFilter
        {
            get => status;
            set
            {
                if (StatusFilter != value)
                {
                    status = value;
                    var param = GetByColumn(Table.StatusKey);
                    if (param == null)
                    {
                        param = Table.GetStatusParam(status);
                        param.IsDefault = true;
                        if (param != null)
                        {
                            parameters.Insert(0, param);
                        }
                    }
                    else
                    {
                        if (status == DBStatus.Empty)
                        {
                            parameters.Remove(param);
                        }
                        else
                        {
                            param.RightItem = Table.GetStatusEnum(status);
                        }
                    }
                }
            }
        }

        public bool IsRefence
        {
            get => refmode;
            set => refmode = value;
        }

        public QParam Add()
        {
            return parameters.Add();
        }

        public void SimpleFilter(object text)
        {
            //Parameters.Clear();
            if (Table != null)
            {
                bool sec = false;
                foreach (DBColumn column in Table.Columns)
                    if (column.ColumnType == DBColumnTypes.Default && (column.Keys & DBColumnKeys.View) == DBColumnKeys.View || (column.Keys & DBColumnKeys.Code) == DBColumnKeys.Code)
                    {
                        QParam param = null;
                        if (column.IsReference && column.ReferenceTable != Table)
                        {
                            QQuery q = new QQuery("", column.ReferenceTable);
                            q.SimpleFilter(text);
                            param = BuildParam(column, q);
                        }
                        else
                            param = BuildParam(column, text);
                        if (sec)
                            param.Logic = LogicType.Or;
                        sec = true;
                    }
            }
        }

        public string ParceStringConstant(string[] split, ref int i)
        {
            string val = "";
            for (int j = i; j < split.Length; j++, i++)
            {
                if (split[j] == "&B")
                    val += "(";
                else if (split[j] == "&E")
                    val += ")";
                else if (split[j] == "&S")
                    val += " ";
                else if (split[j] == "&C")
                    val += ",";
                else if (split[j] == "&L")
                    break;
                else
                    val += split[j];
            }

            return val;
        }

        public string ParseString(string query, ref int i, char start, char end)
        {
            string word = string.Empty;
            int k = 0;
            for (i++; i < query.Length; i++)
            {
                var c = query[i];
                if (c == end)
                    if (k > 0)
                        k--;
                    else
                        break;
                if (c == start)
                    k++;

                word += c;
            }
            return word;
        }

        public DBTable ParseTable(string word)
        {
            var table = DBService.Schems.ParseTable(word);
            if (table == null)
            {
                table = DBService.Schems.ParseTableByTypeName(word);
            }
            return table;
        }

        public DBColumn ParseColumn(string word)
        {
            if (tables.Count == 1)
            {
                DBColumn column = tables[0].Table.ParseColumnProperty(word);
                if (column != null)
                    return column;
            }
            else if (tables.Count > 1)
            {
                string prefix = word.Substring(0, word.IndexOf('.'));
                foreach (var table in tables)
                    if (prefix.Equals(table.Text, StringComparison.OrdinalIgnoreCase) ||
                        prefix.Equals(table.Alias, StringComparison.OrdinalIgnoreCase))
                        return table.Table.ParseColumnProperty(word);
            }
            var q = Query as QQuery;
            while (q != this)
            {
                var column = q.ParseColumn(word);
                if (column != null)
                {
                    IsRefence = true;
                    return column;
                }
                q = q.Query as QQuery;
            }
            return null;
        }
        
        public int FindFrom(string query)
        {
            int exit = 0;
            string word = string.Empty;

            for (int i = 0; i <= query.Length; i++)
            {
                var c = i < query.Length ? query[i] : '\n';
                if (c == ' ' && exit <= 0)
                {
                    if (word.Equals("from", StringComparison.OrdinalIgnoreCase))
                        return i;
                    word = string.Empty;
                }
                else if (c == '(')
                    exit++;
                else if (c == ')')
                    exit--;
                else
                    word += c;
            }
            return query.Length;
        }

        public void Parse(string query)
        {
            CacheQuery = query;
            parameters.Clear();
            columns.Clear();
            orders.Clear();
            if (query == null || query.Length == 0)
                return;
            object val = null;
            bool alias = false;
            bool not = false;
            QParcerState state = QParcerState.Where;

            QParam parametergroup = null;
            QParam parameter = null;
            QTable table = null;
            QColumn column = null;
            QFunc func = null;
            QQuery sub = null;
            QOrder order = null;

            List<string> prefix = new List<string>();
            string word = string.Empty;
            if (Table == null)
            {
                for (int i = FindFrom(query); i <= query.Length; i++)
                {
                    var c = i < query.Length ? query[i] : '\n';

                    if (c == ' ' || c == '\n' || c == '\r')
                    {
                        if (word.Equals("where", StringComparison.OrdinalIgnoreCase))
                            break;
                        var tb = ParseTable(word);

                        if (tb != null)
                        {
                            table = new QTable(tb);
                            tables.Add(table);
                        }
                        else if (table != null)
                        {
                            table.Alias = word;
                        }
                        word = string.Empty;
                    }
                    else
                        word += c;
                }
            }
            word = string.Empty;
            for (int i = 0; i <= query.Length; i++)
            {
                var c = i < query.Length ? query[i] : '\n';
                if (c == '.')
                {
                    prefix.Add(word);
                    word = string.Empty;
                }
                else if (c == '\'' || c == ' ' || c == ',' || c == '(' || c == ')' || c == '\n' || c == '\r' || c == '!' || c == '=' || c == '>' || c == '<')//word.Length > 0 && 
                {
                    if (word.Equals("select", StringComparison.OrdinalIgnoreCase))
                    {
                        state = QParcerState.Select;
                    }
                    else if (word.Equals("from", StringComparison.OrdinalIgnoreCase))
                    {
                        prefix.Clear();
                        state = QParcerState.From;
                    }
                    else if (word.Equals("where", StringComparison.OrdinalIgnoreCase))
                    {
                        prefix.Clear();
                        table = null;
                        state = QParcerState.Where;
                    }
                    else if (word.Equals("order", StringComparison.OrdinalIgnoreCase))
                    {
                        i = query.IndexOf("by", i, StringComparison.OrdinalIgnoreCase) + 1;
                        state = QParcerState.OrderBy;
                    }
                    else if (word.Equals("group", StringComparison.OrdinalIgnoreCase))
                    {
                        i = query.IndexOf("by", i, StringComparison.OrdinalIgnoreCase) + 1;
                        state = QParcerState.GroupBy;
                    }
                    else
                    {
                        switch (state)
                        {
                            case QParcerState.Select:
                                if (word.Length > 0)
                                {
                                    if (word.Equals("as", StringComparison.OrdinalIgnoreCase) && c == ' ')
                                    {
                                        alias = true;
                                    }
                                    else
                                    {
                                        if (alias)
                                        {
                                            if (column != null)
                                            {
                                                column.Alias = word;
                                                column = null;
                                            }
                                            else if (sub != null)
                                            {
                                                sub.Alias = word;
                                                sub = null;
                                            }
                                            alias = false;
                                        }
                                        else
                                        {
                                            var fn = QFunc.ParseFunction(word);
                                            if (fn != QFunctionType.none)
                                            {
                                                func = new QFunc(fn);
                                                columns.Add(func);
                                            }
                                            else
                                            {
                                                var scolumn = ParseColumn(word);
                                                if (scolumn != null)
                                                {
                                                    column = new QColumn(scolumn) { Prefix = prefix.FirstOrDefault() };
                                                    columns.Add(column);
                                                    prefix.Clear();
                                                }
                                            }
                                        }
                                    }
                                }
                                if (c == '(')
                                {
                                    string word2 = ParseString(query, ref i, '(', ')').Trim();
                                    if (word2.StartsWith("select", StringComparison.OrdinalIgnoreCase))
                                    {
                                        sub = new QQuery(word2, null, null, this);
                                        columns.Add(sub);
                                        sub = null;

                                    }
                                    else if (func != null)
                                    {
                                        ParseFunc(func, word2);
                                        func = null;
                                    }
                                    else
                                    {
                                        QExpression expression = new QExpression();
                                        ParseExpression(expression, word2);
                                    }
                                }

                                break;
                            case QParcerState.From:
                                {
                                    if (word.Length > 0)
                                    {
                                        //Table = ParseTable(word);
                                    }
                                }
                                break;
                            case QParcerState.Where:
                                if (word.Length > 0)
                                {
                                    if (parameter == null || parameter.IsCompaund ||
                                        (parameter.RightItem != null && parameter.Comparer.Type != CompareTypes.Between) ||
                                        (parameter.RightItem is QBetween && ((QBetween)parameter.RightItem).Max != null))
                                    {
                                        column = null;
                                        parameter = new QParam();
                                        if (parametergroup != null)
                                            parametergroup.Parameters.Add(parameter);
                                        else
                                            parameters.Add(parameter);
                                    }

                                    if (Helper.IsDecimal(word))
                                    {
                                        val = decimal.Parse(word, NumberStyles.Number, CultureInfo.InvariantCulture.NumberFormat);
                                        parameter.SetValue(new QValue(val, column?.Column) { Text = word });
                                    }
                                    else if (word.Equals("true", StringComparison.OrdinalIgnoreCase))
                                    {
                                        parameter.SetValue(new QValue(true, column?.Column));
                                    }
                                    else if (word.Equals("false", StringComparison.OrdinalIgnoreCase))
                                    {
                                        parameter.SetValue(new QValue(false, column?.Column));
                                    }
                                    else if (word.Equals("null", StringComparison.OrdinalIgnoreCase))
                                    {
                                        parameter.SetValue(new QValue(DBNull.Value, column?.Column));
                                    }
                                    else if (word.Equals("not", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (parameter.Comparer.Type == CompareTypes.Is)
                                            parameter.Comparer = new CompareType(CompareTypes.Is, true);
                                        else if (parameter.Logic.Type != LogicTypes.Undefined && parameter.LeftItem == null)
                                            parameter.Logic = new LogicType(parameter.Logic.Type, true);
                                        else
                                            not = true;
                                    }
                                    else
                                    {
                                        var cm = CompareType.Parse(word);
                                        if (cm != CompareTypes.Undefined)
                                        {
                                            parameter.Comparer = new CompareType(cm, not);
                                        }
                                        else
                                        {
                                            var lg = LogicType.Parse(word);
                                            if (lg != LogicTypes.Undefined)
                                            {
                                                if (parameter.Comparer.Type == CompareTypes.Between && lg == LogicTypes.And)
                                                {
                                                    parameter.RightItem = new QBetween(parameter.RightItem, null, column?.Column);
                                                }
                                                else
                                                {
                                                    parameter.Logic = new LogicType(lg);
                                                }
                                                prefix.Clear();
                                            }
                                            else
                                            {
                                                var fn = QFunc.ParseFunction(word);
                                                if (fn != QFunctionType.none)
                                                {
                                                    func = new QFunc(fn);
                                                    parameter.SetValue(func);
                                                    prefix.Clear();
                                                }
                                                else
                                                {
                                                    var wcolumn = ParseColumn(word);
                                                    if (wcolumn != null && (prefix.Count == 0 || tables.Any(p => string.Equals(p.Alias, prefix.FirstOrDefault(), StringComparison.OrdinalIgnoreCase))))
                                                    {
                                                        column = new QColumn(wcolumn) { Prefix = prefix.FirstOrDefault() };
                                                        prefix.Clear();
                                                        parameter.SetValue(column);
                                                    }
                                                    else
                                                    {
                                                        if (parameter.LeftItem == null)
                                                        {
                                                            if (prefix.Count > 0)
                                                            {
                                                                var pQuery = this;
                                                                var pTable = Table;
                                                                foreach (var pcolumn in prefix)
                                                                {
                                                                    var dbColumn = pTable.ParseColumnProperty(pcolumn);

                                                                    if (dbColumn != null)
                                                                    {
                                                                        column = new QColumn(dbColumn);
                                                                        parameter.SetValue(column);
                                                                        parameter.Comparer = CompareType.In;
                                                                        pTable = dbColumn.ReferenceTable;
                                                                        pQuery = new QQuery("", pTable, new[] { pTable.PrimaryKey }, pQuery);
                                                                        parameter.SetValue(pQuery);
                                                                        parameter = pQuery.Parameters.Add();
                                                                    }
                                                                    else
                                                                    {
                                                                        var referencing = pTable.ParseReferencing(pcolumn);
                                                                        if (referencing != null)
                                                                        {
                                                                            column = new QColumn(pTable.PrimaryKey);
                                                                            parameter.SetValue(column);
                                                                            parameter.Comparer = CompareType.In;
                                                                            pTable = referencing.ReferenceTable.Table;
                                                                            pQuery = new QQuery("", pTable, new[] { referencing.ReferenceColumn.Column }, pQuery);
                                                                            parameter.SetValue(pQuery);
                                                                            parameter = pQuery.Parameters.Add();
                                                                        }
                                                                    }
                                                                }
                                                                var lastColumn = pTable.ParseColumnProperty(word);
                                                                column = new QColumn(lastColumn);
                                                                parameter.SetValue(column);
                                                                prefix.Clear();
                                                            }
                                                            else
                                                            {
                                                                var invoker = EmitInvoker.Initialize(Table.ItemType.Type, word);
                                                                if (invoker != null)
                                                                {
                                                                    parameter.SetValue(new QReflection(invoker));
                                                                    prefix.Clear();
                                                                }
                                                            }
                                                        }
                                                        else// if (parameter.Column != null)
                                                        {
                                                            parameter.SetValue(new QValue(word, parameter.LeftColumn));
                                                            prefix.Clear();
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                                switch (c)
                                {
                                    case '!':
                                        i++;
                                        parameter.Comparer = CompareType.NotEqual;
                                        break;
                                    case '=':
                                        parameter.Comparer = CompareType.Equal;
                                        break;
                                    case '>':
                                        if (query[i + 1] == '=')
                                        {
                                            i++;
                                            parameter.Comparer = CompareType.GreaterOrEqual;
                                        }
                                        else
                                            parameter.Comparer = CompareType.Greater;
                                        break;
                                    case '<':
                                        if (query[i + 1] == '=')
                                        {
                                            i++;
                                            parameter.Comparer = CompareType.LessOrEqual;
                                        }
                                        else if (query[i + 1] == '>')
                                        {
                                            i++;
                                            parameter.Comparer = CompareType.NotEqual;
                                        }
                                        else
                                            parameter.Comparer = CompareType.Less;
                                        break;
                                    case '\'':
                                        parameter.SetValue(new QValue(ParseString(query, ref i, '\'', '\''), column?.Column));
                                        break;
                                    case '(':
                                        int j = i;
                                        string word2 = ParseString(query, ref i, '(', ')').Trim();
                                        if (word2.StartsWith("select", StringComparison.OrdinalIgnoreCase))
                                        {
                                            sub = new QQuery(word2, null, null, this);
                                            parameter.SetValue(sub);
                                        }
                                        else if (func != null)
                                        {
                                            ParseFunc(func, word2);
                                            func = null;
                                        }
                                        else if (parameter != null && parameter.Comparer.Type != CompareTypes.Undefined)
                                        {
                                            var list = new QEnum();
                                            foreach (var s in word2.Split(','))
                                            {
                                                list.Items.Add(new QValue(s.Trim(' ', '\''), column?.Column));
                                            }

                                            parameter.SetValue(list);
                                            if (parameter.Comparer.Type == CompareTypes.Between && parameter.RightItem is QEnum)
                                            {
                                                var qEnum = (QEnum)parameter.RightItem;
                                                var between = new QBetween(qEnum.Items[0], qEnum.Items[1], column?.Column);
                                                parameter.RightItem = between;
                                            }
                                        }
                                        else
                                        {
                                            i = j;
                                            if (parameter == null)
                                            {
                                                parameter = new QParam();
                                                if (parametergroup != null)
                                                    parametergroup.Parameters.Add(parameter);
                                                else
                                                    parameters.Add(parameter);
                                            }
                                            parametergroup = parameter;
                                            parameter = null;
                                            //ParseParameter(parameter, word2);
                                        }

                                        break;
                                    case ')':
                                        parametergroup = parametergroup.Group;
                                        break;
                                }

                                break;
                            case QParcerState.OrderBy:
                                if (word.Length == 0)
                                    continue;

                                var cl = ParseColumn(word);
                                if (cl != null && (prefix.Count == 0 || tables.Any(p => string.Equals(p.Alias, prefix.FirstOrDefault(), StringComparison.OrdinalIgnoreCase))))
                                {
                                    order = new QOrder
                                    {
                                        Column = new QColumn(cl) { Prefix = prefix.FirstOrDefault() }
                                    };
                                    orders.Add(order);
                                    prefix.Clear();
                                }
                                else if (word.Equals("asc", StringComparison.OrdinalIgnoreCase))
                                {
                                    if (order != null)
                                    {
                                        order.Direction = ListSortDirection.Ascending;
                                        order = null;
                                    }
                                }
                                else if (word.Equals("desc", StringComparison.OrdinalIgnoreCase))
                                {
                                    if (order != null)
                                    {
                                        order.Direction = ListSortDirection.Descending;
                                        order = null;
                                    }
                                }
                                else
                                {
                                    if (prefix.Count > 0)
                                    {
                                        var property = $"{string.Join(".", prefix)}.{word}";
                                        prefix.Clear();
                                        var invoker = EmitInvoker.Initialize(Table.ItemType.Type, property);
                                        if (invoker != null)
                                        {
                                            order = new QOrder
                                            {
                                                Column = new QReflection(invoker)
                                            };
                                            orders.Add(order);
                                            prefix.Clear();
                                        }
                                    }
                                    else
                                    {
                                        var invoker = EmitInvoker.Initialize(Table.ItemType.Type, word);
                                        if (invoker != null)
                                        {
                                            order = new QOrder
                                            {
                                                Column = new QReflection(invoker)
                                            };
                                            orders.Add(order);
                                            prefix.Clear();
                                        }
                                    }
                                }
                                break;
                        }
                    }
                    word = string.Empty;
                }
                else
                    word += c;
            }
        }

        //       public void ParseExpression(QParam param, string query)
        //       {
        //string word = string.Empty;
        //for (int i = 0; i <= query.Length; i++)
        //{
        //    var c = i < query.Length ? query[i] : '\n';
        //    if (c == ',' || c == '\r' || c == '\n' || c == '(' || c == ' ')
        //    {
        //    }           
        //}
        //       }

        public void ParseExpression(QExpression expression, string query)
        {
            QFunc func = null;
            string word = string.Empty;
            for (int i = 0; i <= query.Length; i++)
            {
                var c = i < query.Length ? query[i] : '\n';
                if (c == '+' || c == '-' || c == '/' || c == '*')
                {
                    expression.Types.Add(ParseMath(c));
                }
                else if (c == ',' || c == '\r' || c == '\n' || c == '(' || c == ' ')
                {
                    if (word.Length > 0)
                    {
                        var fn = QFunc.ParseFunction(word);
                        var col = ParseColumn(word);

                        if (fn != QFunctionType.none)
                        {
                            func = new QFunc(fn);
                            expression.Items.Add(func);
                        }
                        else if (col != null)
                            expression.Items.Add(new QColumn(col));
                        else
                            expression.Items.Add(new QValue(word));

                        word = string.Empty;
                    }

                    if (c == '(')
                    {
                        string word2 = ParseString(query, ref i, '(', ')').Trim();
                        if (word2.StartsWith("select", StringComparison.OrdinalIgnoreCase))
                        {
                            expression.Items.Add(new QQuery(word2, null, null, this));
                        }
                        else if (func != null)
                        {
                            ParseFunc(func, word2);
                            func = null;
                        }
                        else
                        {
                            QExpression ex = new QExpression();
                            ParseExpression(ex, word2);
                            expression.Items.Add(ex);
                        }
                    }
                }
                else
                    word += c;
            }
        }

        public void ParseFunc(QFunc function, string query)
        {
            QFunc func = null;
            QType qtype = null;
            string word = string.Empty;
            for (int i = 0; i <= query.Length; i++)
            {
                var c = i < query.Length ? query[i] : '\n';
                if (c == '\'')
                {
                    var literal = ParseString(query, ref i, '\'', '\'');
                    function.Items.Add(new QValue(literal));
                }
                else if (c == ',' || c == '\r' || c == '\n' || c == '(' || c == ' ')
                {
                    if (word.Length > 0)
                    {
                        if (Helper.IsDecimal(word))
                        {
                            function.Items.Add(new QItem(word));
                        }
                        else
                        {
                            var comp = CompareType.Parse(word);
                            if (comp != CompareTypes.Undefined)
                            {
                                function.Items.Add(new QComparer(comp));
                            }
                            else
                            {
                                var type = QType.ParseType(word);
                                if (type != QType.None)
                                {
                                    qtype = type;
                                    function.Items.Add(type);
                                }
                                else
                                {
                                    var fn = QFunc.ParseFunction(word);
                                    if (fn != QFunctionType.none)
                                    {
                                        func = new QFunc(fn);
                                        function.Items.Add(func);
                                    }
                                    else
                                    {
                                        var col = ParseColumn(word);
                                        if (col != null)
                                            function.Items.Add(new QColumn(col));
                                        else
                                            function.Items.Add(new QItem(word));
                                    }
                                }
                            }
                        }
                        word = string.Empty;
                    }

                    if (c == '(')
                    {
                        string word2 = ParseString(query, ref i, '(', ')').Trim();
                        if (word2.StartsWith("select", StringComparison.OrdinalIgnoreCase))
                        {
                            function.Items.Add(new QQuery(word2, null, null, this));
                        }
                        else if (func != null)
                        {
                            ParseFunc(func, word2);
                            func = null;
                        }
                        else if (Helper.IsDecimal(word2))
                        {
                            qtype.Size = decimal.Parse(word2, NumberStyles.Number, CultureInfo.InvariantCulture.NumberFormat);
                            qtype = null;
                        }
                        else
                        {
                            QExpression ex = new QExpression();
                            ParseExpression(ex, word2);
                            function.Items.Add(ex);
                        }
                    }
                }
                else
                    word += c;
            }
        }

        public void Sort<T>(List<T> list)
        {
            DBComparerList comparer = GetComparer();
            if (comparer != null)
            {
                ListHelper.QuickSort(list, comparer);
            }
        }

        public DBComparerList GetComparer()
        {
            var comparer = new DBComparerList();
            foreach (QOrder order in Orders)
            {
                var comparerEntry = order.CreateComparer();
                if (comparerEntry != null)
                {
                    comparer.Comparers.Add(comparerEntry);
                }
            }
            return comparer.Comparers.Count == 0 ? null : comparer;
        }

        public override string ToString()
        {
            string rez = "Query";
            if (Table != null)
                rez = Table.ToString();
            return rez;
        }

        public QParam CreateParam(DBColumn column, object value)
        {
            return CreateParam(LogicType.And, column, CompareType.Equal, value);
        }

        public QParam CreateParam(DBColumn column, CompareType comp, object value)
        {
            return CreateParam(LogicType.And, column, comp, value);
        }

        public QParam CreateParam(LogicType logic, DBColumn column, CompareType compare, object value)
        {
            if (Table is IDBVirtualTable
                && column?.Table != Table)
            {
                column = column.GetVirtualColumn(Table);
            }
            QParam param = new QParam
            {
                Logic = logic,
                LeftItem = new QColumn(column),
                Comparer = compare,
                RightValue = value
            };
            return param;
        }

        public QParam BuildNameParam(string property, CompareType comparer, object value)
        {
            var parameter = new QParam();
            foreach (var item in Table.Columns.Select(DBColumn.GroupNameInvoker<DBColumn>.Instance, CompareType.Equal, property))
            {
                parameter.Parameters.Add(CreateParam(LogicType.Or, item, comparer, value));
            }
            Parameters.Add(parameter);
            return parameter;
        }

        public QParam BuildPropertyParam(string property, CompareType comparer, object value)
        {
            return BuildParam(Table.ParseProperty(property), comparer, value);
        }

        public QParam BuildParam(string column, object value, QQueryBuildParam buildParam = QQueryBuildParam.None)
        {
            int index = column.IndexOf('.');
            if (index >= 0)
            {
                string[] split = column.Split(DBService.DotSplit);//TODO JOIN
                DBTable table = Table;
                QQuery q = this;
                QParam param = null;
                DBColumn dbColumn = null;
                for (int i = 0; i < split.Length; i++)
                {
                    dbColumn = table.Columns[split[i]];
                    if (dbColumn != null)
                        ///q.Columns.Add(new QColumn(column.ReferenceTable.PrimaryKey.Code));
                        if (dbColumn.IsReference && i + 1 < split.Length)
                        {
                            param = CreateParam(LogicType.Undefined, dbColumn, CompareType.In, null);
                            q.Parameters.Add(param);
                            table = dbColumn.ReferenceTable;
                            q = new QQuery("", table);
                            param.RightItem = q;
                            q.Columns.Add(new QColumn(dbColumn.ReferenceTable.PrimaryKey.Name));
                        }
                }
                q.BuildParam(dbColumn, value, buildParam);
                param = parameters[parameters.Count - 1];
                if (parameters.Count > 1)
                {
                    param.Logic = LogicType.And;
                }

                return param;
            }
            else
            {
                return BuildParam(Table.Columns[column], value, buildParam);
            }
        }

        public QParam BuildParam(DBColumn column, object value, QQueryBuildParam buildParam = QQueryBuildParam.None)
        {
            CompareType comparer = CompareType.Equal;
            if (value is QQuery)
            {
                comparer = CompareType.In;
            }
            else if (value is DBItem)
            {
                if (value is DBGroupItem)
                {
                    value = ((DBGroupItem)value).GetSubGroupFull(true);
                    comparer = CompareType.In;
                }
                else
                {
                    value = ((DBItem)value).PrimaryId;
                }
            }
            else if (column.DataType == typeof(string))
            {
                if ((buildParam & QQueryBuildParam.AutoLike) != 0)
                    comparer = CompareType.Like;
                else if ((buildParam & QQueryBuildParam.SplitString) != 0
                    && value.ToString().Contains(','))
                    comparer = CompareType.In;
            }
            else if (value is DateInterval)
            {
                comparer = CompareType.Between;
            }
            else if (column.DataType == typeof(DateTime))
            {
                comparer = CompareType.Equal;
            }

            return BuildParam(column, comparer, value, buildParam);
        }

        public QParam BuildParam(DBColumn column, CompareType comparer, object value, QQueryBuildParam buildParam = QQueryBuildParam.None)
        {
            if (column == null)
                return null;

            QParam param = null;
            if (column.DataType == typeof(string) &&
                (comparer.Type == CompareTypes.Like
                || comparer.Type == CompareTypes.Equal
                || comparer.Type == CompareTypes.In))
            {
                string like = (buildParam & QQueryBuildParam.AutoLike) != 0
                    && comparer.Type == CompareTypes.Like ? "%" : "";

                if ((buildParam & QQueryBuildParam.SplitString) != 0
                    && value is string stringValue)
                {
                    string[] split = stringValue.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                    if (split.Length == 1)
                    {
                        param = CreateParam(column, comparer, like + split[0].Trim() + like);
                    }
                    else if (comparer.Type == CompareTypes.In)
                    {
                        param = CreateParam(column, comparer, split);
                    }
                    else
                    {
                        param = parameters.Add();
                        foreach (string item in split)
                        {
                            if (item.Trim().Length == 0)
                            {
                                continue;
                            }

                            param.Parameters.Add(CreateParam(param.Parameters.Count == 0
                                ? LogicType.And
                                : LogicType.Or,
                                column,
                                comparer,
                                like + item.Trim() + like));
                        }
                    }
                }
                else
                {
                    param = CreateParam(column, comparer, like + value + like);
                }
            }
            else
            {
                param = CreateParam(column, comparer, value);
            }

            if (param != null && param.Query == null)
            {
                Parameters.Add(param);
            }

            return param;
        }

        public QParam BuildParam(DBColumn parent, DBColumn column, object p, QQueryBuildParam buildParam)
        {
            var query = new QQuery("", column.Table);
            query.BuildColumn(column.Table.PrimaryKey);
            query.BuildParam(column, p, buildParam);
            var param = CreateParam(parent, CompareType.In, query);
            Parameters.Add(param);
            return param;
        }

        public static QMathType ParseMath(char code)
        {
            QMathType en = QMathType.None;
            if (code.Equals('+'))
                en = QMathType.Plus;
            else if (code.Equals('-'))
                en = QMathType.Minus;
            else if (code.Equals('/'))
                en = QMathType.Devide;
            else if (code.Equals('*'))
                en = QMathType.Multiply;

            return en;
        }

        public static string MathCode(QMathType type)
        {
            switch (type)
            {
                case (QMathType.Devide):
                    return "/";
                case (QMathType.Minus):
                    return "-";
                case (QMathType.Multiply):
                    return "*";
                case (QMathType.Plus):
                    return "+";
            }
            return "";
        }

        public IEnumerable<QParam> GetAllParameters()
        {
            foreach (var param in Parameters)
            {
                foreach (var item in GetParameters(param))
                {
                    yield return item;
                }
            }
        }

        private IEnumerable<QParam> GetParameters(QParam param)
        {
            yield return param;

            if (param.IsCompaund)
            {
                foreach (QParam parameter in param.Parameters)
                {
                    foreach (var subParam in GetParameters(parameter))
                    {
                        yield return subParam;
                    }
                }
                yield break;
            }
            if (param.LeftValue is QQuery leftQuery)
            {
                foreach (var subParam in leftQuery.GetAllParameters())
                {
                    yield return subParam;
                }
            }
            if (param.RightItem is QQuery rightQuery)
            {
                foreach (var subParam in rightQuery.GetAllParameters())
                {
                    yield return subParam;
                }
            }
        }

        public void OnParametersListChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
        }

        public bool CheckItem(DBItem item)
        {
            return QParam.CheckItem(item, Parameters);
        }

        public override string Format(IDbCommand command = null)
        {
            return FormatAll(command);
        }

        public string FormatAll(IDbCommand command = null, bool defColumns = false)
        {
            var cols = new StringBuilder();
            var order = new StringBuilder();
            var from = new StringBuilder();
            var whr = ToWhere(command);

            if (defColumns || columns.Count == 0)
            {
                var table = tables.FirstOrDefault();
                foreach (var col in table.Table.Columns.Where(p => !p.IsFile))
                {
                    string temp = table.Table.FormatQColumn(col, table.Alias);
                    if (temp != null && temp.Length > 0)
                    {
                        if (!table.Table.Columns.IsFirst(col))
                            cols.Append(", ");
                        cols.Append(temp);
                    }
                }
            }
            else
            {
                foreach (QItem col in columns)
                {
                    string temp = col.Format(command);
                    if (temp.Length > 0)
                    {
                        if (!columns.IsFirst(col))
                            cols.Append(", ");
                        cols.Append(temp);
                    }
                }
            }

            foreach (QOrder col in orders)
            {
                var formatOrder = col.Format(command);
                if (!string.IsNullOrEmpty(formatOrder))
                {
                    order.Append(formatOrder);
                    if (!orders.IsLast(col))
                        order.Append(", ");
                }
            }

            foreach (QTable table in tables)
            {
                from.Append(table.Format(command));
                if (!tables.IsLast(table))
                    from.Append(", ");
            }
            return $@"select {cols.ToString()}
    from {from} 
{(whr.Length > 0 ? "    where " : string.Empty)}{whr}
{(order.Length > 0 ? "    order by " : string.Empty)}{order}";
        }

        public string ToWhere(IDbCommand command = null)
        {
            var wbuf = new StringBuilder();
            for (int i = 0; i < parameters.Count; i++)
            {
                QParam param = parameters[i];
                string bufRez = param.Format(command);
                if (bufRez.Length > 0)
                    wbuf.Append((wbuf.Length == 0 || i == 0 ? "" : param.Logic.Format() + " ") + bufRez + " ");
            }
            var buf = new StringBuilder();
            //parameters._ApplySort("Order");
            if (command != null
                && Table is IDBVirtualTable vtable
                && vtable.FilterQuery.Parameters.Count > 0)
            {
                if (wbuf.Length > 0)
                    buf.Append("(");
                foreach (QParam param in vtable.FilterQuery.Parameters)
                {
                    if (Contains(param.LeftColumn))
                        continue;
                    string bufRez = param.Format(command);
                    if (bufRez.Length > 0)
                    {
                        //bufRez = $"{QTable.Alias}.{bufRez}";
                        buf.Append((buf.Length <= 1 ? "" : param.Logic.Format() + " ") + bufRez + " ");
                    }
                }
                if (wbuf.Length > 0)
                    buf.Append(") and (");
            }
            buf.Append(wbuf);
            if (Table is IDBVirtualTable && command != null && wbuf.Length > 0)
                buf.Append(")");
            return buf.ToString();
        }

        public IDbCommand ToCommand(bool defcolumns = false)
        {
            var command = Table.Schema.Connection.CreateCommand();
            command.CommandText = FormatAll(command, defcolumns);
            return command;
        }

        public string ToText()
        {
            string buf = string.Empty;
            foreach (QParam param in parameters)
            {
                string bufRez = param.ToString();
                if (bufRez != "")
                    buf += (buf != "" ? param.Logic.ToString() : "") + " " + bufRez + " " + "\r\n";
            }
            return buf;
        }

        public bool IsEmpty()
        {
            return parameters.Count == 0;
        }

        public IEnumerable<DBItem> Load(DBLoadParam param = DBLoadParam.Load)
        {
            return Table.LoadItems(this, param);
        }

        public IEnumerable<DBItem> Select()
        {
            return Table.SelectItems(this);
        }

        public bool Contains(string column)
        {
            return parameters.Select(QParam.ColumnNameInvoker.Instance, CompareType.Equal, column).Any();
        }

        public QParam GetByColumn(DBColumn column)
        {
            foreach (QParam p in parameters)
                if (p.IsColumn(column))
                    return p;
            return null;
        }

        public bool Contains(DBColumn column)
        {
            return GetByColumn(column) != null;
        }

        public void Remove(DBColumn column)
        {
            for (int i = 0; i < parameters.Count;)
            {
                QParam p = parameters[i];
                if (p.IsColumn(column))
                    parameters.Remove(p);
                else
                    i++;
            }
        }

        public override object GetValue(DBItem row = null)
        {
            return this;
        }

        public override void Dispose()
        {
            parameters?.Dispose();
            parameters = null;
            columns?.Dispose();
            columns = null;
            orders?.Dispose();
            orders = null;
            groups?.Dispose();
            groups = null;
            tables?.Dispose();
            tables = null;
        }

        public void BuildColumn(DBColumn dBColumn)
        {
            QColumn column = new QColumn(dBColumn);

            columns.Add(column);
        }

        public void Delete(QItem item)
        {
            throw new NotImplementedException();
        }
    }

    public enum QMathType
    {
        None,
        Plus,
        Minus,
        Devide,
        Multiply
    }

    [Flags]
    public enum QQueryBuildParam
    {
        None = 0,
        AutoLike = 1,
        SplitString = 2
    }

}
