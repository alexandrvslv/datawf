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
using System.Collections.Generic;
using System.Linq;

namespace DataWF.Data
{
    public class DBTableComparer : IComparer<DBTable>
    {
        public static readonly DBTableComparer Instance = new DBTableComparer();

        public int Compare(DBTable a, DBTable b)
        {
            return Compare(a, b, false);
        }

        public int Compare(DBTable a, DBTable b, bool referenceCheck)
        {
            var rez = 0;
            if (a is IDBLogTable)
            {
                if (!(b is IDBLogTable))
                {
                    rez = 1;
                }
            }
            else if (b is IDBLogTable)
            {
                rez = -1;
            }
            if (rez == 0)
            {
                if (a is IDBVirtualTable)
                {
                    if (!(b is IDBVirtualTable))
                    {
                        rez = 1;
                    }
                }
                else if (b is IDBVirtualTable)
                {
                    rez = -1;
                }
            }
            if (rez == 0)
            {
                if (referenceCheck)
                {
                    var referenceA = a.Columns.GetIsReference().Select(p => p.ReferenceTableName);
                    var referenceB = b.Columns.GetIsReference().Select(p => p.ReferenceTableName);

                    if (referenceA.Contains(b.FullName))
                    {
                        rez = 1;
                    }
                    else if (referenceB.Contains(a.FullName))
                    {
                        rez = -1;
                    }
                }
                if (rez == 0)
                {
                    rez = string.Compare(a.Name, b.Name, StringComparison.Ordinal);
                }
            }

            return rez;
        }
    }
}
