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
using DataWF.Data;

namespace DataWF.Data
{
    public class DBColumnInt32 : DBColumn<int>
    {
        public override void LoadFromReader(DBTransaction transaction, DBItem row, int i)
        {
            if (row.Attached && row.UpdateState != DBUpdateState.Default && row.GetOld(this, out _))
            {
                return;
            }
            var value = transaction.Reader.IsDBNull(i) ? 0 : transaction.Reader.GetInt32(i);
            row.SetValue(value, this, DBSetValueMode.Loading);
        }

        public override F SelectOneFromReader<F>(DBTransaction transaction, int i)
        {
            var value = transaction.Reader.GetInt32(i);
            return Table.GetPullIndex(this)?.SelectOne<F>(value);
        }
    }
}