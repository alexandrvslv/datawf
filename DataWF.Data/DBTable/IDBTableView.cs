﻿//  The MIT License (MIT)
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
using System.Data;
using System.Threading.Tasks;

namespace DataWF.Data
{
    public interface IDBTableView : IDBTableContent, ISortable, INotifyListPropertyChanged, IDisposable, IFilterable
    {
        IDbCommand Command { get; set; }

        QParam DefaultParam { get; set; }

        QQuery Query { get; set; }

        DBStatus StatusFilter { get; set; }

        Type TypeFilter { get; set; }

        bool ClearFilter();

        void ResetFilter();

        bool IsEdited { get; }

        bool IsStatic { get; set; }

        new bool IsSynchronized { get; set; }

        void OnSourceCollectioChanged(DBItem item, NotifyCollectionChangedAction type);

        void OnSourceItemChanged(DBItem item, string property, DBColumn column);

        IEnumerable<DBItem> Load(DBLoadParam param = DBLoadParam.None);

        void LoadAsynch(DBLoadParam param = DBLoadParam.None);

        Task Save();

        IList ToList();
    }
}
