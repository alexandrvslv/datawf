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
using System.Collections.Generic;
using System.ComponentModel;

namespace DataWF.Data
{
    public enum DBItemMethod
    {
        Accept,
        Attach,
        Change,
        Update
    }

    public class DBItemEventArgs : CancelEventArgs
    {
        public DBItemEventArgs(DBItem item, DBColumn column = null, string property = null, object value = null)
        {
            Item = item;
            State = item.UpdateState;
            Column = column;
            Value = value;
            Property = property ?? string.Empty;
        }

        public DBItemEventArgs(DBItem item, DBTransaction transaction)
            : this(item, transaction, transaction.Caller)
        { }

        public DBItemEventArgs(DBItem item, DBTransaction transaction, IUserIdentity user)
        {
            Item = item;
            State = item.UpdateState;
            Transaction = transaction;
            User = user;
        }

        public DBUpdateState State { get; set; }

        public DBColumn Column { get; }

        public string Property { get; }

        public object Value { get; set; }

        public DBItem Item { get; }

        public DBLogItem LogItem { get; set; }

        public List<DBColumn> Columns { get; set; }

        public DBTransaction Transaction { get; }

        public IUserIdentity User { get; }

        public bool StateAdded(DBUpdateState filter)
        {
            return (State & filter) != filter && (Item.UpdateState & filter) == filter;
        }

        public bool StateRemoved(DBUpdateState filter)
        {
            return (State & filter) == filter && (Item.UpdateState & filter) != filter; ;
        }
    }
}
