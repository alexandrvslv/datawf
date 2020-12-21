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

namespace DataWF.Data
{
    public class DBItemDefaultComparer<T> : IComparer<T>, IComparer where T : DBItem
    {
        public static readonly DBItemDefaultComparer<T> Instance = new DBItemDefaultComparer<T>();

        public int Compare(T x, T y)
        {
            ref readonly var xHandler = ref x.handler;
            ref readonly var yHandler = ref y.handler;
            var xValue = xHandler.Value;
            var yValue = yHandler.Value;
            return xValue == yValue ? 0 : xValue > yValue ? 1 : -1;
        }

        public int Compare(object x, object y)
        {
            return Compare((T)x, (T)y);
        }
    }

}

