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

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class TableAttribute : Attribute
    {
        public TableAttribute(string name, string groupName, string schema = null)
        {
            TableName = name;
            GroupName = groupName ?? "Default";
            Schema = schema;
        }

        public string TableName { get; set; }

        public virtual string GroupName { get; set; }

        public string Schema { get; }

        [DefaultValue(DBTableType.Table)]
        public DBTableType TableType { get; set; } = DBTableType.Table;

        public int BlockSize { get; set; } = 256;

        public DBTableKeys Keys { get; set; }

        public bool ForceLoadReference { get; set; }

        public string SequenceName { get; set; }

        public Type Type { get; set; }
    }
}
