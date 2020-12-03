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
using System.Runtime.Serialization;

namespace DataWF.Data
{
    public class DBColumnBinarySerializable<T> : DBColumn<T> where T : IBinarySerializable
    {
        public override void Read(DBTransaction transaction, DBItem row, int i)
        {
            if (row.Attached && row.UpdateState != DBUpdateState.Default && row.GetOld(this, out _))
            {
                return;
            }
            if (!transaction.Reader.IsDBNull(i))
            {
                var byteArray = (byte[])transaction.Reader.GetValue(i);
                var serializable = (T)FormatterServices.GetUninitializedObject(DataType);
                serializable.Deserialize(byteArray);
                SetValue(row, serializable, DBSetValueMode.Loading);
            }
            else
            {
                SetValue(row, default(T), DBSetValueMode.Loading);
            }
        }

        public override F ReadAndSelect<F>(DBTransaction transaction, int i)
        {
            var byteArray = (byte[])transaction.Reader.GetValue(i);
            var serializable = (T)FormatterServices.GetUninitializedObject(DataType);
            serializable.Deserialize(byteArray);
            return Table.GetPullIndex(this)?.SelectOne<F>(serializable);
        }

        public override object GetParameterValue(DBItem item)
        {
            var value = GetValue(item);
            if (Equal(value, default(T)))
                return DBNull.Value;
            return value.Serialize();
        }
    }
}