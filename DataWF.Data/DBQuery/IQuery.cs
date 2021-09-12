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
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace DataWF.Data
{
    public interface IQuery<T> : IQuery, IEnumerable<T> where T : DBItem
    {
        IEnumerable<T> Select();

        new IQuery<T> Column(QFunctionType function, params object[] args);
        new IQuery<T> Column(IInvoker invoker);

        new IQuery<T> Where(string filter, QBuildParam buildParam = QBuildParam.AutoLike);
        new IQuery<T> Where(Type typeFilter);
        new IQuery<T> Where(QParam parameter);
        new IQuery<T> Where(Action<QParam> parameter);
        new IQuery<T> Where(IInvoker column, object value, QBuildParam buildParam = QBuildParam.None);
        new IQuery<T> Where(IInvoker column, CompareType comparer, object value);

        new IQuery<T> And(QParam parameter);
        new IQuery<T> And(Action<QParam> parameter);
        new IQuery<T> And(IInvoker column, object value, QBuildParam buildParam = QBuildParam.None);
        new IQuery<T> And(IInvoker column, CompareType comparer, object value);

        new IQuery<T> Or(QParam parameter);
        new IQuery<T> Or(Action<QParam> parameter);
        new IQuery<T> Or(IInvoker column, object value, QBuildParam buildParam = QBuildParam.None);
        new IQuery<T> Or(IInvoker column, CompareType comparer, object value);

        new IQuery<T> OrderBy(IInvoker column, ListSortDirection direction = ListSortDirection.Ascending);
    }

    public interface IQuery : IQItem, IEnumerable
    {
        QParamList Parameters { get; }
        QItemList<QItem> Columns { get; }
        QItemList<QOrder> Orders { get; }
        QItemList<QTable> Tables { get; }
        DBLoadParam LoadParam { get; set; }
        DBCacheState CacheState { get; set; }
        DBStatus StatusFilter { get; set; }
        Type TypeFilter { get; set; }
        string WhereText { get; }
        string QueryText { get; }

        string Format(IDbCommand command = null);
        IDbCommand ToCommand(bool defcolumns = false);

        bool IsNoParameters();
        IEnumerable<QParam> GetAllParameters();
        bool Contains(string columnName);
        bool Contains(DBColumn column);

        QTable GetTableByAlias(string alias);
        QTable GetTable(IDBTable table);

        DBColumn ParseColumn(string name, string prefix, out QTable qTable);
        DBTable ParseTable(string name);

        bool CheckItem(DBItem item);

        IEnumerable<T> Select<T>() where T : DBItem;

        QParam CreateParam(LogicType logic, IInvoker invoker, object value, QBuildParam buildParam = QBuildParam.None);
        QParam CreateParam(LogicType logic, IInvoker invoker, CompareType comparer, object value);

        IQuery Column(QFunctionType function, params object[] args);
        IQuery Column(IInvoker invoker);

        IQuery Where(string filter, QBuildParam buildParam = QBuildParam.AutoLike);
        IQuery Where(Type typeFilter);
        IQuery Where(QParam parameter);
        IQuery Where(Action<QParam> parameter);
        IQuery Where(IInvoker column, object value, QBuildParam buildParam = QBuildParam.None);
        IQuery Where(IInvoker column, CompareType comparer, object value);

        IQuery And(QParam parameter);
        IQuery And(Action<QParam> parameter);
        IQuery And(IInvoker column, object value, QBuildParam buildParam = QBuildParam.None);
        IQuery And(IInvoker column, CompareType comparer, object value);

        IQuery Or(QParam parameter);
        IQuery Or(Action<QParam> parameter);
        IQuery Or(IInvoker column, object value, QBuildParam buildParam = QBuildParam.None);
        IQuery Or(IInvoker column, CompareType comparer, object value);

        IQuery OrderBy(IInvoker column, ListSortDirection direction = ListSortDirection.Ascending);

        DBComparerList<T> GetComparer<T>() where T : DBItem;
        List<T> Sort<T>(List<T> ts) where T : DBItem;
    }

}
