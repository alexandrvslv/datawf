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
using System.ComponentModel;
using DataWF.Common;
using System.Text.RegularExpressions;
using System.Data;
using System.Text;
using System.Collections;
using System.Globalization;
using System.Linq;

namespace DataWF.Data
{
    public interface IQuery
    {
        string Format(IDbCommand command);
        DBTable Table { get; }
        QParamList Parameters { get; }
        QItemList<QItem> Columns { get; }
        QItemList<QOrder> Orders { get; }
    }

    public class QParamList : QItemList<QParam>
    {
        static readonly Invoker<QParam, QParam> groupInvoker = new Invoker<QParam, QParam>(nameof(QParam.Group), (item) => item.Group);

        public QParamList()
        {
        }

        public QParamList(IQItemList owner) : base(owner)
        {
            Indexes.Add(groupInvoker);
        }
    }

    public class QQuery : QItem, IQuery, IDisposable, IQItemList
    {
        public string CacheQuery;
        protected SelectableList<QParam> allParameters;
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
            parameters.ListChanged += OnParametersListChanged;
            columns = new QItemList<QItem>(this);
            orders = new QItemList<QOrder>(this);
            groups = new QItemList<QColumn>(this);
            order = 0;
        }

        public QQuery(Type type) : this()
        {
            var attribute = DBTable.GetTableAttribute(type, true);
            if (attribute != null)
                Table = attribute.Table;
        }

        public void Sort<T>(List<T> list)
        {
            var comparer = new DBComparerList();
            foreach (QOrder order in Orders)
                comparer.Comparers.Add(new DBComparer(Table, order.Column.Name, order.Direction));
            ListHelper.QuickSort(list, comparer);
        }

        public QQuery(string query, DBTable table = null, IEnumerable cols = null, QQuery bquery = null)
            : this()
        {
            baseQuery = bquery;
            if (bquery != null)
                order = bquery.order + 1;
            Table = table;
            Parse(query);
            if (cols != null)
                foreach (DBColumn col in cols)
                    BuildColumn(col);

        }
        public IQItemList Owner { get { return baseQuery ?? this; } }

        public override IQuery Query { get { return Owner as IQuery; } }

        public Type TypeFilter
        {
            get { return type; }
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
                        param.ValueRight = new QValue(typeIndex, Table.ItemTypeKey);
                    }
                }
            }
        }

        public DBStatus StatusFilter
        {
            get { return status; }
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
                            param.ValueRight = Table.GetStatusEnum(status);
                        }
                    }
                }
            }
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
                    if (column.ColumnType == DBColumnTypes.Default && column.Access.View && (column.Keys & DBColumnKeys.View) == DBColumnKeys.View || (column.Keys & DBColumnKeys.Code) == DBColumnKeys.Code)
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

        public DBColumn ParseColumn(string word)
        {
            if (tables.Count == 1)
            {
                DBColumn column = tables[0].Table.ParseColumn(word);
                if (column != null)
                    return column;
            }
            else if (tables.Count > 1)
            {
                string prefix = word.Substring(0, word.IndexOf('.'));
                foreach (var table in tables)
                    if (prefix.Equals(table.Text, StringComparison.OrdinalIgnoreCase) ||
                        prefix.Equals(table.Alias, StringComparison.OrdinalIgnoreCase))
                        return table.Table.ParseColumn(word);
            }
            var q = Query as QQuery;
            while (q != this)
            {
                var column = q.ParseColumn(word);
                if (column != null)
                {
                    refmode = true;
                    return column;
                }
                q = q.Query as QQuery;
            }
            return null;
        }
        //public QFunc ParseFunc(string query, )
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

            string prefix = null;
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
                        var tb = DBService.ParseTable(word);

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
                    prefix = word;
                else if (c == '\'' || c == ' ' || c == ',' || c == '(' || c == ')' || c == '\n' || c == '\r' || c == '!' || c == '=' || c == '>' || c == '<')//word.Length > 0 && 
                {
                    //if (c == ' ')
                    //    continue;

                    if (word.Equals("select", StringComparison.OrdinalIgnoreCase))
                    {
                        state = QParcerState.Select;
                    }
                    else if (word.Equals("from", StringComparison.OrdinalIgnoreCase))
                    {
                        prefix = null;
                        state = QParcerState.From;
                    }
                    else if (word.Equals("where", StringComparison.OrdinalIgnoreCase))
                    {
                        prefix = null;
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
                                                    column = new QColumn(scolumn) { Prefix = prefix };
                                                    columns.Add(column);
                                                    prefix = null;
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

                                    }
                                }
                                break;
                            case QParcerState.Where:
                                if (word.Length > 0)
                                {
                                    if (parameter == null || parameter.IsCompaund ||
                                        (parameter.ValueRight != null && parameter.Comparer.Type != CompareTypes.Between) ||
                                        (parameter.ValueRight is QBetween && ((QBetween)parameter.ValueRight).Max != null))
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
                                        parameter.SetValue(new QValue(val, column == null ? null : column.Column) { Text = word });
                                    }
                                    else if (word.Equals("true", StringComparison.OrdinalIgnoreCase))
                                    {
                                        parameter.SetValue(new QValue(true, column == null ? null : column.Column));
                                    }
                                    else if (word.Equals("false", StringComparison.OrdinalIgnoreCase))
                                    {
                                        parameter.SetValue(new QValue(false, column == null ? null : column.Column));
                                    }
                                    else if (word.Equals("null", StringComparison.OrdinalIgnoreCase))
                                    {
                                        parameter.SetValue(new QValue(DBNull.Value, column == null ? null : column.Column));
                                    }
                                    else if (word.Equals("not", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (parameter.Comparer.Type == CompareTypes.Is)
                                            parameter.Comparer = new CompareType(CompareTypes.Is, true);
                                        else if (parameter.Logic.Type != LogicTypes.Undefined && parameter.ValueLeft == null)
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
                                                    var between = new QBetween(parameter.ValueRight, null, column?.Column);
                                                    parameter.ValueRight = between;
                                                }
                                                else
                                                    parameter.Logic = new LogicType(lg);
                                            }
                                            else
                                            {
                                                var fn = QFunc.ParseFunction(word);
                                                if (fn != QFunctionType.none)
                                                {
                                                    func = new QFunc(fn);
                                                    parameter.SetValue(func);
                                                }
                                                else
                                                {
                                                    var wcolumn = ParseColumn(word);
                                                    if (wcolumn != null)
                                                    {
                                                        column = new QColumn(wcolumn) { Prefix = prefix };
                                                        prefix = null;
                                                        parameter.SetValue(column);
                                                    }
                                                    else
                                                    {
                                                        var invoker = EmitInvoker.Initialize(typeof(DBItem), word);
                                                        if (invoker != null)
                                                        {
                                                            QReflection reflection = new QReflection(invoker);
                                                            parameter.SetValue(reflection);
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
                                        parameter.SetValue(new QValue(ParseString(query, ref i, '\'', '\''), column != null ? column.Column : null));
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
                                            QEnum list = new QEnum();
                                            string[] split = word2.Split(',');
                                            foreach (var s in split)
                                                list.Items.Add(new QValue(s.Trim(' ', '\''), column != null ? column.Column : null));
                                            parameter.SetValue(list);
                                            if (parameter.Comparer.Type == CompareTypes.Between && parameter.ValueRight is QEnum)
                                            {
                                                var qEnum = (QEnum)parameter.ValueRight;
                                                var between = new QBetween(qEnum.Items[0], qEnum.Items[1], column?.Column);
                                                parameter.ValueRight = between;
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
                                var cl = ParseColumn(word);

                                if (cl != null)
                                {
                                    order = new QOrder(cl) { Prefix = prefix };
                                    orders.Add(order);
                                    prefix = null;
                                }
                                else if (word.Equals("asc", StringComparison.OrdinalIgnoreCase))
                                    order.Direction = ListSortDirection.Ascending;
                                else if (word.Equals("desc", StringComparison.OrdinalIgnoreCase))
                                    order.Direction = ListSortDirection.Descending;
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

        public override string ToString()
        {
            string rez = "Query";
            if (Table != null)
                rez = Table.ToString();
            return rez;
        }

        public static QParam CreateParam(DBColumn column, object value)
        {
            return CreateParam(LogicType.And, column, CompareType.Equal, value);
        }

        public static QParam CreateParam(DBColumn column, CompareType comp, object value)
        {
            return CreateParam(LogicType.And, column, comp, value);
        }

        public static QParam CreateParam(LogicType logic, DBColumn column, CompareType compare, object value)
        {
            QParam param = new QParam
            {
                Logic = logic,
                ValueLeft = new QColumn(column),
                Comparer = compare,
                Value = value
            };
            return param;
        }

        public QParam BuildParam(DBColumn col, object val)
        {
            return BuildParam(col, val, true);
        }

        public QParam BuildNameParam(string property, CompareType comparer, object value)
        {
            var param = new QParam();
            foreach (var item in Table.Columns.Select(nameof(DBColumn.Property), CompareType.Equal, property))
            {
                param.Parameters.Add(QQuery.CreateParam(LogicType.Or, item, comparer, value));
            }
            return param;
        }

        public QParam BuildPropertyParam(string property, CompareType comparer, object value)
        {
            return BuildParam(Table.ParseProperty(property), comparer, value);
        }

        public QParam BuildParam(string column, object value, bool autoLike)
        {
            int index = column.IndexOf('.');
            if (index >= 0)
            {
                string[] split = column.Split(DBService.DotSplit);//TODO replace with substring
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
                            param.ValueRight = q;
                            q.Columns.Add(new QColumn(dbColumn.ReferenceTable.PrimaryKey.Name));
                        }
                }
                q.BuildParam(dbColumn, value, autoLike);
                param = parameters[parameters.Count - 1];
                if (parameters.Count > 1)
                    param.Logic = LogicType.And;
                return param;
            }
            else
                return BuildParam(Table.Columns[column], value, autoLike);

        }

        public QParam BuildParam(DBColumn column, object value, bool autoLike)
        {
            CompareType comparer = CompareType.Equal;
            if (value is QQuery)
                comparer = CompareType.In;
            else if (value is DBItem)
            {
                if (value is DBGroupItem)
                {
                    value = ((DBGroupItem)value).GetSubGroupFull<DBGroupItem>(true);
                    comparer = CompareType.In;
                }
                else
                    value = ((DBItem)value).PrimaryId;
            }
            else if ((column.Keys & DBColumnKeys.Boolean) == DBColumnKeys.Boolean)
                comparer = CompareType.Equal;
            else if (column.DataType == typeof(string))
                comparer = CompareType.Like;
            else if (value is DateInterval)// && !((DateInterval)val).IsEqual())
                comparer = CompareType.Between;
            else if (column.DataType == typeof(DateTime))
                comparer = CompareType.Equal;
            return BuildParam(column, comparer, value, autoLike);
        }

        public QParam BuildParam(DBColumn column, CompareType comparer, object value, bool autoLike = false)
        {
            if (column == null)
                return null;

            //  foreach (QParam p in parameters)
            // if (p.Column == col && p.Comparer == comp && p.Value == val)
            //return p;

            QParam param = null;

            if (column.DataType == typeof(string) && (comparer.Type == CompareTypes.Like || comparer.Type == CompareTypes.Equal))
            {
                if (value == null)
                    return null;
                string like = autoLike ? "%" : "";
                string[] split = value.ToString().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (split.Length == 1)
                    param = CreateParam(column, comparer, like + split[0].Trim() + like);
                else
                {
                    param = parameters.Add();
                    int i = 0;
                    foreach (string s in split)
                    {
                        if (s.Trim().Length == 0)
                            continue;

                        param.Parameters.Add(CreateParam(i == 0 ? LogicType.And : LogicType.Or, column, comparer, like + s.Trim() + like));
                        i++;
                    }
                }
            }
            else
            {
                param = CreateParam(column, comparer, value);
            }

            if (param != null && param.Query == null)
                Parameters.Add(param);

            return param;
        }

        public QParam BuildParam(DBColumn parent, DBColumn column, object p, bool p_4)
        {
            var query = new QQuery("", column.Table);
            query.Columns.Add(new QColumn(column.Table.PrimaryKey.Name));
            query.Parameters.Remove(column);
            query.BuildParam(column, p, p_4);
            QParam param = CreateParam(parent, CompareType.In, query);
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

        public override DBTable Table
        {
            get { return tables.FirstOrDefault()?.Table; }
            set
            {
                if (value != Table)
                {
                    if (value != null)
                        tables.Add(new QTable(value));
                    else
                        tables.Clear();
                }
            }
        }
        public QItemList<QTable> Tables
        {
            get { return tables; }
        }

        public QItemList<QItem> Columns
        {
            get { return columns; }
        }

        public QItemList<QOrder> Orders
        {
            get { return orders; }
        }

        public QParamList Parameters
        {
            get { return parameters; }
        }

        public SelectableList<QParam> AllParameters
        {
            get
            {
                if (allParameters == null)
                    OnParametersListChanged(this, new ListChangedEventArgs(ListChangedType.Reset, -1));
                return allParameters;
            }
        }

        //protected List<QParam> GetDownParam(QParam prm)
        //{
        //    List<QParam> prms = new List<QParam>();
        //    if (!(prm.Value is Query))
        //    {
        //        prms.Add(prm);
        //        return prms;
        //    }
        //    ((Query)prm.Value).parameters.ListChanged -= handleChild;
        //    ((Query)prm.Value).parameters.ListChanged += handleChild;
        //    prms.Add(prm);
        //    foreach (QParam p in ((Query)prm.Value).parameters)
        //        prms.AddRange(GetDownParam(p).ToArray());
        //    return prms;
        //}

        private void AddAllParam(QParam param)
        {
            if (param.IsCompaund)
                foreach (var p in param.Parameters)
                    AddAllParam(p);
            if (!allParameters.Contains(param))
                allParameters.AddInternal(param);

            if (param.ValueRight is QQuery)
                foreach (QParam pp in ((QQuery)param.ValueRight).Parameters)
                {
                    AddAllParam(pp);
                }
        }

        public void OnParametersListChanged(object sender, ListChangedEventArgs e)
        {
            //var pe = (ListPropertyChangedEventArgs)e;
            //if (e.ListChangedType == ListChangedType.ItemAdded)
            //{
            //    var param = ((IList)sender)[e.NewIndex] as QParam;
            //    if (!allParameters.Contains(param))
            //        allParameters.Add(param);
            //}
            if (allParameters == null)
                allParameters = new SelectableList<QParam>();
            if (e.ListChangedType == ListChangedType.Reset || e.ListChangedType == ListChangedType.ItemDeleted)
            {
                refmode = false;
                allParameters.ClearInternal();
            }

            foreach (QParam p in parameters)
                AddAllParam(p);

            allParameters.OnListChanged(ListChangedType.Reset, -1);
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
                var table = Table;
                foreach (var col in table.Columns)
                {
                    string temp = table.FormatQColumn(col);
                    if (temp != null && temp.Length > 0)
                    {
                        if (!table.Columns.IsFirst(col))
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
                order.Append(col.Format(command));
                if (!orders.IsLast(col))
                    order.Append(", ");
            }

            foreach (QTable table in tables)
            {
                from.Append(table.Format(command));
                if (!tables.IsLast(table))
                    from.Append(", ");
            }
            return $"select {cols.ToString()}\n    from {from} {(whr.Length > 0 ? "\n    where " : string.Empty)}{whr}{(order.Length > 0 ? "\n    order by " : string.Empty)}{order}";
        }

        public string ToWhere(IDbCommand command = null)
        {
            var buf = new StringBuilder();
            //parameters._ApplySort("Order");
            if (Table is IDBVirtualTable && command != null)
            {
                if (parameters.Count > 0)
                    buf.Append("(");
                foreach (QParam param in ((IDBVirtualTable)Table).FilterQuery.parameters)
                {
                    string bufRez = param.Format(command);
                    if (bufRez.Length > 0)
                        buf.Append((buf.Length <= 1 ? "" : param.Logic.Format() + " ") + bufRez + " ");
                }
                if (parameters.Count > 0)
                    buf.Append(") and (");
            }
            for (int i = 0; i < parameters.Count; i++)
            {
                QParam param = parameters[i];
                string bufRez = param.Format(command);
                if (bufRez.Length > 0)
                    buf.Append((buf.Length == 0 || i == 0 ? "" : param.Logic.Format() + " ") + bufRez + " ");
            }
            if (Table is IDBVirtualTable && command != null && parameters.Count > 0)
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

        public IEnumerable Load(DBLoadParam param = DBLoadParam.Load)
        {
            return Table.LoadItems(this, param);
        }

        public IEnumerable Select()
        {
            return Table.SelectItems(this);
        }

        public bool Contains(string column)
        {
            return columns.Select("Value1.Text", CompareType.Equal, column).Any();
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
            if (allParameters != null)
                allParameters.Dispose();
            allParameters = null;

            parameters.Dispose();
            parameters = null;
            columns.Dispose();
            columns = null;
            orders.Dispose();
            orders = null;
            groups.Dispose();
            groups = null;
            tables.Dispose();
            tables = null;
        }

        public bool IsRefence
        {
            get { return refmode; }
            set { refmode = value; }
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

}
