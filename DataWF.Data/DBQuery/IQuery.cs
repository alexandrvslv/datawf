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
using System.Linq.Expressions;

namespace DataWF.Data
{

    public interface IQQuery : IQItem, IQItemList, IEnumerable
    {
        QParamList Parameters { get; }
        QItemList<QItem> Columns { get; }
        QItemList<QOrder> Orders { get; }
        QTableList Tables { get; }
        DBLoadParam LoadParam { get; set; }
        DBCacheState CacheState { get; set; }
        DBStatus StatusFilter { get; set; }
        IQQuery BaseQuery { get; set; }
        Type TypeFilter { get; set; }
        string WhereText { get; }
        string QueryText { get; }
        ITreeComparer TreeComparer { get; set; }
        bool IsJoinAffectResult { get; }
        bool IsNoParameters();

        string Format(IDbCommand command = null);
        IDbCommand ToCommand(bool defcolumns = false);

        IEnumerable<QParam> GetAllParameters(Func<QParam, bool> predicate = null);
        bool Contains(string columnName);
        bool Contains(DBColumn column);

        string GenerateTableAlias(IDBTable Table);
        QTable GetTableByAlias(string alias);
        QTable GetTable(IDBTable table);

        DBColumn ParseColumn(string name, string prefix, out QTable qTable);
        DBTable ParseTable(string name);

        bool CheckItem(DBItem item);
        QParam CreateParam(LogicType logic, IInvoker invoker, object value, QBuildParam buildParam = QBuildParam.None);
        QParam CreateParam(LogicType logic, IInvoker invoker, CompareType comparer, object value);

        IQQuery Column(QFunctionType function, params object[] args);
        IQQuery Column(IInvoker invoker);

        IQQuery Join(DBColumn column);
        IQQuery Join(DBReferencing referencing);
        IQQuery JoinAllReferencing();
        IQQuery Join(DBColumn column, DBColumn refColumn);

        IQQuery WhereViewColumns(string filter, QBuildParam buildParam = QBuildParam.AutoLike);
        IQQuery Where(string filter);
        IQQuery Where(Type typeFilter);
        IQQuery Where(QParam parameter);
        IQQuery Where(Action<QParam> parameter);
        IQQuery Where(IInvoker column, object value, QBuildParam buildParam = QBuildParam.None);
        IQQuery Where(IInvoker column, CompareType comparer, object value);
        IQQuery Where(string column, CompareType comparer, object value);

        IQQuery And(QParam parameter);
        IQQuery And(Action<QParam> parameter);
        IQQuery And(IInvoker column, object value, QBuildParam buildParam = QBuildParam.None);
        IQQuery And(IInvoker column, CompareType comparer, object value);
        IQQuery And(string column, CompareType comparer, object value);

        IQQuery Or(QParam parameter);
        IQQuery Or(Action<QParam> parameter);
        IQQuery Or(IInvoker column, object value, QBuildParam buildParam = QBuildParam.None);
        IQQuery Or(IInvoker column, CompareType comparer, object value);
        IQQuery Or(string column, CompareType comparer, object value);

        IQQuery OrderBy(IInvoker column, ListSortDirection direction = ListSortDirection.Ascending);

        DBComparerList<T> GetComparer<T>() where T : DBItem;
        List<T> Sort<T>(List<T> ts) where T : DBItem;

        IEnumerable<T> Select<T>() where T : DBItem;

        IEnumerable<T> Load<T>(DBTransaction transaction = null) where T : DBItem;

        T FirstOrDefault<T>(DBTransaction transaction = null) where T : DBItem;

        T FirstOrDefault<T>(Func<T, bool> predicate, DBTransaction transaction = null) where T : DBItem;
        
        void Parse(string test);
    }

}
