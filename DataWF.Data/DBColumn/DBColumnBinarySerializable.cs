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
    public class DBColumnBinarySerializable<T> : DBColumn<T> where T : IBinarySerializable, new()
    {
        public override void Read(DBTransaction transaction, DBItem row, int i)
        {
            if (!transaction.Reader.IsDBNull(i))
            {
                var byteArray = (byte[])transaction.Reader.GetValue(i);
                var serializable = new T();
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
            var serializable = new T();
            serializable.Deserialize(byteArray);
            return ((IPullOutIndex<F, T>)pullIndex).SelectOne(serializable);
        }

        public override object GetParameterValue(T value)
        {
            if (Equal(value, default(T)))
                return DBNull.Value;
            return value.Serialize();
        }

        public override object GetParameterValue(object value)
        {
            if (value is byte[] data)
                return data;
            return base.GetParameterValue(value);
        }

        public override T Parse(object value)
        {
            if (value is T typedValue)
                return typedValue;
            if (value == null || value == DBNull.Value)
                return default(T);
            if (value is byte[] byteArray)
            {
                var item = new T();
                item.Deserialize(byteArray);
                return item;
            }
            if (value is string stringValue)
            {
                var item = new T();
                //TODO string formatable
                item.Deserialize(Convert.FromBase64String(stringValue));
                return item;
            }

            throw new Exception($"Unable to parse type {typeof(T)} from {value}");
        }

        public override string FormatQuery(T value)
        {
            if (Equal(value, default(T)))
                return "null";
            var data = (byte[])GetParameterValue(value);
            return Helper.GetHexString(data);
        }

        public override string FormatDisplay(T value)
        {
            if (Equal(value, default(T)))
                return string.Empty;
            return value.ToString();
        }
    }
}