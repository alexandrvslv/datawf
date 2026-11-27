//  The MIT License (MIT)
//
// Copyright © 2023 Vassilyev Alexandr
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
using System.Linq;
using System.Linq.Expressions;

namespace DataWF.Collections.Query
{
    public class QQueryKeySelector<T, BT> : IQueryProvider<T>
    {
        private readonly Func<BT, T> keyFunction;

        public QQueryKeySelector(IQQuery<BT> holdingQuery, Expression<Func<BT, T>> keySelector)
        {
            HoldingQuery = holdingQuery;
            this.keyFunction = keySelector.Compile();
            IQueryable<BT> query;
        }

        public IQQuery<BT> HoldingQuery { get; }

        public IEnumerator<T> GetEnumerator()
        {
            foreach (var entry in HoldingQuery)
                yield return keyFunction(entry);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

