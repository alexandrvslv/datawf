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
using DataWF.Comparers;
using DataWF.Invokers;
using DataWF.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;

namespace DataWF.Collections.Query;

public interface IQQuery : IQItemList, IEnumerable, IQFrozenable, IQueryable
{
    QParamList Parameters { get; }
    QItemList<QItem> Columns { get; }
    QItemList<QOrder> Orders { get; }
    QTableList Tables { get; }
    //DBLoadParam LoadParam { get; set; }
    //DBCacheState CacheState { get; set; }
    //DBStatus StatusFilter { get; set; }
    IQQuery BaseQuery { get; set; }
    Type TypeFilter { get; set; }
    string WhereText { get; }
    string QueryText { get; }
    ITreeComparer TreeComparer { get; set; }
    bool IsJoinAffectResult { get; }
    bool IsNoParameters();

    IEnumerable<IQCompareParameter> GetAllParameters(Func<IQParameter, bool>? predicate = null);
    bool Contains(string columnName);
    bool Contains(IQueryInvoker column);

    string GenerateTableAlias(IModelTable table);
    IQTable GetTableByAlias(string alias);
    IQTable GetTable(IModelTable table);
    IQTable GetTable(Type itemType);

    IQueryInvoker ParseInvoker(string name, string? prefix, out IQTable qTable);
    IModelTable ParseTable(string name);

    bool CheckItem(object item);
    IQCompareParameter CreateParam(IQueryInvoker invoker, object value, QBuildParam buildParam = QBuildParam.None);
    IQCompareParameter CreateParam(IQueryInvoker invoker, CompareOperator comparer, object value);

    IQQuery<V> Select<V>(FunctionType function, params object[] args);
    IQQuery<V> Select<V>(IQueryInvoker<V> invoker);
    IQQuery<V> Select<V>(Expression keySelector);
    IEnumerable<T> Select<T>() where T : class;

    IQQuery Join(IQueryInvoker column);
    //IQQuery Join(DBReferencing referencing);
    //IQQuery JoinAllReferencing();
    IQQuery Join(IQueryInvoker column, IQueryInvoker refColumn);

    //IQQuery WhereViewColumns(string filter, QBuildParam buildParam = QBuildParam.AutoLike);
    IQQuery Where(string filter);
    IQQuery Where(Expression expression);
    IQQuery Where(Type typeFilter);
    IQQuery Where(IQCompareParameter parameter);
    IQQuery Where(Action<IQCompareParameter> parameter);
    IQQuery Where(IQueryInvoker column, object value, QBuildParam buildParam = QBuildParam.None);
    IQQuery Where(IQueryInvoker column, CompareOperator comparer, object value);
    IQQuery Where(string column, CompareOperator comparer, object value);

    IQQuery Or(IQCompareParameter parameter);
    IQQuery Or(Action<IQCompareParameter> parameter);
    IQQuery Or(IQueryInvoker column, object value, QBuildParam buildParam = QBuildParam.None);
    IQQuery Or(IQueryInvoker column, CompareOperator comparer, object value);
    IQQuery Or(string column, CompareOperator comparer, object value);

    IQQuery OrderBy(IQueryInvoker column, ListSortDirection direction = ListSortDirection.Ascending);

    IComparerList<T> GetComparer<T>();
    List<T> Sort<T>(List<T> ts);


    IEnumerable<T> Load<T>(object? transaction = null);

    T FirstOrDefault<T>(object? transaction = null);

    T FirstOrDefault<T>(Func<T, bool> predicate, object? transaction = null);

    void Parse(string test);
}

