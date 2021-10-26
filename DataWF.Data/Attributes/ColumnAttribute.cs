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
using System.ComponentModel;

namespace DataWF.Data
{

    [AttributeUsage(AttributeTargets.Property)]
    public class ColumnAttribute : Attribute
    {
        public ColumnAttribute()
        { }

        public ColumnAttribute(string name, int size = 0, short scale = 0)
        {
            ColumnName = name;
            Size = size;
            Scale = scale;
        }

        public int Order { get; set; } = -1;

        public string ColumnName { get; set; }

        public string GroupName { get; set; }

        [DefaultValue(DBColumnTypes.Default)]
        public DBColumnTypes ColumnType { get; set; }

        [DefaultValue((int)0)]
        public int Size { get; set; }

        [DefaultValue((short)0)]
        public short Scale { get; set; }

        public DBColumnKeys Keys { get; set; }

        public DBDataType DBDataType { get; set; }

        public Type DataType { get; set; }

        public ColumnAttribute Clone()
        {
            return new ColumnAttribute(ColumnName, Size, Scale)
            {
                GroupName = GroupName,
                ColumnType = ColumnType,
                DBDataType = DBDataType,
                DataType = DataType,
                Keys = Keys
            };
        }
    }
}
