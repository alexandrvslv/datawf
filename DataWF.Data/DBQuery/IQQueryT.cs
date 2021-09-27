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
using System.ComponentModel;
using System.Linq.Expressions;

namespace DataWF.Data
{
    public interface IQQuery<T> : IQQuery, IEnumerable<T> where T : DBItem
    {
        new IQQuery<T> Column(QFunctionType function, params object[] args);
        new IQQuery<T> Column(IInvoker invoker);
        IQQuery<T> Column<K>(Expression<Func<T, K>> keySelector);

        new IQQuery<T> WhereViewColumns(string filter, QBuildParam buildParam = QBuildParam.AutoLike);
        new IQQuery<T> Where(string filter);

        IQQuery<T> Where(Expression<Func<T, bool>> expression);
        new IQQuery<T> Where(Type typeFilter);
        new IQQuery<T> Where(QParam parameter);
        new IQQuery<T> Where(Action<QParam> parameter);
        new IQQuery<T> Where(IInvoker column, object value, QBuildParam buildParam = QBuildParam.None);
        new IQQuery<T> Where(IInvoker column, CompareType comparer, object value);
        new IQQuery<T> Where(string column, CompareType comparer, object value);

        new IQQuery<T> And(QParam parameter);
        new IQQuery<T> And(Action<QParam> parameter);
        new IQQuery<T> And(IInvoker column, object value, QBuildParam buildParam = QBuildParam.None);
        new IQQuery<T> And(IInvoker column, CompareType comparer, object value);
        new IQQuery<T> And(string column, CompareType comparer, object value);

        new IQQuery<T> Or(QParam parameter);
        new IQQuery<T> Or(Action<QParam> parameter);
        new IQQuery<T> Or(IInvoker column, object value, QBuildParam buildParam = QBuildParam.None);
        new IQQuery<T> Or(IInvoker column, CompareType comparer, object value);
        new IQQuery<T> Or(string column, CompareType comparer, object value);

        new IQQuery<T> OrderBy(IInvoker column, ListSortDirection direction = ListSortDirection.Ascending);
        IQQuery<T> OrderBy<K>(Expression<Func<T, K>> keySelector);
        IQQuery<T> OrderByDescending<K>(Expression<Func<T, K>> keySelector);

        IEnumerable<T> Select();
        IEnumerable<T> Load(DBTransaction transaction = null);


    }

}
