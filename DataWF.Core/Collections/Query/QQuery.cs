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
using DataWF.Collections.Query.Evaluators;
using DataWF.Invokers;
using DataWF.Models;
using DataWF.Comparers;

namespace DataWF.Collections.Query;

public class QQuery<T> : QItem, IQQuery<T>, IDisposable
{
    protected QParamList? parameters;
    protected QItemList<QItem>? columns;
    protected QItemList<QOrder>? orders;
    protected QItemList<QItem>? groups;
    protected QTableList? tables;
    private IQQuery? baseQuery;
    private DBStatus status = DBStatus.Empty;
    private IModelSchema? schema;
    private Type? type;
    private string? queryText;
    private string? whereText;
    private ITreeComparer? treeComparer;
    public QQuery()
    {
        order = 0;
    }

    public QQuery(IModelSchema schema) : this()
    {
        Schema = schema;
    }

    public QQuery(IModelTable table, string? queryText = null) : this(table.Schema!)
    {
        Table = table;
        Parse(queryText);
    }

    public QQuery(IEnumerable<T> source, string? queryText = null)
    {
        Source = source;
        Parse(queryText);
    }

    public QQuery(IModelTable table, Expression<Func<T, bool>> expression) : this(table.Schema!)
    {
        Table = table;
        Where(expression);
    }

    public QQuery(IModelSchema schema, string queryText, IModelTable? table = null)
        : this(schema)
    {
        Table = table;
        Parse(queryText);
    }

    public QQuery(IModelSchema schema, ReadOnlySpan<char> queryText, IModelTable? table = null)
        : this(schema)
    {
        Table = table;
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

    public override IModelSchema? Schema
    {
        get => schema ??= Table?.Schema;
        set => schema = value;
    }

    public override IQTable? QTable
    {
        get => qTable ??= tables?.FirstOrDefault();
        set => Tables.Add(value);
    }

    public IEnumerable<T> Source
    {
        get => ((IQTable<T>)QTable)?.Source;
        set
        {
            if (Source != value)
            {
                if (value != null)
                    Tables.Add(new QTable<T>(value, GenerateTableAlias(value)));
                else
                    Tables.Clear();
            }
        }
    }

    public IModelTable<T> TTable => (IModelTable<T>)Table;

    public override IModelTable? Table
    {
        get => QTable?.Table;
        set
        {
            if (value != Table)
            {
                if (value != null)
                    Tables.Add(value.CreateQTable(GenerateTableAlias(value)));
                else
                    Tables.Clear();
            }
        }
    }

    public QTableList Tables => tables ??= new QTableList(this);

    public QItemList<QItem> Columns => columns ??= new QItemList<QItem>(this);

    public QItemList<QOrder> Orders => orders ??= new QItemList<QOrder>(this);

    public QParamList Parameters => parameters ??= new QParamList(this);

    public QItemList<QItem> Groups => groups ??= new QItemList<QItem>(this);

    public IQQuery? BaseQuery
    {
        get => baseQuery ?? Holder?.Query;
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
    public override IQItem? Holder => base.Holder ?? this;

    [JsonIgnore]
    public override IQQuery Query => BaseQuery ?? this;

    public Type TypeFilter
    {
        get => type;
        set
        {
            if (Table?.ItemTypeKey == null || TypeFilter == value)
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
                    param.RightItem = new QValue<int>(typeIndex);
                }
            }
        }
    }

    public ITreeComparer TreeComparer
    {
        get => treeComparer;
        set
        {
            if (treeComparer != value)
            {
                treeComparer = value;
                OnOrdersChanged(treeComparer, EventArgs.Empty);
            }
        }
    }

    public bool IsEnabledFormatting => Parameters.GetAllItems<IQCompareParameter>(p => !p.FormatIgnore && p.IsEnabled).Any();

    public bool IsEnabled => Parameters.GetAllItems<IQCompareParameter>(p => p.IsEnabled).Any();

    public bool HasOrderBy => Orders.Count > 0 || TreeComparer != null;

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

    public event EventHandler? OrdersChanged;

    public event EventHandler? ParametersChanged;

    internal void OnOrdersChanged(object sender, EventArgs e)
    {
        OrdersChanged?.Invoke(sender, e);
    }

    internal void OnParametersChanged(object sender, EventArgs e)
    {
        ParametersChanged?.Invoke(sender, e);
    }

    public IEnumerable<IQCompareParameter> GetGlobal()
    {
        return ((IEnumerable<IQCompareParameter>)Parameters).Where(p => p.IsEnabled && p.IsGlobal);
    }

    public void Clear()
    {
        Parameters.Clear();
    }

    public IQCompareParameter CreateTypeParam(Type type)
    {
        var typeIndex = Table.GetTypeIndex(type);
        if (Table.ItemTypeKey != null && typeIndex > 0)
        {
            return CreateParam(CompareOperator.AndAlso, Table.ItemTypeKey, CompareOperator.Equal, typeIndex);
        }
        return null;
    }

    public QQuery<T> Where(Expression<Func<T, bool>> expression)
    {
        Parameters.Add(this.ParseParameter(expression.Body));
        return this;
    }

    public QQuery<T> Where(string filter)
    {
        if (Table == null)
            throw new InvalidOperationException();
        //Parameters.Clear();
        ParseParameters(filter.AsSpan(), Parameters);

        return this;
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

    public IModelTable ParseTable(string word)
    {
        return Schema.GetTable(word) ?? Schema.GetTableByTypeName(word);
    }

    public IModelTable ParseTable(Type type)
    {
        return Schema.GetTable(type);
    }

    public IQueryInvoker ParseInvoker(string word, string prefix, out IQTable qTable)
    {
        qTable = null;
        prefix = prefix == null && word.IndexOf('.') > -1 ? word.Substring(0, word.IndexOf('.')) : prefix;
        if (prefix == null)
        {
            foreach (var table in Tables)
            {
                var column = table.Table.GetColumn(word);
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
                || prefix.Equals(table.Table.ItemType.Name, StringComparison.OrdinalIgnoreCase))
                {
                    var column = table.Table.GetColumn(word);
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
            var column = q.ParseInvoker(word, prefix, out qTable);
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

    public void Parse(string? query)
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
        IQCompareParameter parameter = null;
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
                    if (MemoryExtensions.Equals(word, QueryHelper.StrSelect.AsSpan(), StringComparison.OrdinalIgnoreCase))
                    {
                        state = QParserState.Select;
                    }
                    else if (MemoryExtensions.Equals(word, QueryHelper.StrFrom.AsSpan(), StringComparison.OrdinalIgnoreCase))
                    {
                        prefix.Clear();
                        state = QParserState.From;
                    }
                    else if (MemoryExtensions.Equals(word, QueryHelper.StrWhere.AsSpan(), StringComparison.OrdinalIgnoreCase))
                    {
                        prefix.Clear();
                        state = QParserState.Where;
                    }
                    else if (MemoryExtensions.Equals(word, QueryHelper.StrOrder.AsSpan(), StringComparison.OrdinalIgnoreCase))
                    {
                        state = QParserState.OrderBy;
                    }
                    else if (MemoryExtensions.Equals(word, QueryHelper.StrGroup.AsSpan(), StringComparison.OrdinalIgnoreCase))
                    {
                        state = QParserState.GroupBy;
                    }
                    else if (MemoryExtensions.Equals(word, QueryHelper.StrBy.AsSpan(), StringComparison.OrdinalIgnoreCase))
                    {

                    }
                    else
                    {
                        switch (state)
                        {
                            case QParserState.Select:
                                if (word.Length > 0)
                                {
                                    if (MemoryExtensions.Equals(word, QueryHelper.StrAs.AsSpan(), StringComparison.OrdinalIgnoreCase) && c == ' ')
                                    {
                                        alias = true;
                                    }
                                    else if (MemoryExtensions.Equals(word, QueryHelper.StrNull.AsSpan(), StringComparison.OrdinalIgnoreCase))
                                    {
                                        Columns.Add(new QObjectValue(null));
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
                                            var fn = Function.Parse(wordStr);
                                            if (fn != FunctionType.None)
                                            {
                                                Columns.Add(new QFunction(fn));
                                            }
                                            else
                                            {
                                                var dbColumn = ParseInvoker(wordStr, prefix.LastOrDefault(), out var qTable);
                                                if (dbColumn != null)
                                                {
                                                    Columns.Add(new IQInvoker(dbColumn)
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
                                    else if (MemoryExtensions.StartsWith(word2, QueryHelper.StrSelect.AsSpan(), StringComparison.OrdinalIgnoreCase))
                                    {
                                        Columns.Add(new QQuery<object>(this, word2));
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
                                        Columns.Add(new QValue<string>(wordStr));
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
                                if (parameter != null && parameter.Holder == null)
                                    Parameters.Add(parameter);
                                break;
                            case QParserState.OrderBy:
                                if (word.Length > 0)
                                {

                                    if (MemoryExtensions.Equals(word, QueryHelper.StrAsc.AsSpan(), StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (order != null)
                                        {
                                            order.Direction = ListSortDirection.Ascending;
                                            order = null;
                                        }
                                    }
                                    else if (MemoryExtensions.Equals(word, QueryHelper.StrDesc.AsSpan(), StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (order != null)
                                        {
                                            order.Direction = ListSortDirection.Descending;
                                            order = null;
                                        }
                                    }
                                    else if (MemoryExtensions.Equals(word, QueryHelper.StrIGroup.AsSpan(), StringComparison.OrdinalIgnoreCase))
                                    {
                                        TreeComparer = GroupHelper.GetTreeInvoker(typeof(T)).CreateTreeComparer(typeof(T));
                                    }
                                    else
                                    {
                                        var wordStr = word.ToString();
                                        var dbColumn = ParseInvoker(wordStr, prefix.LastOrDefault(), out var qTable);
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
                                            var invoker = InvokeProvider.GetInvoker(Table.ItemType, property);
                                            if (invoker != null)
                                            {
                                                order = new QOrder
                                                {
                                                    Item = new IQInvoker(invoker) { QTable = QTable }
                                                };
                                                Orders.Add(order);
                                                prefix.Clear();
                                            }
                                        }
                                        else
                                        {
                                            var invoker = InvokeProvider.GetInvoker(Table.ItemType, wordStr);
                                            if (invoker != null)
                                            {
                                                order = new QOrder
                                                {
                                                    Item = new IQInvoker(invoker)
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

    private void ParseParameter(ReadOnlySpan<char> query, ref IQParameter parameter, ReadOnlySpan<char> word, ref int i, char c, List<string> prefix)
    {
        if (word.Length == 0)
        {
            return;
        }
        if (parameter == null || parameter.IsContainer || parameter.IsFilled)
        {
            var tempParameter = parameter;
            if (tempParameter?.IsContainer ?? false)
            {
                parameter = new QCompareParameter();
                tempParameter.Add(parameter);
            }
            else if (tempParameter?.Group?.IsCompaund ?? false)
                tempParameter.Group.Add(parameter);
        }

        if (Helper.IsDecimal(word, out var decimalValue))
        {
            parameter.Add(new QObjectValue(word.ToString(), parameter.LeftProperty, parameter.CompareOperator.Type));
        }
        else
        {
            var cm = CompareOperator.Parse(word);
            if (cm != CompareOperatorType.Undefined)
            {
                if (cm == CompareOperatorType.Not
                    && parameter.LeftItem == null
                    && parameter.Logic.Type != CompareOperatorType.Undefined)
                    parameter.Logic = new CompareOperator(parameter.Logic.Type, true);
                else if (cm == CompareOperatorType.True
                    && (parameter.LeftItem == null
                    || (parameter.CompareOperator.Type & CompareOperatorType.Is) != CompareOperatorType.Is))
                    parameter.Add(QItem.CreateConst(true, parameter.LeftProperty, parameter.CompareOperator.Type));
                else if (cm == CompareOperatorType.False
                        && (parameter.LeftItem == null
                            || (parameter.CompareOperator.Type & CompareOperatorType.Is) != CompareOperatorType.Is))
                    parameter.Add(QItem.CreateConst(false, parameter.LeftProperty, parameter.CompareOperator.Type));
                else if (cm == CompareOperatorType.Null
                        && (parameter.LeftItem == null
                            || (parameter.CompareOperator.Type & CompareOperatorType.Is) != CompareOperatorType.Is))
                    parameter.Add(new QObjectValue(null));
                else
                    parameter.CompareOperator = new CompareOperator(parameter.CompareOperator.Type | cm);
            }
            else
            {
                var lg = CompareOperator.Parse(word);
                if (lg != CompareOperatorType.Undefined)
                {
                    if (parameter.CompareOperator.Type == CompareOperatorType.Between && lg == CompareOperatorType.AndAlso)
                    {
                        parameter.RightItem = new QBetween(parameter.RightItem, null, parameter.LeftProperty);
                    }
                    else
                    {
                        parameter.Logic = new CompareOperator(lg);
                    }
                    prefix.Clear();
                }
                else
                {
                    var fn = Function.Parse(word);
                    if (fn != FunctionType.None)
                    {
                        parameter.Add(new QFunction(Function.Fabrica(fn)));
                        prefix.Clear();
                    }
                    else
                    {
                        var wordStr = word.ToString();
                        var wcolumn = ParseInvoker(wordStr, prefix.LastOrDefault(), out var qTable);
                        if (wcolumn != null)
                        {
                            parameter.Add(Fabric(wcolumn, parameter.LeftProperty, parameter.CompareOperator.Type, qTable));
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
                                        var dbColumn = pTable.Table.GetColumn(pcolumn);

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
                                    var lastColumn = pTable.Table.GetColumn(wordStr);
                                    parameter.Add(new QColumn(lastColumn)
                                    {
                                        QTable = pTable
                                    });
                                    prefix.Clear();
                                }
                                else
                                {
                                    var invoker = InvokeProvider.GetInvoker(Table.ItemType, wordStr);
                                    if (invoker != null)
                                    {
                                        parameter.Add(new IQInvoker(invoker));
                                        prefix.Clear();
                                    }
                                }
                                if (parameter.LeftItem == null)
                                    throw new Exception($"Column '{wordStr}' not found in table {Table}");
                            }
                            else// if (parameter.Column != null)
                            {
                                parameter.Add(QItemExtensions.CreateItem(wordStr, parameter.LeftProperty, parameter.CompareOperator.Type));
                                prefix.Clear();
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
                parameter.CompareOperator = CompareOperator.NotEqual;
                break;
            case '=':
                parameter.CompareOperator = CompareOperator.Equal;
                break;
            case '>':
                if (query[i + 1] == '=')
                {
                    i++;
                    parameter.CompareOperator = CompareOperator.GreaterOrEqual;
                }
                else
                    parameter.CompareOperator = CompareOperator.Greater;
                break;
            case '<':
                if (query[i + 1] == '=')
                {
                    i++;
                    parameter.CompareOperator = CompareOperator.LessOrEqual;
                }
                else if (query[i + 1] == '>')
                {
                    i++;
                    parameter.CompareOperator = CompareOperator.NotEqual;
                }
                else
                    parameter.CompareOperator = CompareOperator.Less;
                break;
            case '\'':
            case '"':
                parameter.Add(QItemExtensions.CreateItem(query.GetSubPart(ref i, c, c).ToString(), parameter));
                break;
            case '(':
                int tempIndex = i;
                var word2 = query.GetSubPart(ref i, '(', ')').Trim();
                if (MemoryExtensions.StartsWith(word2, QueryHelper.StrSelect.AsSpan(), StringComparison.OrdinalIgnoreCase))
                {
                    parameter.Add(new QQuery<DBItem>(this, word2));
                }
                else if (parameter != null && parameter.Parameters.LastOrDefault() is QFunction qFunc)
                {
                    ParseFunction(qFunc, word2);
                }
                else if (parameter != null && parameter.CompareOperator.Type != CompareOperatorType.Undefined)
                {
                    var list = new QArray();
                    foreach (var s in word2.Split(Helper.CommaSeparator))
                    {
                        var entry = s.Trim(' ', '\'');
                        if (string.Equals(entry, QueryHelper.StrNull, StringComparison.Ordinal))
                            continue;
                        list.Add(new QValue(entry, parameter.LeftProperty));
                    }

                    parameter.Add(list);
                    if (parameter.CompareOperator.Type == CompareOperatorType.Between && parameter.RightItem is QArray)
                    {
                        var qEnum = (QArray)parameter.RightItem;
                        var between = new QBetween(qEnum[0], qEnum[1], parameter.LeftProperty);
                        parameter.RightItem = between;
                    }
                }
                else
                {
                    i = tempIndex;
                    if (parameter != null
                        && parameter.LeftItem == null
                        && parameter.Logic != CompareOperator.Undefined)
                    {
                        parameter.IsCompaund = true;
                        break;
                    }
                    var tempParameter = parameter;
                    parameter = new QCompareParameter() { IsCompaund = true };
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

    private void ParseParameters(ReadOnlySpan<char> query, IQParameter parameter)
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
        IQTable? table = null;
        var prefix = new List<string>();
        var word = ReadOnlySpan<char>.Empty;
        IQCompareParameter? parameter = null;

        var joinType = JoinOperatorType.Undefined;

        var fromIndex = QueryHelper.IndexOfFrom(query);
        if (fromIndex < 0)
            return;
        int startIndex = fromIndex + 5;
        for (int i = startIndex; i <= query.Length; i++)
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
                    if (MemoryExtensions.Equals(word, QueryHelper.StrWhere.AsSpan(), StringComparison.OrdinalIgnoreCase))
                        return;
                    var join = JoinOperator.Parse(word);
                    if (join != JoinOperatorType.Undefined)
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
                    else if (MemoryExtensions.Equals(word, QueryHelper.StrOn.AsSpan(), StringComparison.OrdinalIgnoreCase))
                    {
                        table.On = parameter = new QCompareParameter();
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
                                if (Table != tb || joinType != JoinOperatorType.Undefined)
                                {
                                    table = new QTable(tb) { Join = new JoinOperator(joinType) };
                                    Tables.Add(table);
                                }
                                else
                                {
                                    table = QTable;
                                }
                                joinType = JoinOperatorType.Undefined;
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
                    expression.Add(MathOperator.Fabric(c));
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

                            if (fn != FunctionType.None)
                            {
                                func = new QFunction(fn);
                                expression.Add(func);
                            }
                            else
                            {
                                var wordStr = word.ToString();
                                var column = ParseInvoker(wordStr, prefix.LastOrDefault(), out var qTable);
                                if (column != null)
                                {
                                    expression.Add(new IQInvoker(column) { QTable = qTable });
                                    prefix.Clear();
                                }
                                else
                                {
                                    expression.Add(new QValue(wordStr));
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
                        if (MemoryExtensions.StartsWith(word2, QueryHelper.StrSelect.AsSpan(), StringComparison.OrdinalIgnoreCase))
                        {
                            expression.Add(new QQuery<object>(this, word2));
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
                            expression.Add(ex);
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
                            function.Add(new QValue<decimal>(decimal.Parse($"{prefix[0]}.{word.ToString()}", CultureInfo.InvariantCulture)));
                            prefix.Clear();
                        }
                        else
                        {
                            function.Add(new QValue<decimal>(decimal.Parse(word.ToString())));
                        }
                    }
                    else
                    {
                        var comp = CompareOperator.Parse(word);
                        if (comp != CompareOperatorType.Undefined)
                        {
                            function.Add(new QCompareOperatorType(comp));
                        }
                        else
                        {
                            var type = QType.ParseType(word);
                            if (type != QType.None)
                            {
                                qtype = type;
                                function.Add(type);
                            }
                            else
                            {
                                var fn = Function.Parse(word);
                                if (fn != FunctionType.None)
                                {
                                    func = new QFunction(fn);
                                    function.Add(func);
                                }
                                else
                                {
                                    var wordStr = word.ToString();
                                    var col = ParseInvoker(wordStr, prefix.LastOrDefault(), out var qTable);
                                    if (col != null)
                                    {
                                        function.Add(new IQInvoker(col) { QTable = qTable });
                                        prefix.Clear();
                                    }
                                    else
                                    {
                                        function.Add(new QValue(wordStr));
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
                    if (MemoryExtensions.StartsWith(word2, QueryHelper.StrSelect.AsSpan(), StringComparison.OrdinalIgnoreCase))
                    {
                        function.Add(new QQuery<object>(this, word2));
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
                        function.Add(ex);
                    }
                }
                else if (c == '\'')
                {
                    var literal = query.GetSubPart(ref i, '\'', '\'');
                    function.Add(new QValue<string>(literal.ToString()));
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

    public bool IsJoinAffectResult => Tables.Count > 1
        && Tables.Skip(1).Any(table => table.Join.IsInner
                            || table.Join.IsRight
                            || table.HasParameters);

    public QCompareParameter NewParam() => NewParam(CompareOperator.Undefined);

    public QCompareParameter NewParam(CompareOperator logic)
    {
        var param = new QCompareParameter
        {
            Logic = logic
        };
        Parameters.Add(param);
        return param;
    }

    public QQuery<T> Column(FunctionType type, params object[] args)
    {
        Columns.Add(CreateFunc(type, args));
        return this;
    }

    public QQuery<T> Column(IQueryInvoker invoker, string prefix = null)
    {
        Columns.Add(QItemExtensions.CreateColumn(invoker, prefix));
        return this;
    }

    public QQuery<T> Column<K>(Expression<Func<T, K>> keySelector)
    {
        Columns.Add(QItemExtensions.CreateColumn(keySelector.Body));
        return this;
    }

    public QQuery<T> Column(string textConstant)
    {
        Columns.Add(QItemExtensions.CreateItem(textConstant, null));
        return this;
    }

    public QQuery<T> Join(IQueryInvoker column)
    {
        var join = CreateJoin(column);
        if (!Tables.Contains(join))
            Tables.Add(join);
        return this;
    }

    public QQuery<T> Join(IQueryInvoker column, IQueryInvoker refColumn)
    {
        var join = CreateJoin(column, refColumn);
        if (!Tables.Contains(join))
            Tables.Add(join);
        return this;
    }

    public QQuery<T> Join(JoinOperator type, IQueryInvoker column, string alias, IQueryInvoker refColumn, string refAlias)
    {
        var join = CreateJoin(type, column, alias, refColumn, refAlias);
        if (!Tables.Contains(join))
            Tables.Add(join);
        return this;
    }

    public QQuery<T> OrderBy(IQueryInvoker invoker, ListSortDirection direction = ListSortDirection.Ascending)
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

    public QQuery<T> Where(QCompareParameter param)
    {
        Parameters.Add(param);
        return this;
    }

    public QQuery<T> Where(Action<QCompareParameter> parameterGroup)
    {
        var qParam = new QCompareParameter() { Logic = CompareOperator.Undefined };
        Parameters.Add(qParam);

        parameterGroup(qParam);
        return this;
    }

    public QQuery<T> Where(string column, CompareOperator compare, object param)
    {
        Parameters.Add(CreateParam(CompareOperator.Undefined, column, compare, param));
        return this;
    }

    public QQuery<T> Where(IQueryInvoker invoker, object param, QBuildParam buildParam = QBuildParam.None)
    {
        Parameters.Add(QItemExtensions.CreateParameter(invoker, param, buildParam));
        return this;
    }

    public QQuery<T> Where(IQueryInvoker invoker, CompareOperator compare, object param)
    {
        Parameters.Add(QItemExtensions.CreateParameter(invoker, compare, param, Source));
        return this;
    }

    public QQuery<T> And(QCompareParameter param)
    {
        param.Logic = CompareOperator.AndAlso;
        Parameters.Add(param);
        return this;
    }

    public QQuery<T> And(Action<QCompareParameter> parameterGroup)
    {
        var qParam = new QCompareParameter() { Logic = CompareOperator.AndAlso };
        parameterGroup(qParam);
        Parameters.Add(qParam);
        return this;
    }

    public QQuery<T> And(string column, CompareOperator compare, object param)
    {
        Parameters.Add(CreateParam(CompareOperator.AndAlso, column, compare, param));
        return this;
    }

    public QQuery<T> And(IQueryInvoker invoker, object param, QBuildParam buildParam = QBuildParam.None)
    {
        Parameters.Add(CreateParam(CompareOperator.AndAlso, invoker, param, buildParam));
        return this;
    }

    public QQuery<T> And(IQueryInvoker invoker, CompareOperator compare, object param)
    {
        Parameters.Add(CreateParam(CompareOperator.AndAlso, invoker, compare, param));
        return this;
    }

    public QQuery<T> Or(QCompareParameter param)
    {
        param.Logic = CompareOperator.Or;
        Parameters.Add(param);
        return this;
    }

    public QQuery<T> Or(Action<QCompareParameter> parameterGroup)
    {
        var qParam = new QCompareParameter() { Logic = CompareOperator.Or };
        parameterGroup(qParam);
        Parameters.Add(qParam);
        return this;
    }

    public QQuery<T> Or(string column, CompareOperator compare, object param)
    {
        Parameters.Add(CreateParam(CompareOperator.Or, column, compare, param));
        return this;
    }

    public QQuery<T> Or(IQueryInvoker invoker, object param, QBuildParam buildParam = QBuildParam.None)
    {
        Parameters.Add(CreateParam(CompareOperator.Or, invoker, param, buildParam));
        return this;
    }

    public QQuery<T> Or(IQueryInvoker invoker, CompareOperator compare, object param)
    {
        Parameters.Add(CreateParam(CompareOperator.Or, invoker, compare, param));
        return this;
    }

    public List<R> Sort<R>(List<R> list)
    {
        var comparer = GetComparer<R>();
        if (comparer != null)
        {
            ListHelper.QuickSort<R>(list, comparer);
        }
        return list;
    }

    public virtual IComparerList<R> GetComparer<R>()
    {
        if (orders == null
            || orders.Count == 0)
            return null;
        var comparer = new InvokerComparerList<R>();
        foreach (QOrder order in Orders)
        {
            var comparerEntry = order.CreateComparer<R>();
            if (comparerEntry != null)
            {
                comparer.Add(comparerEntry);
            }
        }
        return comparer.Count == 0 ? null : comparer;
    }

    public override string ToString()
    {
        string rez = "Query ";
        if (Table != null)
            rez += Table.ToString();
        return rez;
    }

    public string GenerateTableAlias(IEnumerable table)
    {
        var topQuery = GetTopQuery();
        var tables = topQuery.GetAllItems<QTable>();

        return tables.Count().IntToChar();
    }

    public string GenerateTableAlias(Type tableType)
    {
        var topQuery = GetTopQuery();
        var tables = topQuery.GetAllItems<QTable>();

        return tables.Count().IntToChar();
    }

    protected virtual QTable GetTable(IQueryInvoker column) => GetTable(column?.TargetType);

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

    public IQTable GetTable(IModelTable table)
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

    public IQTable GetTableByAlias(string alias)
    {
        if (alias == null)
            return null;
        return tables?.FirstOrDefault(p => string.Equals(alias, p.TableAlias, StringComparison.OrdinalIgnoreCase));
    }

    public QFunction CreateFunc(FunctionType type, params object[] args)
    {
        return new QFunction(type, args);
    }

    public IEnumerable<IQInvoker> CreateColumns(IEnumerable<IInvoker> cols)
    {
        foreach (var col in cols)
        {
            yield return CreateInvoker(col);
        }
    }

    public IQTable CreateJoin(IModelColumn refColumn)
    {
        return CreateJoin(refColumn.DataType, refColumn);
    }

    public IQTable CreateJoin(Type tableType, IModelColumn refColumn)
    {
        return CreateJoin(ParseTable(tableType), refColumn);
    }

    public IQTable CreateJoin(IModelTable table, IModelColumn refColumn)
    {
        return CreateJoin(table.PrimaryKey, refColumn);
    }

    public IQTable CreateJoin(IModelColumn column, IModelColumn refColumn)
    {
        return CreateJoin(JoinOperator.Left, column, GenerateTableAlias(column.TargetType), refColumn, GetTable(refColumn.TargetType).TableAlias);
    }

    public IQTable CreateJoin(JoinOperator joinType, IModelTable table, string alias, IModelColumn refColumn, string refAlias)
    {
        return CreateJoin(joinType, table.PrimaryKey, alias, refColumn, refAlias);
    }

    public IQTable CreateJoin(JoinOperator joinType, IModelColumn column, string alias, IModelColumn refColumn, string refAlias)
    {
        var joinTable = Tables.FirstOrDefault(p => p.Table == column.Table
                                            && p.On != null
                                            && p.On.IsProperty(column)
                                            && p.On.IsProperty(refColumn));
        if (joinTable != null)
            return joinTable;
        joinTable = column.Table.CreateQTable(alias);
        joinTable.Join = joinType;
        joinTable.On = new QCompareParameter
        {
            LeftItem = column.CreateColumn(alias),
            Comparer = CompareOperator.Equal,
            RightItem = refColumn.CreateColumn(refAlias)
        };

    }

    public QCompareParameter CreateNameParam(CompareOperator logic, string property, CompareOperator comparer, object value)
    {
        var parameter = new QCompareParameter { Logic = logic };
        foreach (var column in Table.Columns.GetByGroup(property))
        {
            parameter.Parameters.Add(CreateParam(CompareOperator.Or, column, comparer, value));
        }
        return parameter;
    }

    public QCompareParameter CreateParam(CompareOperator logic, string column, object value, QBuildParam buildParam = QBuildParam.None)
    {
        var comparer = this.DetectComparer(null, ref value, buildParam);
        return CreateParam(logic, column, comparer, value);
    }

    public QCompareParameter CreateParam(CompareOperator logic, string column, CompareOperator comparer, object value)
    {
        int index = column.IndexOf('.');
        if (index >= 0)
        {
            string[] split = column.Split(Helper.DotSeparator);//TODO JOIN
            var qTable = QTable;
            var table = Table;
            IModelColumn refColumn = null;
            for (int i = 0; i < split.Length; i++)
            {
                refColumn = table.GetColumn(split[i]);
                if (refColumn != null)
                {
                    ///q.Columns.Add(new QColumn(column.ReferenceTable.PrimaryKey.Code));
                    if (refColumn.IsReference && i + 1 < split.Length)
                    {
                        table = ParseTable(refColumn.TargetType);
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
            var dbColumn = ParseInvoker(column, null, out var qTable);
            if (dbColumn == null)
                return CreateNameParam(logic, column, comparer, value);
            return CreateParam(logic, dbColumn, comparer, value);
        }
    }

    public QCompareParameter CreateParam(CompareOperator logic, IQueryInvoker column, CompareOperator compare, object value)
    {
        var alias = QTable?.TableAlias;
        if (column.TargetType != Table.ItemType)
        {
            var table = GetTable(column);
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

        return new QCompareParameter(alias)
        {
            LeftItem = CreateColumn(column, alias),
            Comparer = compare,
            RightItem = QItem.Fabric(value, column)
        }
        ;
    }

    public QCompareParameter CreateParam(CompareOperator logic, IQueryInvoker invoker, CompareOperator comparer, object value)
    {
        return new QCompareParameter
        {
            LeftItem = invoker.CreateQInvoker(),
            Comparer = comparer,
            RightItem = QItemExtensions.CreateItem(value, invoker)
        };
    }

    public QCompareParameter CreateParam(CompareOperator logic, IQueryInvoker invoker, object value, QBuildParam buildParam = QBuildParam.None)
    {
        var comparer = DetectComparer(invoker, ref value, buildParam);
        return CreateParam(logic, invoker, comparer, value);
    }

    public virtual QCompareParameter CreateParam(CompareOperator logic, IQueryInvoker parent, IQueryInvoker column, object p, QBuildParam buildParam)
    {
        var table = ParseTable(column.TargetType);
        var query = Join(table.PrimaryKey, parent);
        return CreateParam(logic, column, p, buildParam);
    }










    protected CompareOperator DetectComparer(IQueryInvoker column, ref object value, QBuildParam buildParam)
    {
        var valueType = value?.GetType();
        var type = column?.DataType ?? valueType;
        var comparer = CompareOperator.Equal;
        if (value is IQQuery)
        {
            comparer = CompareOperator.In;
        }
        else if (value is IPrimaryKey dbItem)
        {
            value = dbItem.PrimaryKey;
        }
        else if (type == typeof(string)
            && value is string stringValue)
        {
            if ((buildParam & QBuildParam.AutoLike) != 0)
            {
                comparer = CompareOperator.Like;
                if (!stringValue.Contains('%'))
                    value = $"%{stringValue}%";
            }
            else if ((buildParam & QBuildParam.SplitString) != 0
                && stringValue.Contains(',') == true)
            {
                comparer = CompareOperator.In;
                value = value.ToString().Split(Helper.CommaSeparator, StringSplitOptions.RemoveEmptyEntries);
            }
        }
        else if (value is IBetween)
        {
            comparer = CompareOperator.Between;
        }
        else if ((valueType?.IsList() ?? false) && valueType != type)
        {
            comparer = CompareOperator.In;
        }


        return comparer;
    }

    public IEnumerable<IQCompareParameter> GetAllParameters(Func<IQCompareParameter, bool> predicate = null)
    {
        return Parameters.GetAllItems<IQCompareParameter>(predicate);
    }

    public bool Compare(object item)
    {
        return parameters.Compare(item);
    }

    public override string Format(object context = null)
    {
        return FormatAll(context);
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
        resultBuilder.Append(QueryHelper.StrSelect);
        resultBuilder.Append(' ');
        resultBuilder.Append(cols);
        resultBuilder.Append('\n');
        resultBuilder.Append(subSpace);
        resultBuilder.Append(QueryHelper.StrFrom);
        resultBuilder.Append(partFrom);
        if (partWhere.Length > 0)
        {
            resultBuilder.Append('\n');
            resultBuilder.Append(subSpace);
            resultBuilder.Append(QueryHelper.StrWhere);
            resultBuilder.Append(' ');
            resultBuilder.Append(partWhere);
        }
        if (partOrder.Length > 0)
        {
            resultBuilder.Append('\n');
            resultBuilder.Append(subSpace);
            resultBuilder.Append(QueryHelper.StrOrder);
            resultBuilder.Append(' ');
            resultBuilder.Append(QueryHelper.StrBy);
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
                        cols.Append('(');
                    }
                    cols.Append(temp);
                    if (col is IQQuery)
                    {
                        cols.Append(')');
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
            QCompareParameter param = parameters[i];
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
            foreach (QCompareParameter param in Table.FilterQuery.Parameters)
            {
                if (Contains(param.LeftProperty))
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
                buf.Append(')');
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
        foreach (QCompareParameter param in Parameters)
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
        return parameters?.Select(QCompareParameter.ColumnNameInvoker.Instance, CompareOperator.Equal, column).Any() ?? false;
    }

    public bool Contains(IQueryInvoker column)
    {
        return GetByColumn(column) != null;
    }

    public QCompareParameter GetByColumn(IQueryInvoker column)
    {
        if (parameters == null)
            return null;
        foreach (QCompareParameter p in parameters)
            if (p.IsProperty(column))
                return p;
        return null;
    }

    public void RemoveParameter(IQueryInvoker column)
    {
        for (int i = 0; i < Parameters.Count;)
        {
            QCompareParameter p = Parameters[i];
            if (p.IsProperty(column))
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
            foreach (QCompareParameter param in GetAllParameters())
            {
                if (param.RightProperty is var rColumn
                    && rColumn.Table == item.Table)
                {
                    param.RightQProperty.Value = item.GetValue(rColumn);
                }
                if (param.LeftProperty is var lColumn
                    && lColumn.Table == item.Table)
                {
                    param.LeftQProperty.Value = item.GetValue(lColumn);
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
        var param = Holder as QCompareParameter;
        var column = param.LeftProperty ?? param.RightProperty;
        var comparer = param.CompareOperator;
        if ((column?.IsPrimaryKey ?? false) && Enumerable.FirstOrDefault(Columns) is QColumn qcolumn)
        {
            var buf = new List<R>();
            foreach (var item in Select())
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

    public bool Compare<T>(T item)
    {
        return QCompareParameter.CheckItem(item, Parameters);
    }

    public void Add(IQItem item)
    {
        if (item is QColumn column)
            Columns.Add(column);
        else if (item is QCompareParameter param)
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
        else if (item is QCompareParameter param)
            Parameters.Remove(param);
        else if (item is QOrder order)
            Orders.Remove(order);
        else if (item is QTable table)
            Tables.Remove(table);
    }

    public IEnumerable<IT> GetAllItems<IT>(Func<IT, bool>? predicate = null) where IT : IQItem
    {
        var result = Enumerable.Empty<IT>();
        if (columns != null)
            result = columns.GetAllItems<IT>(predicate);
        if (tables != null)
            result = result.Concat(tables.GetAllItems<IT>(predicate));
        if (parameters != null)
            result = result.Concat(parameters.GetAllItems<IT>(predicate));
        if (orders != null)
            result = result.Concat(orders.GetAllItems<IT>(predicate));
        return result;
    }

    public IEnumerable<TT> Select<TT>() where TT : T => Table.Select<TT>(this);

    public IEnumerable<T> Select() => QCompareParameter.Select(TTable, Parameters);

    private IEnumerable<QTuple> AsTuple(IEnumerable<T> items = null)
    {
        foreach (var item in items)
        {
            yield return new QTuple { Item0 = item };
        }
    }

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

    IQQuery IQQuery.Column(FunctionType function, params object[] args) => Column(function, args);
    IQQuery IQQuery.Column(IQueryInvoker invoker) => Column(invoker);

    IQQuery IQQuery.Join(IQueryInvoker column) => Join(column);
    IQQuery IQQuery.Join(IQueryInvoker column, IQueryInvoker refColumn) => Join(column, refColumn);

    IQQuery IQQuery.Where(string text) => Where(text);
    IQQuery IQQuery.Where(Type typeFilter) => Where(typeFilter);
    IQQuery IQQuery.Where(QCompareParameter qParam) => Where(qParam);

    IQQuery IQQuery.Where(Action<QCompareParameter> qParam) => Where(qParam);
    IQQuery IQQuery.Where(IQueryInvoker invoker, object value, QBuildParam param) => Where(invoker, value, param);
    IQQuery IQQuery.Where(IQueryInvoker invoker, CompareOperator comparer, object value) => Where(invoker, comparer, value);
    IQQuery IQQuery.Where(string column, CompareOperator comparer, object value) => Where(column, comparer, value);

    IQQuery IQQuery.And(QCompareParameter qParam) => And(qParam);
    IQQuery IQQuery.And(Action<QCompareParameter> qParam) => And(qParam);
    IQQuery IQQuery.And(IQueryInvoker invoker, object value, QBuildParam param) => And(invoker, value, param);
    IQQuery IQQuery.And(IQueryInvoker invoker, CompareOperator comparer, object value) => And(invoker, comparer, value);
    IQQuery IQQuery.And(string column, CompareOperator comparer, object value) => And(column, comparer, value);

    IQQuery IQQuery.OrWhere(QCompareParameter qParam) => Or(qParam);
    IQQuery IQQuery.OrWhere(Action<QCompareParameter> qParam) => Or(qParam);
    IQQuery IQQuery.OrWhere(IQueryInvoker invoker, object value, QBuildParam param) => Or(invoker, value, param);
    IQQuery IQQuery.OrWhere(IQueryInvoker invoker, CompareOperator comparer, object value) => Or(invoker, comparer, value);
    IQQuery IQQuery.OrWhere(string column, CompareOperator comparer, object value) => Or(column, comparer, value);

    IQQuery IQQuery.OrderBy(IQueryInvoker invoker, ListSortDirection direction) => OrderBy(invoker, direction);

    IQQuery<T> IQQuery<T>.Column(FunctionType function, params object[] args) => Column(function, args);
    IQQuery<T> IQQuery<T>.Column(IQueryInvoker invoker) => Column(invoker);
    IQQuery<T> IQQuery<T>.Column<K>(Expression<Func<T, K>> keySelector) => Column(keySelector);

    IQQuery<T> IQQuery<T>.Join(IQueryInvoker column) => Join(column);
    IQQuery<T> IQQuery<T>.Join(IQueryInvoker column, IQueryInvoker refColumn) => Join(column, refColumn);

    IQQuery<T> IQQuery<T>.Where(string text) => Where(text);
    IQQuery<T> IQQuery<T>.Where(Expression<Func<T, bool>> expression) => Where(expression);
    IQQuery<T> IQQuery<T>.Where(Type typeFilter) => Where(typeFilter);
    IQQuery<T> IQQuery<T>.Where(QCompareParameter qParam) => Where(qParam);
    IQQuery<T> IQQuery<T>.Where(Action<QCompareParameter> qParam) => Where(qParam);
    IQQuery<T> IQQuery<T>.Where(IQueryInvoker invoker, object value, QBuildParam param) => Where(invoker, value, param);
    IQQuery<T> IQQuery<T>.Where(IQueryInvoker invoker, CompareOperator comparer, object value) => Where(invoker, comparer, value);
    IQQuery<T> IQQuery<T>.Where(string invoker, CompareOperator comparer, object value) => Where(invoker, comparer, value);

    IQQuery<T> IQQuery<T>.And(QCompareParameter qParam) => And(qParam);
    IQQuery<T> IQQuery<T>.And(Action<QCompareParameter> qParam) => And(qParam);
    IQQuery<T> IQQuery<T>.And(IQueryInvoker invoker, object value, QBuildParam param) => And(invoker, value, param);
    IQQuery<T> IQQuery<T>.And(IQueryInvoker invoker, CompareOperator comparer, object value) => And(invoker, comparer, value);
    IQQuery<T> IQQuery<T>.And(string invoker, CompareOperator comparer, object value) => And(invoker, comparer, value);

    IQQuery<T> IQQuery<T>.Or(QCompareParameter qParam) => Or(qParam);
    IQQuery<T> IQQuery<T>.Or(Action<QCompareParameter> qParam) => Or(qParam);
    IQQuery<T> IQQuery<T>.Or(IQueryInvoker invoker, object value, QBuildParam param) => Or(invoker, value, param);
    IQQuery<T> IQQuery<T>.Or(IQueryInvoker invoker, CompareOperator comparer, object value) => Or(invoker, comparer, value);
    IQQuery<T> IQQuery<T>.Or(string invoker, CompareOperator comparer, object value) => Or(invoker, comparer, value);

    IQQuery<T> IQQuery<T>.OrderBy(IQueryInvoker invoker, ListSortDirection direction) => OrderBy(invoker, direction);
    IQQuery<T> IQQuery<T>.OrderBy<K>(Expression<Func<T, K>> keySelector) => OrderBy(keySelector);
    IQQuery<T> IQQuery<T>.OrderByDescending<K>(Expression<Func<T, K>> keySelector) => OrderByDescending(keySelector);

}
public static class QueryHelper
{
    public static readonly string StrFrom = "from";
    public static readonly string StrOn = "on";
    public static readonly string StrSelect = "select";
    public static readonly string StrWhere = "where";
    public static readonly string StrOrder = "order";
    public static readonly string StrGroup = "group";
    public static readonly string StrBy = "by";
    public static readonly string StrAs = "as";
    public static readonly string StrTrue = "true";
    public static readonly string StrFalse = "false";
    public static readonly string StrNull = "null";
    public static readonly string StrNot = "not";
    public static readonly string StrAsc = "asc";
    public static readonly string StrDesc = "desc";
    public static readonly string StrIGroup = nameof(IGroup);

    public static int IndexOfFrom(this ReadOnlySpan<char> query) => IndexOfWord(query, StrFrom.AsSpan());

    public static int IndexOfWhere(this ReadOnlySpan<char> query) => IndexOfWord(query, StrWhere.AsSpan());

    public static int IndexOfWord(this ReadOnlySpan<char> query, ReadOnlySpan<char> specialWord)
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
                        && MemoryExtensions.Equals(word, specialWord, StringComparison.OrdinalIgnoreCase))
                        return i - specialWord.Length;
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
        return -1;
    }
}

