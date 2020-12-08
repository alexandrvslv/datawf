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
    public class DBColumnByteArray : DBColumnReferenceType<byte[]>
    {
        public override bool Equal(byte[] oldValue, byte[] newValue)
        {
            return ByteArrayComparer.Default.Equals(oldValue, newValue);
        }

        public override void Read(DBTransaction transaction, DBItem row, int i)
        {
            if (row.Attached && row.UpdateState != DBUpdateState.Default && row.GetOld(this, out _))
            {
                return;
            }
            var value = transaction.Reader.IsDBNull(i) ? null : (byte[])transaction.Reader.GetValue(i);
            SetValue(row, value, DBSetValueMode.Loading);
        }

        public override string FormatValue(byte[] val)
        {
            if (val == null)
                return string.Empty;

            if ((Keys & DBColumnKeys.Access) == DBColumnKeys.Access)
            {
                var cash = new AccessValue(val);
                string rez = string.Empty;
                foreach (var item in cash)
                {
                    rez += string.Format("{0}{1}", rez.Length > 0 ? "; " : string.Empty, item);
                }
                return rez;
            }
            else
            {
                return Helper.LenghtFormat(val.Length);
            }
        }

        public override byte[] Parse(object value)
        {
            if (value is byte[] typedValue)
                return typedValue;
            if (value == null || value == DBNull.Value)
                return null;
            if (value is string stringValue)
            {
                try
                {
                    return Convert.FromBase64String(stringValue);
                }
                catch (Exception ex)
                {
                    Helper.OnException(ex);
                    throw;
                }
            }
            return base.Parse(value);
        }
    }
}