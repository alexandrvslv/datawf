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
using System;

namespace DataWF.Data
{
    public class DBColumnNDateTime : DBColumnNullable<DateTime>
    {
        public override void Read(DBTransaction transaction, DBItem row, int i)
        {
            if (row.Attached && row.UpdateState != DBUpdateState.Default && row.GetOld(this, out _))
            {
                return;
            }
            var isNull = transaction.Reader.IsDBNull(i);
            var value = isNull ? (DateTime?)null : transaction.Reader.GetDateTime(i);
            if (!isNull && (Keys & DBColumnKeys.UtcDate) != 0)
            {
                value = DateTime.SpecifyKind((DateTime)value, DateTimeKind.Utc);
            }
            SetValue(row, value, DBSetValueMode.Loading);
        }

        public override F ReadAndSelect<F>(DBTransaction transaction, int i)
        {
            var value = transaction.Reader.GetDateTime(i);
            if ((Keys & DBColumnKeys.UtcDate) != 0)
            {
                value = DateTime.SpecifyKind(value, DateTimeKind.Utc);
            }
            return PullIndex?.SelectOne<F>(value);
        }

        public override string FormatValue(DateTime? value)
        {
            if (value is DateTime val)
            {
                if (Format != null)
                    return val.ToString(Format);
                return val.Equals(val.Date) ? val.ToString("yyyy.MM.dd") : val.ToString();
            }
            else
                return string.Empty;
        }
    }
}