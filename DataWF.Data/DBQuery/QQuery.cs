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
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DataWF.Data
{

    public class QQuery<T> : QItem, IQQuery<T>, IDisposable where T : DBItem
    {
        private const string StrFrom = "from";
        private const string StrOn = "on";
        private const string StrSelect = "select";
        private const string StrWhere = "where";
        private const string StrOrder = "order";
        private const string StrGroup = "group";
        private const string StrBy = "by";
        private const string StrAs = "as";
        private const string StrTrue = "true";
        private const string StrFalse = "false";
        private const string strNull = "null";
        private const string strNot = "not";
        private const string StrAsc = "asc";
        private const string StrDesc = "desc";
        private const string StrIGroup = nameof(IGroup);

        protected QParamList parameters;
        protected QItemList<QItem> columns;
        protected QItemList<QOrder> orders;
        protected QItemList<QColumn> groups;
        protected QTableList tables;
        private IQQuery baseQuery;
        private DBStatus status = DBStatus.Empty;
        private IDBSchema schema;
        private Type type;
        private string queryText;
        private string whereText;

        //internal QQuery()
        //{ }

        public QQuery(IDBSchema schema) : base()
        {
            Schema = schema;
            order = 0;
        }

        public QQuery(IDBTable table, string queryText = null) : this(table.Schema)
        {
            Table = (DBTable)table;
            Parse(queryText);
        }

        public QQuery(IDBTable table, Expression<Func<T, bool>> expression) : this(table.Schema)
        {
            Table = (DBTable)table;
            Where(expression);
        }

        public QQuery(IDBSchema schema, string queryText, IDBTable table = null)
            : this(schema)
        {
            Table = (DBTable)table;
            Parse(queryText);
        }

        public QQuery(IDBSchema schema, ReadOnlySpan<char> queryText, IDBTable table = null)
            : this(schema)
        {
            Table = (DBTable)table;
            Parse(queryText);
        }

        public QQuery(IQQuery query, string queryText)
            : this(query.Schema)
        {
            BaseQuery = query;
            Parse(queryText);
        }

        public QQuery(IQQuery query, ReadOnlySpan<char> queryText)
           : this(query.Schema)
        {
            BaseQuery = query;
            Parse(queryText);
        }

        ~QQuery()
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

        public DBLoadParam LoadParam { get; set; }

        public override IDBSchema Schema
        {
            get => schema ??= Table.Schema;
            set => schema = value;
        }

        public override QTable QTable
        {
            get => qTable ??= Enumerable.FirstOrDefault(Tables);
            set => Tables.Add(value);
        }

        public DBTable<T> TTable => (DBTable<T>)Table;

        public override IDBTable Table
        {
            get => QTable?.Table;
            set
            {
                if (value != Table)
                {
                    if (value != null)
                        Tables.Add(new QTable(value, GenerateTableAlias(value)));
                    else
                        Tables.Clear();
                }
            }
        }

        public QTableList Tables => tables ??= new QTableList(this);

        public QItemList<QItem> Columns => columns ??= new QItemList<QItem>(this);

        public QItemList<QOrder> Orders => orders ??= new QItemList<QOrder>(this);

        public QParamList Parameters => parameters ??= new QParamList(this);

        public QItemList<QColumn> Groups => groups ??= new QItemList<QColumn>(this);

        public IQQuery BaseQuery
        {
            get => baseQuery ?? Container?.Query;
            set
            {
                if (baseQuery != value)
                {
                    baseQuery = value;
                    if (value != null)
                    {
                        order = value.Order + 1;
                    }
                }
            }
        }

        [JsonIgnore]
        public IQItem Owner => Container?.Owner ?? this;

        [JsonIgnore]
        public override IQQuery Query => BaseQuery ?? this;

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
                    param = CreateTypeParam(type);
                    if (param != null)
                    {
                        param.IsDefault = true;
                        Parameters.Insert(0, param);
                    }
                }
                else
                {
                    var typeIndex = Table.GetTypeIndex(type);
                    if (typeIndex <= 0)
                    {
                        Parameters.Remove(param);
                    }
                    else
                    {
                        param.RightItem = new QValue(typeIndex, Table.ItemTypeKey);
                    }
                }
            }
        }

        public QParam CreateTypeParam(Type type)
        {
            var typeIndex = Table.GetTypeIndex(type);
            if (Table.ItemTypeKey != null && typeIndex > 0)
            {
                return CreateParam(LogicType.And, Table.ItemTypeKey, CompareType.Equal, typeIndex);
            }
            return null;
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
                        param = GetStatusParam(status);
                        param.IsDefault = true;
                        if (param != null)
                        {
                            Parameters.Insert(0, param);
                        }
                    }
                    else
                    {
                        if (status == DBStatus.Empty)
                        {
                            Parameters.Remove(param);
                        }
                        else
                        {
                            param.RightItem = GetStatusEnum(status);
                        }
                    }
                }
            }
        }

        public DBCacheState CacheState { get; set; }

        public string QueryText
        {
            get => queryText ??= FormatAll(null, true);
            set => queryText = value;
        }

        public string WhereText
        {
            get => whereText ??= FormatWhere(null).ToString();
            set => whereText = value;
        }

        public ITreeComparer TreeComparer { get; set; }

        protected QArray GetStatusEnum(DBStatus status)
        {
            var qlist = new QArray();
            if ((status & DBStatus.Actual) == DBStatus.Actual)
                qlist.Items.Add(new QValue((int)DBStatus.Actual));
            if ((status & DBStatus.New) == DBStatus.New)
                qlist.Items.Add(new QValue((int)DBStatus.New));
            if ((status & DBStatus.Edit) == DBStatus.Edit)
                qlist.Items.Add(new QValue((int)DBStatus.Edit));
            if ((status & DBStatus.Delete) == DBStatus.Delete)
                qlist.Items.Add(new QValue((int)DBStatus.Delete));
            if ((status & DBStatus.Archive) == DBStatus.Archive)
                qlist.Items.Add(new QValue((int)DBStatus.Archive));
            if ((status & DBStatus.Error) == DBStatus.Error)
                qlist.Items.Add(new QValue((int)DBStatus.Error));

            return qlist;
        }

        protected QParam GetStatusParam(DBStatus status)
        {
            if (Table.StatusKey != null && status != 0 && status != DBStatus.Empty)
            {
                return new QParam()
                {
                    LeftItem = new QColumn(Table.StatusKey),
                    Comparer = CompareType.In,
                    RightItem = GetStatusEnum(status)
                };
            }
            return null;
        }

        public QQuery<T> Where(Expression<Func<T, bool>> expression)
        {
            Parameters.Add(CreateParam(expression.Body, LogicType.Undefined));
            return this;
        }

        public QQuery<T> Where(string filter)
        {
            if (Table == null)
                throw new InvalidOperationException();
            //Parameters.Clear();
            var parameter = new QParam() { IsCompaund = true };
            Parameters.Add(parameter);
            ParseParameters(filter.AsSpan(), parameter);

            return this;
        }

        public QQuery<T> WhereViewColumns(string filter, QBuildParam param = QBuildParam.AutoLike)
        {
            if (Table == null)
                throw new InvalidOperationException();
            //Parameters.Clear();
            Where(p => p.Parameters.AddRange(WhereViewColumns(filter, Table, param)));
            return this;
        }

        private IEnumerable<QParam> WhereViewColumns(string filter, IDBTable table, QBuildParam param = QBuildParam.AutoLike)
        {
            foreach (DBColumn column in table.Columns)
            {
                if (column.ColumnType == DBColumnTypes.Default
                && (column.Keys & DBColumnKeys.View) == DBColumnKeys.View
                || (column.Keys & DBColumnKeys.Code) == DBColumnKeys.Code)
                {
                    if (column.IsReference && column.ReferenceTable != Table)
                    {
                        Join(column);
                        foreach (var qParam in WhereViewColumns(filter, column.ReferenceTable, param))
                            yield return qParam;
                    }
                    else
                    {
                        yield return CreateParam(LogicType.Or, column, filter, param);
                    }
                }
            }
        }

        protected string ParceStringConstant(string[] split, ref int i)
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

        public DBTable ParseTable(string word)
        {
            return Schema.ParseTable(word) ?? Schema.Tables.GetByTypeName(word);
        }

        public DBColumn ParseColumn(string word, string prefix, out QTable qTable)
        {
            qTable = null;
            prefix = prefix == null && word.IndexOf('.') > -1 ? word.Substring(0, word.IndexOf('.')) : prefix;
            if (prefix == null)
            {
                foreach (var table in Tables)
                {
                    var column = table.Table.GetColumnOrProperty(word);
                    if (column != null)
                    {
                        qTable = table;
                        return column;
                    }
                }
            }
            else
            {
                foreach (var table in Tables)
                {
                    if (prefix.Equals(table.Table.Name, StringComparison.OrdinalIgnoreCase)
                    || prefix.Equals(table.TableAlias, StringComparison.OrdinalIgnoreCase)
                    || prefix.Equals(table.Table.ItemTypeName, StringComparison.OrdinalIgnoreCase))
                    {
                        var column = table.Table.GetColumnOrProperty(word);
                        if (column != null)
                        {
                            qTable = table;
                            return column;
                        }
                    }
                }
            }
            var q = BaseQuery;
            while (q != null)
            {
                var column = q.ParseColumn(word, prefix, out qTable);
                if (column != null)
                {
                    IsReference = true;
                    return column;
                }
                q = q.BaseQuery;
            }
            return null;
        }

        public IQQuery GetTopQuery()
        {
            var q = (IQQuery)this;
            while (q.BaseQuery != null)
            {
                q = q.BaseQuery;
            }
            return q;
        }

        protected int GetSubQueryLevel()
        {
            int i = 0;
            var q = BaseQuery;
            while (q != null)
            {
                i++;
                q = q.BaseQuery;
            }
            return i;
        }

        protected int FindFrom(ReadOnlySpan<char> query)
        {
            int exit = 0;
            int startIndex = 0;
            var word = ReadOnlySpan<char>.Empty;

            for (int i = 0; i <= query.Length; i++)
            {
                var c = i < query.Length ? query[i] : '\n';
                switch (c)
                {
                    case ' ':
                    case '\n':
                    case '\r':
                        if (exit <= 0
                            && MemoryExtensions.Equals(word, StrFrom.AsSpan(), StringComparison.OrdinalIgnoreCase))
                            return i;
                        word = ReadOnlySpan<char>.Empty;
                        startIndex = i + 1;
                        break;
                    case '(':
                        exit++;
                        break;
                    case ')':
                        exit--;
                        break;
                    default:
                        word = query.Slice(startIndex, (i - startIndex) + 1);
                        break;
                }
            }
            return query.Length;
        }

        public void Parse(string query)
        {
            QueryText = query;
            WhereText = query;
            if (query == null)
                return;

            Parse(query.AsSpan());
        }

        public void Parse(ReadOnlySpan<char> query)
        {
            parameters?.Clear();
            columns?.Clear();
            orders?.Clear();
            groups?.Clear();
            if (query.Length == 0)
                return;
            bool alias = false;

            ParseFrom(query);

            QParserState state = QParserState.Where;
            QParam parameter = null;
            QOrder order = null;
            var startIndex = 0;
            var prefix = new List<string>();
            var word = ReadOnlySpan<char>.Empty;

            for (int i = 0; i <= query.Length; i++)
            {
                var c = i < query.Length ? query[i] : '\n';
                switch (c)
                {
                    case '.':
                        prefix.Add(word.ToString());
                        word = ReadOnlySpan<char>.Empty;
                        startIndex = i + 1;
                        break;
                    case '\'':
                    case '"':
                    case ' ':
                    case ',':
                    case '(':
                    case ')':
                    case '\n':
                    case '\r':
                    case '!':
                    case '=':
                    case '>':
                    case '<':
                        if (MemoryExtensions.Equals(word, StrSelect.AsSpan(), StringComparison.OrdinalIgnoreCase))
                        {
                            state = QParserState.Select;
                        }
                        else if (MemoryExtensions.Equals(word, StrFrom.AsSpan(), StringComparison.OrdinalIgnoreCase))
                        {
                            prefix.Clear();
                            state = QParserState.From;
                        }
                        else if (MemoryExtensions.Equals(word, StrWhere.AsSpan(), StringComparison.OrdinalIgnoreCase))
                        {
                            prefix.Clear();
                            state = QParserState.Where;
                        }
                        else if (MemoryExtensions.Equals(word, StrOrder.AsSpan(), StringComparison.OrdinalIgnoreCase))
                        {
                            state = QParserState.OrderBy;
                        }
                        else if (MemoryExtensions.Equals(word, StrGroup.AsSpan(), StringComparison.OrdinalIgnoreCase))
                        {
                            state = QParserState.GroupBy;
                        }
                        else if (MemoryExtensions.Equals(word, StrBy.AsSpan(), StringComparison.OrdinalIgnoreCase))
                        {

                        }
                        else
                        {
                            switch (state)
                            {
                                case QParserState.Select:
                                    if (word.Length > 0)
                                    {
                                        if (MemoryExtensions.Equals(word, StrAs.AsSpan(), StringComparison.OrdinalIgnoreCase) && c == ' ')
                                        {
                                            alias = true;
                                        }
                                        else if (MemoryExtensions.Equals(word, strNull.AsSpan(), StringComparison.OrdinalIgnoreCase))
                                        {
                                            Columns.Add(new QValue(null));
                                        }
                                        else
                                        {
                                            var wordStr = word.ToString();
                                            if (alias)
                                            {
                                                SetColumAlias(wordStr);
                                                alias = false;
                                            }
                                            else
                                            {
                                                var fn = QFunction.ParseFunction(wordStr);
                                                if (fn != QFunctionType.none)
                                                {
                                                    Columns.Add(new QFunction(fn));
                                                }
                                                else
                                                {
                                                    var dbColumn = ParseColumn(wordStr, prefix.LastOrDefault(), out var qTable);
                                                    if (dbColumn != null)
                                                    {
                                                        Columns.Add(new QColumn(dbColumn)
                                                        {
                                                            QTable = qTable
                                                        });
                                                        prefix.Clear();
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    if (c == '(')
                                    {
                                        var word2 = query.GetSubPart(ref i, '(', ')').Trim();
                                        if (Columns.LastOrDefault() is QFunction qFunc)
                                        {
                                            ParseFunction(qFunc, word2);
                                        }
                                        else if (MemoryExtensions.StartsWith(word2, StrSelect.AsSpan(), StringComparison.OrdinalIgnoreCase))
                                        {
                                            Columns.Add(new QQuery<DBItem>(this, word2));
                                        }
                                        else
                                        {
                                            var expression = new QExpression();
                                            ParseExpression(expression, word2);
                                            Columns.Add(expression);
                                        }
                                    }
                                    else if (c == '\'' || c == '"')
                                    {
                                        var wordStr = query.GetSubPart(ref i, c, c).ToString();
                                        if (alias)
                                        {
                                            SetColumAlias(wordStr);
                                            alias = false;
                                        }
                                        else
                                        {
                                            Columns.Add(new QValue(wordStr, null));
                                        }
                                    }
                                    break;
                                case QParserState.From:
                                    {
                                        if (word.Length > 0)
                                        {
                                            //Table = ParseTable(word);
                                        }
                                    }
                                    break;
                                case QParserState.Where:
                                    ParseParameter(query, ref parameter, word, ref i, c, prefix);
                                    if (parameter != null && parameter.Container == null)
                                        Parameters.Add(parameter);
                                    break;
                                case QParserState.OrderBy:
                                    if (word.Length > 0)
                                    {

                                        if (MemoryExtensions.Equals(word, StrAsc.AsSpan(), StringComparison.OrdinalIgnoreCase))
                                        {
                                            if (order != null)
                                            {
                                                order.Direction = ListSortDirection.Ascending;
                                                order = null;
                                            }
                                        }
                                        else if (MemoryExtensions.Equals(word, StrDesc.AsSpan(), StringComparison.OrdinalIgnoreCase))
                                        {
                                            if (order != null)
                                            {
                                                order.Direction = ListSortDirection.Descending;
                                                order = null;
                                            }
                                        }
                                        else if (MemoryExtensions.Equals(word, StrIGroup.AsSpan(), StringComparison.OrdinalIgnoreCase))
                                        {
                                            TreeComparer = GroupHelper.GetTreeInvoker(typeof(T)).CreateTreeComparer(typeof(T));
                                        }
                                        else
                                        {
                                            var wordStr = word.ToString();
                                            var dbColumn = ParseColumn(wordStr, prefix.LastOrDefault(), out var qTable);
                                            if (dbColumn != null)
                                            {
                                                order = new QOrder
                                                {
                                                    Item = new QColumn(dbColumn)
                                                    {
                                                        TableAlias = prefix.FirstOrDefault(),
                                                        QTable = qTable
                                                    }
                                                };
                                                Orders.Add(order);
                                                prefix.Clear();
                                            }
                                            else if (prefix.Count > 0)
                                            {
                                                var property = $"{string.Join(".", prefix)}.{wordStr}";
                                                prefix.Clear();
                                                var invoker = EmitInvoker.Initialize(Table.ItemType, property);
                                                if (invoker != null)
                                                {
                                                    order = new QOrder
                                                    {
                                                        Item = new QInvoker(invoker) { QTable = QTable }
                                                    };
                                                    Orders.Add(order);
                                                    prefix.Clear();
                                                }
                                            }
                                            else
                                            {
                                                var invoker = EmitInvoker.Initialize(Table.ItemType, wordStr);
                                                if (invoker != null)
                                                {
                                                    order = new QOrder
                                                    {
                                                        Item = new QInvoker(invoker)
                                                    };
                                                    Orders.Add(order);
                                                    prefix.Clear();
                                                }
                                            }
                                        }
                                    }
                                    break;
                            }
                        }
                        word = ReadOnlySpan<char>.Empty;
                        startIndex = i + 1;
                        break;
                    default:
                        word = query.Slice(startIndex, (i - startIndex) + 1);
                        break;
                }
            }
        }

        private void ParseParameter(ReadOnlySpan<char> query, ref QParam parameter, ReadOnlySpan<char> word, ref int i, char c, List<string> prefix)
        {
            if (word.Length > 0)
            {
                if (parameter == null || parameter.IsCompaund || parameter.IsFilled)
                {
                    var tempParameter = parameter;
                    parameter = new QParam();
                    if (tempParameter?.IsCompaund ?? false)
                        tempParameter.Parameters.Add(parameter);
                    else if (tempParameter?.Group?.IsCompaund ?? false)
                        tempParameter.Group.Parameters.Add(parameter);
                }

                if (Helper.IsDecimal(word, out var decimalValue))
                {
                    parameter.Add(new QValue(word.ToString(), parameter.LeftColumn));
                }
                else if (MemoryExtensions.Equals(word, StrTrue.AsSpan(), StringComparison.OrdinalIgnoreCase))
                {
                    parameter.Add(new QValue(true, parameter.LeftColumn));
                }
                else if (MemoryExtensions.Equals(word, StrFalse.AsSpan(), StringComparison.OrdinalIgnoreCase))
                {
                    parameter.Add(new QValue(false, parameter.LeftColumn));
                }
                else if (MemoryExtensions.Equals(word, strNull.AsSpan(), StringComparison.OrdinalIgnoreCase))
                {
                    parameter.Add(new QValue(DBNull.Value, parameter.LeftColumn));
                }
                else if (MemoryExtensions.Equals(word, strNot.AsSpan(), StringComparison.OrdinalIgnoreCase))
                {
                    if (parameter.Comparer.Type == CompareTypes.Is)
                        parameter.Comparer = new CompareType(CompareTypes.Is, true);
                    else if (parameter.Logic.Type != LogicTypes.Undefined && parameter.LeftItem == null)
                        parameter.Logic = new LogicType(parameter.Logic.Type, true);
                    else
                        parameter.IsNotExpression = true;
                }
                else
                {
                    var cm = CompareType.Parse(word);
                    if (cm != CompareTypes.Undefined)
                    {
                        parameter.Comparer = new CompareType(cm, parameter.IsNotExpression);
                    }
                    else
                    {
                        var lg = LogicType.Parse(word);
                        if (lg != LogicTypes.Undefined)
                        {
                            if (parameter.Comparer.Type == CompareTypes.Between && lg == LogicTypes.And)
                            {
                                parameter.RightItem = new QBetween(parameter.RightItem, null, parameter.LeftColumn);
                            }
                            else
                            {
                                parameter.Logic = new LogicType(lg);
                            }
                            prefix.Clear();
                        }
                        else
                        {
                            var fn = QFunction.ParseFunction(word);
                            if (fn != QFunctionType.none)
                            {
                                parameter.Add(new QFunction(fn));
                                prefix.Clear();
                            }
                            else
                            {
                                var wordStr = word.ToString();
                                var wcolumn = ParseColumn(wordStr, prefix.LastOrDefault(), out var qTable);
                                if (wcolumn != null)
                                {
                                    parameter.Add(new QColumn(wcolumn)
                                    {
                                        QTable = qTable
                                    });
                                    prefix.Clear();
                                }
                                else
                                {
                                    if (parameter.LeftItem == null)
                                    {
                                        if (prefix.Count > 0)
                                        {
                                            var pTable = QTable;
                                            foreach (var pcolumn in prefix)
                                            {
                                                var dbColumn = pTable.Table.GetColumnOrProperty(pcolumn);

                                                if (dbColumn != null && dbColumn.IsReference)
                                                {
                                                    pTable = CreateJoin(dbColumn);
                                                    if (!Tables.Contains(pTable))
                                                        Tables.Add(pTable);
                                                }
                                                else
                                                {
                                                    var referencing = pTable.Table.GetReferencing(pcolumn);
                                                    if (referencing != null)
                                                    {
                                                        pTable = CreateJoin(referencing.ReferenceColumn, pTable.Table.PrimaryKey);
                                                        if (!Tables.Contains(pTable))
                                                            Tables.Add(pTable);
                                                    }
                                                }
                                            }
                                            var lastColumn = pTable.Table.GetColumnOrProperty(wordStr);
                                            parameter.Add(new QColumn(lastColumn)
                                            {
                                                QTable = pTable
                                            });
                                            prefix.Clear();
                                        }
                                        else
                                        {
                                            var invoker = EmitInvoker.Initialize(Table.ItemType, wordStr);
                                            if (invoker != null)
                                            {
                                                parameter.Add(new QInvoker(invoker));
                                                prefix.Clear();
                                            }
                                        }
                                    }
                                    else// if (parameter.Column != null)
                                    {
                                        parameter.Add(new QValue(wordStr, parameter.LeftColumn));
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
                case '"':
                    parameter.Add(new QValue(query.GetSubPart(ref i, c, c).ToString(), parameter.LeftColumn));
                    break;
                case '(':
                    int tempIndex = i;
                    var word2 = query.GetSubPart(ref i, '(', ')').Trim();
                    if (MemoryExtensions.StartsWith(word2, StrSelect.AsSpan(), StringComparison.OrdinalIgnoreCase))
                    {
                        parameter.Add(new QQuery<DBItem>(this, word2));
                    }
                    else if (parameter != null && parameter.Parameters.LastOrDefault() is QFunction qFunc)
                    {
                        ParseFunction(qFunc, word2);
                    }
                    else if (parameter != null && parameter.Comparer.Type != CompareTypes.Undefined)
                    {
                        var list = new QArray();
                        foreach (var s in word2.Split(Helper.CommaSeparator))
                        {
                            var entry = s.Trim(' ', '\'');
                            if (string.Equals(entry, strNull, StringComparison.Ordinal))
                                continue;
                            list.Items.Add(new QValue(, parameter.LeftColumn));
                        }

                        parameter.Add(list);
                        if (parameter.Comparer.Type == CompareTypes.Between && parameter.RightItem is QArray)
                        {
                            var qEnum = (QArray)parameter.RightItem;
                            var between = new QBetween(qEnum.Items[0], qEnum.Items[1], parameter.LeftColumn);
                            parameter.RightItem = between;
                        }
                    }
                    else
                    {
                        i = tempIndex;
                        if (parameter != null
                            && parameter.LeftItem == null
                            && parameter.Logic != LogicType.Undefined)
                        {
                            parameter.IsCompaund = true;
                            break;
                        }
                        var tempParameter = parameter;
                        parameter = new QParam() { IsCompaund = true };
                        if (tempParameter?.IsCompaund ?? false)
                            tempParameter.Add(parameter);
                        if (tempParameter?.Group?.IsCompaund ?? false)
                            tempParameter.Group.Add(parameter);
                        //ParseParameters(word2, parameter);
                    }
                    break;
                case ')':
                    parameter = parameter?.Group?.Group;
                    break;
            }
        }

        private void ParseParameters(ReadOnlySpan<char> query, QParam parameter)
        {
            var prefix = new List<string>();
            var word = ReadOnlySpan<char>.Empty;
            int startIndex = 0;
            for (int i = 0; i <= query.Length; i++)
            {
                var c = i < query.Length ? query[i] : '\n';
                switch (c)
                {
                    case '.':
                        prefix.Add(word.ToString());
                        word = ReadOnlySpan<char>.Empty;
                        startIndex = i + 1;
                        break;
                    case '\'':
                    case '"':
                    case ' ':
                    case ',':
                    case '(':
                    case ')':
                    case '\n':
                    case '\r':
                    case '!':
                    case '=':
                    case '>':
                    case '<':
                        ParseParameter(query, ref parameter, word, ref i, c, prefix);
                        word = ReadOnlySpan<char>.Empty;
                        startIndex = i + 1;
                        break;
                    default:
                        word = query.Slice(startIndex, (i - startIndex) + 1);
                        break;
                }
            }
        }

        private void ParseFrom(ReadOnlySpan<char> query)
        {
            QTable table = null;
            var prefix = new List<string>();
            var word = ReadOnlySpan<char>.Empty;
            QParam parameter = null;
            int startIndex = 0;
            var joinType = JoinTypes.Undefined;

            for (int i = FindFrom(query); i <= query.Length; i++)
            {
                var c = i < query.Length ? query[i] : '\n';
                switch (c)
                {
                    case '.':
                        prefix.Add(word.ToString());
                        word = ReadOnlySpan<char>.Empty;
                        startIndex = i + 1;
                        break;
                    case '\'':
                    case '"':
                    case ' ':
                    case ',':
                    case '(':
                    case ')':
                    case '\n':
                    case '\r':
                    case '!':
                    case '=':
                    case '>':
                    case '<':
                        if (MemoryExtensions.Equals(word, StrWhere.AsSpan(), StringComparison.OrdinalIgnoreCase))
                            return;
                        var join = JoinType.Parse(word);
                        if (join != JoinTypes.Undefined)
                        {
                            table = null;
                            parameter = null;
                            joinType |= join;
                        }
                        else if (parameter != null)
                        {
                            ParseParameter(query, ref parameter, word, ref i, c, prefix);
                            if (parameter.RightItem != null)
                                parameter = null;
                        }
                        else if (MemoryExtensions.Equals(word, StrOn.AsSpan(), StringComparison.OrdinalIgnoreCase))
                        {
                            table.On = parameter = new QParam();
                        }
                        else if (word.Length > 0)
                        {
                            var wordStr = word.ToString();
                            if (table != null)
                            {
                                table.TableAlias = wordStr;
                            }
                            else
                            {
                                var tb = ParseTable(wordStr);
                                if (tb != null)
                                {
                                    if (Table != tb || joinType != JoinTypes.Undefined)
                                    {
                                        table = new QTable(tb) { Join = new JoinType(joinType) };
                                        Tables.Add(table);
                                    }
                                    else
                                    {
                                        table = QTable;
                                    }
                                    joinType = JoinTypes.Undefined;
                                }
                            }
                        }
                        word = ReadOnlySpan<char>.Empty;
                        startIndex = i + 1;
                        break;
                    default:
                        word = query.Slice(startIndex, (i - startIndex) + 1);
                        break;
                }
            }
        }

        public void ParseExpression(QExpression expression, string query)
        {
            ParseExpression(expression, query.AsSpan());
        }

        public void ParseExpression(QExpression expression, ReadOnlySpan<char> query)
        {
            QFunction func = null;
            var prefix = new List<string>();
            var word = ReadOnlySpan<char>.Empty;
            int startIndex = 0;
            for (int i = 0; i <= query.Length; i++)
            {
                var c = i < query.Length ? query[i] : '\n';
                switch (c)
                {
                    case '+':
                    case '-':
                    case '/':
                    case '*':
                        expression.Operations.Add(ParseMath(c));
                        break;
                    case ',':
                    case '\r':
                    case '\n':
                    case '(':
                    case ' ':
                    case '.':
                        if (word.Length > 0)
                        {
                            if (c == '.')
                            {
                                prefix.Add(word.ToString());
                            }
                            else
                            {
                                var fn = QFunction.ParseFunction(word);

                                if (fn != QFunctionType.none)
                                {
                                    func = new QFunction(fn);
                                    expression.Items.Add(func);
                                }
                                else
                                {
                                    var wordStr = word.ToString();
                                    var column = ParseColumn(wordStr, prefix.LastOrDefault(), out var qTable);
                                    if (column != null)
                                    {
                                        expression.Items.Add(new QColumn(column) { QTable = qTable });
                                        prefix.Clear();
                                    }
                                    else
                                    {
                                        expression.Items.Add(new QValue(wordStr));
                                    }
                                }
                            }
                            word = ReadOnlySpan<char>.Empty;
                            startIndex = i + 1;
                        }

                        if (c == '(')
                        {
                            var word2 = query.GetSubPart(ref i, '(', ')');
                            startIndex = i + 1;
                            if (MemoryExtensions.StartsWith(word2, StrSelect.AsSpan(), StringComparison.OrdinalIgnoreCase))
                            {
                                expression.Items.Add(new QQuery<DBItem>(this, word2));
                            }
                            else if (func != null)
                            {
                                ParseFunction(func, word2);
                                func = null;
                            }
                            else
                            {
                                var ex = new QExpression();
                                ParseExpression(ex, word2);
                                expression.Items.Add(ex);
                            }
                        }

                        break;
                    default:
                        word = query.Slice(startIndex, (i - startIndex) + 1);
                        break;
                }
            }
        }

        protected void ParseFunction(QFunction function, ReadOnlySpan<char> query)
        {
            QFunction func = null;
            QType qtype = null;
            var prefix = new List<string>();
            var word = ReadOnlySpan<char>.Empty;
            int startIndex = 0;
            for (int i = 0; i <= query.Length; i++)
            {
                var c = i < query.Length ? query[i] : '\n';

                if (c == ',' || c == '\r' || c == '\n' || c == '(' || c == ' ' || c == '.' || c == '\'')
                {
                    if (word.Length > 0)
                    {
                        if (c == '.')
                        {
                            prefix.Add(word.ToString());
                        }
                        else if (Helper.IsDecimal(word))
                        {
                            if (prefix.Count == 1 && Helper.IsDecimal(prefix[0]))
                            {
                                function.Items.Add(new QValue($"{prefix[0]}.{word.ToString()}"));
                                prefix.Clear();
                            }
                            else
                            {
                                function.Items.Add(new QValue(word.ToString()));
                            }
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
                                    var fn = QFunction.ParseFunction(word);
                                    if (fn != QFunctionType.none)
                                    {
                                        func = new QFunction(fn);
                                        function.Items.Add(func);
                                    }
                                    else
                                    {
                                        var wordStr = word.ToString();
                                        var col = ParseColumn(wordStr, prefix.LastOrDefault(), out var qTable);
                                        if (col != null)
                                        {
                                            function.Items.Add(new QColumn(col) { QTable = qTable });
                                            prefix.Clear();
                                        }
                                        else
                                        {
                                            function.Items.Add(new QValue(wordStr));
                                        }
                                    }
                                }
                            }
                        }

                        word = ReadOnlySpan<char>.Empty;
                        startIndex = i + 1;
                    }
                    if (c == '(')
                    {
                        var word2 = query.GetSubPart(ref i, '(', ')').Trim();
                        startIndex = i;
                        if (MemoryExtensions.StartsWith(word2, StrSelect.AsSpan(), StringComparison.OrdinalIgnoreCase))
                        {
                            function.Items.Add(new QQuery<DBItem>(this, word2));
                        }
                        else if (func != null)
                        {
                            ParseFunction(func, word2);
                            func = null;
                        }
                        else if (Helper.IsDecimal(word2, out var decimalValue))
                        {
                            qtype.Size = decimalValue;
                            qtype = null;
                        }
                        else
                        {
                            var ex = new QExpression();
                            ParseExpression(ex, word2);
                            function.Items.Add(ex);
                        }
                    }
                    else if (c == '\'')
                    {
                        var literal = query.GetSubPart(ref i, '\'', '\'');
                        function.Items.Add(new QValue(literal.ToString()));
                        startIndex = i + 1;
                    }

                }
                else
                    word = query.Slice(startIndex, (i - startIndex) + 1);
            }
        }

        private bool SetColumAlias(string wordStr)
        {
            if (Columns.Count > 0)
            {
                var latestColumn = Columns[Columns.Count - 1];
                if (string.IsNullOrEmpty(latestColumn.ColumnAlias))
                {
                    latestColumn.ColumnAlias = wordStr;
                    return true;
                }
            }
            return false;
        }

        public QParam NewParam() => NewParam(LogicType.Undefined);

        public QParam NewParam(LogicType logic)
        {
            var param = new QParam
            {
                Logic = logic
            };
            Parameters.Add(param);
            return param;
        }

        public QQuery<T> Column(QFunctionType type, params object[] args)
        {
            Columns.Add(CreateFunc(type, args));
            return this;
        }

        public QQuery<T> Column(IInvoker invoker, string prefix = null)
        {
            Columns.Add(CreateColumn(invoker, prefix));
            return this;
        }

        public QQuery<T> Column<K>(Expression<Func<T, K>> keySelector)
        {
            Columns.Add(CreateColumn(keySelector.Body));
            return this;
        }

        public QQuery<T> Column(string textConstant)
        {
            Columns.Add(QItem.Fabric(textConstant, null));
            return this;
        }

        public QQuery<T> Join(DBColumn column)
        {
            var join = CreateJoin(column);
            if (!Tables.Contains(join))
                Tables.Add(join);
            return this;
        }

        public QQuery<T> Join(DBReferencing referencing)
        {
            var join = CreateJoin(referencing);
            if (!Tables.Contains(join))
                Tables.Add(join);
            return this;
        }

        public QQuery<T> JoinAllReferencing()
        {
            foreach (var referencing in Table.Referencings)
                Join(referencing);
            return this;
        }

        public QQuery<T> Join(DBColumn column, DBColumn refColumn)
        {
            var join = CreateJoin(column, refColumn);
            if (!Tables.Contains(join))
                Tables.Add(join);
            return this;
        }

        public QQuery<T> Join(JoinType type, DBColumn column, string alias, DBColumn refColumn, string refAlias)
        {
            var join = CreateJoin(type, column, alias, refColumn, refAlias);
            if (!Tables.Contains(join))
                Tables.Add(join);
            return this;
        }

        public QQuery<T> OrderBy(IInvoker invoker, ListSortDirection direction = ListSortDirection.Ascending)
        {
            Orders.Add(CreateOrderBy(invoker, direction));
            return this;
        }

        public QQuery<T> OrderBy<V>(Expression<Func<T, V>> keySelector)
        {
            Orders.Add(CreateOrderBy(keySelector.Body, ListSortDirection.Ascending));
            return this;
        }

        public QQuery<T> OrderByDescending<V>(Expression<Func<T, V>> keySelector)
        {
            Orders.Add(CreateOrderBy(keySelector.Body, ListSortDirection.Descending));
            return this;
        }

        public QQuery<T> Where(Type typeFilter)
        {
            TypeFilter = typeFilter;
            return this;
        }

        public QQuery<T> Where(QParam param)
        {
            Parameters.Add(param);
            return this;
        }

        public QQuery<T> Where(Action<QParam> parameterGroup)
        {
            var qParam = new QParam() { Logic = LogicType.Undefined };
            Parameters.Add(qParam);

            parameterGroup(qParam);
            return this;
        }

        public QQuery<T> Where(string column, CompareType compare, object param)
        {
            Parameters.Add(CreateParam(LogicType.Undefined, column, compare, param));
            return this;
        }

        public QQuery<T> Where(IInvoker invoker, object param, QBuildParam buildParam = QBuildParam.None)
        {
            Parameters.Add(CreateParam(LogicType.Undefined, invoker, param, buildParam));
            return this;
        }

        public QQuery<T> Where(IInvoker invoker, CompareType compare, object param)
        {
            Parameters.Add(CreateParam(LogicType.Undefined, invoker, compare, param));
            return this;
        }

        public QQuery<T> And(QParam param)
        {
            param.Logic = LogicType.And;
            Parameters.Add(param);
            return this;
        }

        public QQuery<T> And(Action<QParam> parameterGroup)
        {
            var qParam = new QParam() { Logic = LogicType.And };
            parameterGroup(qParam);
            Parameters.Add(qParam);
            return this;
        }

        public QQuery<T> And(string column, CompareType compare, object param)
        {
            Parameters.Add(CreateParam(LogicType.And, column, compare, param));
            return this;
        }

        public QQuery<T> And(IInvoker invoker, object param, QBuildParam buildParam = QBuildParam.None)
        {
            Parameters.Add(CreateParam(LogicType.And, invoker, param, buildParam));
            return this;
        }

        public QQuery<T> And(IInvoker invoker, CompareType compare, object param)
        {
            Parameters.Add(CreateParam(LogicType.And, invoker, compare, param));
            return this;
        }

        public QQuery<T> Or(QParam param)
        {
            param.Logic = LogicType.Or;
            Parameters.Add(param);
            return this;
        }

        public QQuery<T> Or(Action<QParam> parameterGroup)
        {
            var qParam = new QParam() { Logic = LogicType.Or };
            parameterGroup(qParam);
            Parameters.Add(qParam);
            return this;
        }

        public QQuery<T> Or(string column, CompareType compare, object param)
        {
            Parameters.Add(CreateParam(LogicType.Or, column, compare, param));
            return this;
        }

        public QQuery<T> Or(IInvoker invoker, object param, QBuildParam buildParam = QBuildParam.None)
        {
            Parameters.Add(CreateParam(LogicType.Or, invoker, param, buildParam));
            return this;
        }

        public QQuery<T> Or(IInvoker invoker, CompareType compare, object param)
        {
            Parameters.Add(CreateParam(LogicType.Or, invoker, compare, param));
            return this;
        }

        public List<R> Sort<R>(List<R> list) where R : DBItem
        {
            var comparer = GetComparer<R>();
            if (comparer != null)
            {
                ListHelper.QuickSort<R>(list, comparer);
            }
            return list;
        }

        public DBComparerList<R> GetComparer<R>() where R : DBItem
        {
            if (orders == null
                || orders.Count == 0)
                return null;
            var comparer = new DBComparerList<R>();
            foreach (QOrder order in Orders)
            {
                var comparerEntry = order.CreateComparer<R>();
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

        public string GenerateTableAlias(IDBTable table)
        {
            var topQuery = GetTopQuery();
            var tables = topQuery.GetAllQItems<QTable>();

            return tables.Count().IntToChar();
        }

        private QTable GetTable(DBColumn column) => GetTable(column?.Table);

        public QTable GetTable(Type type)
        {
            if (type == null)
                return null;

            for (int i = Tables.Count - 1; i > -1; i--)
            {
                var qTable = Tables[i];
                if (qTable.Table.ItemType == type)
                    return qTable;
            }
            return null;
        }

        public QTable GetTable(IDBTable table)
        {
            if (table == null)
                return null;

            for (int i = Tables.Count - 1; i > -1; i--)
            {
                var qTable = Tables[i];
                if (qTable.Table == table)
                    return qTable;
            }
            return null;
        }

        public QTable GetTableByAlias(string alias)
        {
            if (alias == null)
                return null;
            return tables?.FirstOrDefault(p => string.Equals(alias, p.TableAlias, StringComparison.OrdinalIgnoreCase));
        }

        public QFunction CreateFunc(QFunctionType type, params object[] args)
        {
            return new QFunction(type, args);
        }

        public IEnumerable<QColumn> CreateColumns(IEnumerable<DBColumn> cols)
        {
            foreach (DBColumn col in cols)
            {
                yield return CreateColumn(col);
            }
        }

        public QInvoker CreateInvoker(IInvoker invoker, string tableAlias = null)
        {
            return new QInvoker(invoker)
            {
                TableAlias = tableAlias ?? GetTable(invoker.TargetType)?.TableAlias
            };
        }

        public QColumn CreateColumn(DBColumn column, string tableAlias = null)
        {
            return new QColumn(column)
            {
                TableAlias = tableAlias ?? GetTable(column.Table)?.TableAlias
            };
        }

        private QItem CreateColumn(IInvoker invoker, string prefix)
        {
            if (invoker is DBColumn column)
                return CreateColumn(column, prefix);
            else if (invoker is QItem qItem)
                return qItem;
            else
                return CreateInvoker(invoker, prefix);
        }

        public QItem CreateColumn(Expression expression)
        {
            if (expression is MemberExpression memberExpression)
            {
                var column = ParseColumn(memberExpression.Member.Name, null, out var qTable);
                if (column != null)
                {
                    return CreateColumn(column, qTable.TableAlias);
                }
                else
                {
                    var invoker = EmitInvoker.Initialize(memberExpression.Member);
                    return CreateInvoker(invoker, GetTable(memberExpression.Member.ReflectedType)?.TableAlias);
                }
            }
            else
            {
                throw new NotSupportedException(expression.GetType().Name);
            }
        }

        public QOrder CreateOrderBy(Expression expression, ListSortDirection direction)
        {
            if (expression is MemberExpression memberExpression)
            {
                var column = ParseColumn(memberExpression.Member.Name, null, out var qTable);
                if (column != null)
                {
                    return CreateOrderBy(column, direction, qTable.TableAlias);
                }
                else
                {
                    var invoker = EmitInvoker.Initialize(memberExpression.Member);
                    return CreateOrderBy(invoker, direction, GetTable(memberExpression.Member.ReflectedType)?.TableAlias);
                }
            }
            else
            {
                throw new NotSupportedException(expression.GetType().Name);
            }
        }

        public QOrder CreateOrderBy(DBColumn column, ListSortDirection direction, string tableAlias = null)
        {
            return new QOrder(CreateColumn(column, tableAlias))
            {
                Direction = direction
            };
        }

        public QOrder CreateOrderBy(IInvoker invoker, ListSortDirection direction, string tableAlias = null)
        {
            if (invoker is DBColumn column)
                return CreateOrderBy(column, direction, tableAlias);
            return new QOrder(CreateInvoker(invoker, tableAlias))
            {
                Direction = direction
            };
        }

        public QTable CreateJoin(DBReferencing referencing)
        {
            return CreateJoin(referencing.ReferenceColumn, Table.PrimaryKey);
        }

        public QTable CreateJoin(DBColumn refColumn)
        {
            return CreateJoin(refColumn.ReferenceTable, refColumn);
        }

        public QTable CreateJoin(IDBTable table, DBColumn refColumn)
        {
            return CreateJoin(table.PrimaryKey, refColumn);
        }

        public QTable CreateJoin(DBColumn column, DBColumn refColumn)
        {
            return CreateJoin(JoinType.Left, column, GenerateTableAlias(column.Table), refColumn, GetTable(refColumn.Table).TableAlias);
        }

        public QTable CreateJoin(JoinType joinType, IDBTable table, string alias, DBColumn refColumn, string refAlias)
        {
            return CreateJoin(joinType, table.PrimaryKey, alias, refColumn, refAlias);
        }

        public QTable CreateJoin(JoinType joinType, DBColumn column, string alias, DBColumn refColumn, string refAlias)
        {
            var joinTable = Tables.FirstOrDefault(p => p.Table == column.Table
                                                && p.On != null
                                                && p.On.LeftColumn == column
                                                && p.On.RightColumn == refColumn);
            return joinTable ?? new QTable((DBTable)column.Table, alias)
            {
                Join = joinType,
                On = new QParam
                {
                    LeftItem = CreateColumn(column, alias),
                    Comparer = CompareType.Equal,
                    RightItem = CreateColumn(refColumn, refAlias)
                }
            }; ;
        }

        public QParam CreateNameParam(LogicType logic, string property, CompareType comparer, object value)
        {
            var parameter = new QParam { Logic = logic };
            foreach (var column in Table.Columns.GetByGroup(property))
            {
                parameter.Parameters.Add(CreateParam(LogicType.Or, column, comparer, value));
            }
            return parameter;
        }

        public QParam CreateParam(LogicType logic, string column, object value, QBuildParam buildParam = QBuildParam.None)
        {
            var comparer = this.DetectComparer(null, ref value, buildParam);
            return CreateParam(logic, column, comparer, value);
        }

        public QParam CreateParam(LogicType logic, string column, CompareType comparer, object value)
        {
            int index = column.IndexOf('.');
            if (index >= 0)
            {
                string[] split = column.Split(Helper.DotSeparator);//TODO JOIN
                var qTable = QTable;
                var table = Table;
                DBColumn refColumn = null;
                for (int i = 0; i < split.Length; i++)
                {
                    refColumn = table.GetColumnOrProperty(split[i]);
                    if (refColumn != null)
                    {
                        ///q.Columns.Add(new QColumn(column.ReferenceTable.PrimaryKey.Code));
                        if (refColumn.IsReference && i + 1 < split.Length)
                        {
                            table = (DBTable)refColumn.ReferenceTable;
                            qTable = CreateJoin(table.PrimaryKey, refColumn);
                            if (!Tables.Contains(qTable))
                                Tables.Add(qTable);
                        }
                    }
                }
                return CreateParam(logic, refColumn, comparer, value);
            }
            else
            {
                var dbColumn = ParseColumn(column, null, out var qTable);
                if (dbColumn == null)
                    return CreateNameParam(logic, column, comparer, value);
                return CreateParam(logic, dbColumn, comparer, value);
            }
        }

        public QParam CreateParam(LogicType logic, DBColumn column, CompareType compare, object value)
        {
            string alias = QTable.TableAlias;
            if (column.Table != Table)
            {
                var table = GetTable(column.Table);
                if (table == null)
                {
                    if (Table.IsVirtual && column.Table == Table.ParentTable)
                    {
                        column = column.GetVirtualColumn(Table);
                        table = QTable;
                    }
                    else if (column.Table.IsVirtual && column.Table.ParentTable == Table)
                    {
                        column = column.ParentColumn;
                        table = QTable;
                    }
                    else
                    {
                        var refColumn = Table.GetReferenceColumns().FirstOrDefault(p => p.ReferenceTable == column.Table);
                        if (refColumn != null)
                        {
                            table = CreateJoin(column.Table, refColumn);
                        }
                        else
                        {
                            var refingColumn = Table.GetChildRelations().FirstOrDefault(p => p.Table == column.Table)?.Column;
                            if (refingColumn != null)
                            {
                                table = CreateJoin(refingColumn, Table.PrimaryKey);
                            }
                        }
                        if (table == null)
                            throw new Exception($"Missing Join from {column.Table} to {Table}");
                        Tables.Add(table);
                    }
                }

                alias = table.TableAlias;
            }

            return new QParam
            {
                Logic = logic,
                LeftItem = CreateColumn(column, alias),
                Comparer = compare,
                RightItem = QItem.Fabric(value, column)
            };
        }

        public QParam CreateParam(LogicType logic, IInvoker invoker, CompareType comparer, object value)
        {
            if (invoker is DBColumn column)
                return CreateParam(logic, column, comparer, value);
            return new QParam
            {
                Logic = logic,
                LeftItem = CreateInvoker(invoker),
                Comparer = comparer,
                RightItem = QItem.Fabric(value, null)
            };
        }

        public QParam CreateParam(LogicType logic, IInvoker invoker, object value, QBuildParam buildParam = QBuildParam.None)
        {
            var comparer = DetectComparer(invoker, ref value, buildParam);
            return CreateParam(logic, invoker, comparer, value);
        }

        public QParam CreateParam(LogicType logic, DBColumn parent, DBColumn column, object p, QBuildParam buildParam)
        {
            var query = Join(column.Table.PrimaryKey, parent);
            return CreateParam(logic, column, p, buildParam);
        }

        private CompareType ParseComparer(ExpressionType nodeType)
        {
            switch (nodeType)
            {
                case ExpressionType.Equal: return CompareType.Equal;
                case ExpressionType.NotEqual: return CompareType.NotEqual;
                case ExpressionType.GreaterThan: return CompareType.Greater;
                case ExpressionType.GreaterThanOrEqual: return CompareType.GreaterOrEqual;
                case ExpressionType.LessThanOrEqual: return CompareType.LessOrEqual;
                case ExpressionType.LessThan: return CompareType.Less;

                default: throw new NotImplementedException("TODO");
            }
        }

        private LogicType ParseLogic(ExpressionType nodeType)
        {
            switch (nodeType)
            {
                case ExpressionType.Or:
                case ExpressionType.OrElse: return LogicType.Or;
                case ExpressionType.And:
                case ExpressionType.AndAlso: return LogicType.Or;

                default: return LogicType.Undefined;
            }
        }

        private QParam CreateParam(Expression expression, LogicType logicType)
        {
            switch (expression)
            {
                case BinaryExpression binaryExpression:
                    return CreateParam(binaryExpression, logicType);
                case MemberExpression memberExpression:
                    return CreateParam(memberExpression, logicType);
                case UnaryExpression unaryExpression:
                    return CreateParam(unaryExpression, logicType);
                default:
                    throw new NotImplementedException("TODO");
            }
        }

        private QParam CreateParam(UnaryExpression unaryExpression, LogicType logicType)
        {
            switch (unaryExpression.NodeType)
            {
                case ExpressionType.Not:
                    if (unaryExpression.Operand is MemberExpression memberExpression)
                    {
                        var param = CreateParam(memberExpression, logicType);
                        param.Comparer = CompareType.NotEqual;
                        return param;
                    }
                    throw new NotImplementedException("TODO");
            }
            throw new NotImplementedException("TODO");
        }

        private QParam CreateParam(MemberExpression memberExpression, LogicType logicType)
        {
            if (TypeHelper.GetMemberType(memberExpression.Member) == typeof(bool))
            {
                return new QParam
                {
                    Logic = logicType,
                    LeftItem = CreateParamItem(memberExpression),
                    Comparer = CompareType.Equal,
                    RightItem = new QValue(true),
                };
            }
            throw new NotImplementedException("TODO");
        }

        private QParam CreateParam(BinaryExpression binaryExpression, LogicType logicType)
        {
            var qParam = new QParam();
            qParam.Logic = logicType;
            var logiC = ParseLogic(binaryExpression.NodeType);
            if (logiC == LogicType.Undefined)
            {
                qParam.Comparer = ParseComparer(binaryExpression.NodeType);
                qParam.Add(ParseBinaryMember(binaryExpression.Left, binaryExpression));
                qParam.Add(ParseBinaryMember(binaryExpression.Right, binaryExpression));
                return qParam;
            }
            else
            {
                qParam.Add(CreateParam(binaryExpression.Left, LogicType.Undefined));
                qParam.Add(CreateParam(binaryExpression.Right, logiC));
            }
            return qParam;
        }

        private QItem ParseBinaryMember(Expression expression, Expression baseExpression)
        {
            switch (expression)
            {
                case MemberExpression memberExpression:
                    if (memberExpression.Expression is not ParameterExpression)
                        return ParseBinaryMember(memberExpression.Expression, expression);
                    return CreateParamItem(memberExpression);
                case ConstantExpression constExpression:
                    if (baseExpression is UnaryExpression
                        || baseExpression is BinaryExpression)
                        return new QValue(constExpression.Value);
                    else if (baseExpression is MemberExpression constMemberExpression)
                        return new QValue(EmitInvoker.GetValue(constMemberExpression.Member, constExpression.Value));
                    throw new NotImplementedException("TODO");
                case UnaryExpression unaryExpression:
                    return ParseBinaryMember(unaryExpression.Operand, unaryExpression);
                default:
                    throw new NotImplementedException("TODO");
            }
        }

        private QItem CreateParamItem(MemberExpression memberExpression)
        {
            var column = ParseColumn(memberExpression.Member.Name, null, out var qTable);
            if (column != null)
            {
                return new QColumn(column) { QTable = qTable };
            }
            else
            {
                return new QInvoker(EmitInvoker.Initialize(memberExpression.Member));
            }
        }

        protected CompareType DetectComparer(IInvoker column, ref object value, QBuildParam buildParam)
        {
            var valueType = value?.GetType();
            var type = column?.DataType ?? valueType;
            var comparer = CompareType.Equal;
            if (value is IQQuery)
            {
                comparer = CompareType.In;
            }
            else if (value is DBItem dbItem)
            {
                value = dbItem.PrimaryId;
            }
            else if (type == typeof(string)
                && value is string stringValue)
            {
                if ((buildParam & QBuildParam.AutoLike) != 0)
                {
                    comparer = CompareType.Like;
                    if (!stringValue.Contains('%'))
                        value = $"%{stringValue}%";
                }
                else if ((buildParam & QBuildParam.SplitString) != 0
                    && stringValue.Contains(',') == true)
                {
                    comparer = CompareType.In;
                    value = value.ToString().Split(Helper.CommaSeparator, StringSplitOptions.RemoveEmptyEntries);
                }
            }
            else if (value is DateInterval)
            {
                comparer = CompareType.Between;
            }
            else if ((valueType?.IsList() ?? false) && valueType != type)
            {
                comparer = CompareType.In;
            }

            return comparer;
        }


        public static QMathType ParseMath(char code)
        {
            QMathType en = QMathType.None;
            switch (code)
            {
                case '+': en = QMathType.Plus; break;
                case '-': en = QMathType.Minus; break;
                case '/': en = QMathType.Devide; break;
                case '*': en = QMathType.Multiply; break;
            }

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

        public IEnumerable<QParam> GetAllParameters(Func<QParam, bool> predicate = null)
        {
            return Parameters.GetAllQItems<QParam>(predicate);
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
            var subQueryLevel = GetSubQueryLevel();
            var subSpace = subQueryLevel == 0 ? string.Empty : new string(' ', subQueryLevel * 8);

            var cols = FormatSelect(command, defColumns, subSpace);
            var partOrder = FormatOrders(command, subSpace);
            var partFrom = FormatFrom(command, subSpace);
            var partWhere = FormatWhere(command, subSpace);

            var resultBuilder = new StringBuilder();
            resultBuilder.Append(StrSelect);
            resultBuilder.Append(' ');
            resultBuilder.Append(cols);
            resultBuilder.Append('\n');
            resultBuilder.Append(subSpace);
            resultBuilder.Append(StrFrom);
            resultBuilder.Append(partFrom);
            if (partWhere.Length > 0)
            {
                resultBuilder.Append('\n');
                resultBuilder.Append(subSpace);
                resultBuilder.Append(StrWhere);
                resultBuilder.Append(' ');
                resultBuilder.Append(partWhere);
            }
            if (partOrder.Length > 0)
            {
                resultBuilder.Append('\n');
                resultBuilder.Append(subSpace);
                resultBuilder.Append(StrOrder);
                resultBuilder.Append(' ');
                resultBuilder.Append(StrBy);
                resultBuilder.Append(' ');
                resultBuilder.Append(partOrder);
            }
            return resultBuilder.ToString();
        }

        private StringBuilder FormatFrom(IDbCommand command, string subSpace)
        {
            var from = new StringBuilder();
            foreach (QTable table in Tables)
            {
                from.Append(table.Format(command));
                if (!Tables.IsLast(table))
                {
                    from.Append("\n    ");
                    from.Append(subSpace);
                }
            }
            return from;
        }

        private StringBuilder FormatOrders(IDbCommand command, string subSpace)
        {
            var order = new StringBuilder();
            if (orders != null)
            {
                foreach (QOrder col in Orders)
                {
                    var formatOrder = col.Format(command);
                    if (!string.IsNullOrEmpty(formatOrder))
                    {
                        order.Append(formatOrder);
                        if (!orders.IsLast(col))
                        {
                            order.Append("\n    ,");
                            order.Append(subSpace);
                        }
                    }
                }
            }
            return order;
        }

        private StringBuilder FormatSelect(IDbCommand command, bool defColumns, string subSpace)
        {
            var cols = new StringBuilder();
            if (defColumns || (columns?.Count ?? 0) == 0)
            {
                if (Tables.Count > 1)
                    FormatJoinSelect(subSpace, cols);
                else
                    FormatSelect(subSpace, cols);
            }
            else
            {
                foreach (QItem col in Columns)
                {
                    string temp = col.Format(command);
                    if (!string.IsNullOrEmpty(temp))
                    {
                        if (cols.Length > 0)
                        {
                            cols.Append('\n');
                            cols.Append(subSpace);
                            cols.Append("    ,");
                        }
                        if (col is IQQuery)
                        {
                            cols.Append("(");
                        }
                        cols.Append(temp);
                        if (col is IQQuery)
                        {
                            cols.Append(")");
                        }
                        if (ColumnAlias != null)
                            cols.Append($" as \"{ColumnAlias}\"");
                    }
                }
            }
            return cols;
        }

        private void FormatSelect(string subSpace, StringBuilder cols)
        {
            foreach (var col in Table.GetQueryColumns(LoadParam))
            {
                string temp = System.FormatQColumn(col, QTable.TableAlias);
                if (!string.IsNullOrEmpty(temp))
                {
                    if (cols.Length > 0)
                    {
                        cols.Append('\n');
                        cols.Append(subSpace);
                        cols.Append("    ,");
                    }
                    cols.Append(temp);
                }
            }
        }

        private void FormatJoinSelect(string subSpace, StringBuilder cols)
        {
            foreach (var table in Tables)
            {
                var tableIndex = table.Order;
                foreach (var col in table.Table.GetQueryColumns(LoadParam))
                {
                    var columnAlias = $"{tableIndex}.{col.Name}";
                    string temp = System.FormatQColumn(col, table.TableAlias, columnAlias);
                    if (!string.IsNullOrEmpty(temp))
                    {
                        if (cols.Length > 0)
                        {
                            cols.Append('\n');
                            cols.Append(subSpace);
                            cols.Append("    ,");
                        }
                        cols.Append(temp);
                    }
                }
            }
        }

        public StringBuilder FormatWhere(IDbCommand command = null, string subSpace = null)
        {
            var wbuf = new StringBuilder();
            for (int i = 0; i < Parameters.Count; i++)
            {
                QParam param = parameters[i];
                string bufRez = param.Format(command);
                if (bufRez.Length > 0)
                {
                    if (wbuf.Length > 0 || i > 0)
                    {
                        wbuf.Append('\n');
                        wbuf.Append(subSpace ?? string.Empty);
                        wbuf.Append("    ");
                        wbuf.Append(param.Logic.Format());
                        wbuf.Append(' ');
                    }
                    wbuf.Append(bufRez);
                }
            }
            var buf = new StringBuilder();
            //parameters._ApplySort("Order");
            if (command != null
                && Table.IsVirtual
                && Table.FilterQuery.Parameters.Count > 0)
            {
                if (wbuf.Length > 0)
                    buf.Append("(");
                foreach (QParam param in Table.FilterQuery.Parameters)
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
                {
                    buf.Append(")");
                    buf.Append('\n');
                    buf.Append(subSpace);
                    buf.Append("    and(");
                }
            }
            buf.Append(wbuf);
            if (Table.IsVirtual && command != null && wbuf.Length > 0)
                buf.Append(")");
            return buf;
        }

        public IDbCommand ToCommand(bool defcolumns = false)
        {
            var command = Schema.Connection.CreateCommand();
            command.CommandText = FormatAll(command, defcolumns);
            return command;
        }

        public string ToText()
        {
            string buf = string.Empty;
            foreach (QParam param in Parameters)
            {
                string bufRez = param.ToString();
                if (bufRez != "")
                    buf += (buf != "" ? param.Logic.ToString() : "") + " " + bufRez + " " + "\r\n";
            }
            return buf;
        }

        public bool IsNoParameters()
        {
            return (parameters?.Count ?? 0) == 0;
        }

        public bool Contains(string column)
        {
            return parameters?.Select(QParam.ColumnNameInvoker.Instance, CompareType.Equal, column).Any() ?? false;
        }

        public bool Contains(DBColumn column)
        {
            return GetByColumn(column) != null;
        }

        public QParam GetByColumn(DBColumn column)
        {
            if (parameters == null)
                return null;
            foreach (QParam p in parameters)
                if (p.IsColumn(column))
                    return p;
            return null;
        }

        public void RemoveParameter(DBColumn column)
        {
            for (int i = 0; i < Parameters.Count;)
            {
                QParam p = Parameters[i];
                if (p.IsColumn(column))
                    Parameters.Remove(p);
                else
                    i++;
            }
        }

        public override object GetValue(DBItem item)
        {
            if (Columns.Count == 0)
            {
                Column(Table.PrimaryKey);
            }
            if (IsReference && item != null)
            {
                foreach (QParam param in GetAllParameters())
                {
                    if (param.RightColumn is var rColumn
                        && rColumn.Table == item.Table)
                    {
                        param.RightQColumn.Value = item.GetValue(rColumn);
                    }
                    if (param.LeftColumn is var lColumn
                        && lColumn.Table == item.Table)
                    {
                        param.LeftQColumn.Value = item.GetValue(lColumn);
                    }
                }
            }
            if (Columns[0] is QColumn qColumn)
            {
                return qColumn.Column.Distinct(Select<T>());
            }
            var objects = new List<object>();
            foreach (var row in Select<T>())
            {
                object value = Columns[0].GetValue(row);
                int index = ListHelper.BinarySearch<object>(objects, value, null);
                if (index < 0)
                {
                    objects.Insert(-index - 1, value);
                }
            }
            return objects;
        }

        public override object GetValue<R>()
        {
            if (IsReference)
                throw new Exception("Unable to get query result without referenced value");
            var param = this.Container?.Owner as QParam;
            var column = param.LeftColumn ?? param.RightColumn;
            var comparer = param.Comparer;
            if ((column?.IsPrimaryKey ?? false) && Enumerable.FirstOrDefault(Columns) is QColumn qcolumn)
            {
                var buf = new List<R>();
                foreach (var item in Select<DBItem>())
                {
                    var reference = item.GetReference<DBItem>(qcolumn.Column, DBLoadParam.None);

                    if (reference is R refTyped)
                    {
                        var index = buf.BinarySearch(refTyped);
                        if (index < 0)
                            buf.Insert(-index - 1, refTyped);
                    }
                }
                return buf;
            }
            return GetValue((DBItem)null);
        }

        public override bool CheckItem(DBItem item, object val2, CompareType comparer)
        {
            return QParam.CheckItem(item, Parameters);
        }

        public void Add(QItem item)
        {
            if (item is QColumn column)
                Columns.Add(column);
            else if (item is QParam param)
                Parameters.Add(param);
            else if (item is QOrder order)
                Orders.Add(order);
            else if (item is QTable table)
                Tables.Add(table);
        }

        public void Delete(QItem item)
        {
            if (item is QColumn column)
                Columns.Remove(column);
            else if (item is QParam param)
                Parameters.Remove(param);
            else if (item is QOrder order)
                Orders.Remove(order);
            else if (item is QTable table)
                Tables.Remove(table);
        }

        public IEnumerable<IT> GetAllQItems<IT>(Func<IT, bool> predicate = null) where IT : IQItem
        {
            var result = Enumerable.Empty<IT>();
            if (columns != null)
                result = columns.GetAllQItems<IT>(predicate);
            if (tables != null)
                result = result.Concat(tables.GetAllQItems<IT>(predicate));
            if (parameters != null)
                result = result.Concat(parameters.GetAllQItems<IT>(predicate));
            if (orders != null)
                result = result.Concat(orders.GetAllQItems<IT>(predicate));
            return result;
        }

        public IEnumerable<TT> Select<TT>() where TT : DBItem => Table.Select<TT>(this);

        public IEnumerable<T> Select() => TTable.Select(this);

        public IEnumerable<R> Load<R>(DBTransaction transaction = null) where R : DBItem => TTable.Load<R>(this, transaction);

        public IEnumerable<T> Load(DBTransaction transaction = null) => TTable.Load(this, transaction);

        public ValueTask<IEnumerable<T>> LoadAsync(DBTransaction transaction = null) => TTable.LoadAsync(this, transaction);

        public IEnumerator<TT> GetYieldEnumerator<TT>() where TT : DBItem
        {
            foreach (var item in Table.Load<TT>(this))
            {
                yield return item;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetYieldEnumerator<DBItem>();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return GetYieldEnumerator<T>();
        }

        public async ValueTask<T> FirstOrDefaultAsync(DBTransaction transaction = null)
        {
            return TTable.Select(this).FirstOrDefault() ?? (await TTable.LoadAsync(this, transaction)).FirstOrDefault();
        }

        public T FirstOrDefault(DBTransaction transaction = null)
        {
            return TTable.Select(this).FirstOrDefault() ?? TTable.Load(this, transaction).FirstOrDefault();
        }

        public R FirstOrDefault<R>(DBTransaction transaction = null) where R : DBItem
        {
            return TTable.Select<R>(this).FirstOrDefault() ?? TTable.Load<R>(this, transaction).FirstOrDefault();
        }

        public T FirstOrDefault(Func<T, bool> predicate, DBTransaction transaction = null)
        {
            return TTable.Select(this).FirstOrDefault(predicate) ?? TTable.Load(this, transaction).FirstOrDefault(predicate);
        }

        public R FirstOrDefault<R>(Func<R, bool> predicate, DBTransaction transaction = null) where R : DBItem
        {
            return TTable.Select<R>(this).FirstOrDefault(predicate) ?? TTable.Load<R>(this, transaction).FirstOrDefault(predicate);
        }

        IQQuery IQQuery.Column(QFunctionType function, params object[] args) => Column(function, args);
        IQQuery IQQuery.Column(IInvoker invoker) => Column(invoker);

        IQQuery IQQuery.Join(DBColumn column) => Join(column);
        IQQuery IQQuery.Join(DBReferencing referencing) => Join(referencing);
        IQQuery IQQuery.JoinAllReferencing() => JoinAllReferencing();
        IQQuery IQQuery.Join(DBColumn column, DBColumn refColumn) => Join(column, refColumn);

        IQQuery IQQuery.Where(string text) => Where(text);
        IQQuery IQQuery.WhereViewColumns(string text, QBuildParam param) => WhereViewColumns(text, param);
        IQQuery IQQuery.Where(Type typeFilter) => Where(typeFilter);
        IQQuery IQQuery.Where(QParam qParam) => Where(qParam);

        IQQuery IQQuery.Where(Action<QParam> qParam) => Where(qParam);
        IQQuery IQQuery.Where(IInvoker invoker, object value, QBuildParam param) => Where(invoker, value, param);
        IQQuery IQQuery.Where(IInvoker invoker, CompareType comparer, object value) => Where(invoker, comparer, value);
        IQQuery IQQuery.Where(string column, CompareType comparer, object value) => Where(column, comparer, value);

        IQQuery IQQuery.And(QParam qParam) => And(qParam);
        IQQuery IQQuery.And(Action<QParam> qParam) => And(qParam);
        IQQuery IQQuery.And(IInvoker invoker, object value, QBuildParam param) => And(invoker, value, param);
        IQQuery IQQuery.And(IInvoker invoker, CompareType comparer, object value) => And(invoker, comparer, value);
        IQQuery IQQuery.And(string column, CompareType comparer, object value) => And(column, comparer, value);

        IQQuery IQQuery.Or(QParam qParam) => Or(qParam);
        IQQuery IQQuery.Or(Action<QParam> qParam) => Or(qParam);
        IQQuery IQQuery.Or(IInvoker invoker, object value, QBuildParam param) => Or(invoker, value, param);
        IQQuery IQQuery.Or(IInvoker invoker, CompareType comparer, object value) => Or(invoker, comparer, value);
        IQQuery IQQuery.Or(string column, CompareType comparer, object value) => Or(column, comparer, value);

        IQQuery IQQuery.OrderBy(IInvoker invoker, ListSortDirection direction) => OrderBy(invoker, direction);

        IQQuery<T> IQQuery<T>.Column(QFunctionType function, params object[] args) => Column(function, args);
        IQQuery<T> IQQuery<T>.Column(IInvoker invoker) => Column(invoker);
        IQQuery<T> IQQuery<T>.Column<K>(Expression<Func<T, K>> keySelector) => Column(keySelector);

        IQQuery<T> IQQuery<T>.Join(DBColumn column) => Join(column);
        IQQuery<T> IQQuery<T>.Join(DBReferencing referencing) => Join(referencing);
        IQQuery<T> IQQuery<T>.JoinAllReferencing() => JoinAllReferencing();
        IQQuery<T> IQQuery<T>.Join(DBColumn column, DBColumn refColumn) => Join(column, refColumn);

        IQQuery<T> IQQuery<T>.Where(string text) => Where(text);
        IQQuery<T> IQQuery<T>.Where(Expression<Func<T, bool>> expression) => Where(expression);
        IQQuery<T> IQQuery<T>.WhereViewColumns(string text, QBuildParam param) => WhereViewColumns(text, param);
        IQQuery<T> IQQuery<T>.Where(Type typeFilter) => Where(typeFilter);
        IQQuery<T> IQQuery<T>.Where(QParam qParam) => Where(qParam);
        IQQuery<T> IQQuery<T>.Where(Action<QParam> qParam) => Where(qParam);
        IQQuery<T> IQQuery<T>.Where(IInvoker invoker, object value, QBuildParam param) => Where(invoker, value, param);
        IQQuery<T> IQQuery<T>.Where(IInvoker invoker, CompareType comparer, object value) => Where(invoker, comparer, value);
        IQQuery<T> IQQuery<T>.Where(string invoker, CompareType comparer, object value) => Where(invoker, comparer, value);

        IQQuery<T> IQQuery<T>.And(QParam qParam) => And(qParam);
        IQQuery<T> IQQuery<T>.And(Action<QParam> qParam) => And(qParam);
        IQQuery<T> IQQuery<T>.And(IInvoker invoker, object value, QBuildParam param) => And(invoker, value, param);
        IQQuery<T> IQQuery<T>.And(IInvoker invoker, CompareType comparer, object value) => And(invoker, comparer, value);
        IQQuery<T> IQQuery<T>.And(string invoker, CompareType comparer, object value) => And(invoker, comparer, value);

        IQQuery<T> IQQuery<T>.Or(QParam qParam) => Or(qParam);
        IQQuery<T> IQQuery<T>.Or(Action<QParam> qParam) => Or(qParam);
        IQQuery<T> IQQuery<T>.Or(IInvoker invoker, object value, QBuildParam param) => Or(invoker, value, param);
        IQQuery<T> IQQuery<T>.Or(IInvoker invoker, CompareType comparer, object value) => Or(invoker, comparer, value);
        IQQuery<T> IQQuery<T>.Or(string invoker, CompareType comparer, object value) => Or(invoker, comparer, value);

        IQQuery<T> IQQuery<T>.OrderBy(IInvoker invoker, ListSortDirection direction) => OrderBy(invoker, direction);
        IQQuery<T> IQQuery<T>.OrderBy<K>(Expression<Func<T, K>> keySelector) => OrderBy(keySelector);
        IQQuery<T> IQQuery<T>.OrderByDescending<K>(Expression<Func<T, K>> keySelector) => OrderByDescending(keySelector);

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
    public enum QBuildParam
    {
        None = 0,
        AutoLike = 1,
        SplitString = 2
    }

}

