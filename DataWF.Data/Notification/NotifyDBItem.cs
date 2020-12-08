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
using DataWF.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

[assembly: Invoker(typeof(NotifyDBItem), nameof(NotifyDBItem.Command), typeof(NotifyDBItem.CommandInvoker))]
[assembly: Invoker(typeof(NotifyDBItem), nameof(NotifyDBItem.UserId), typeof(NotifyDBItem.UserIdInvoker))]
[assembly: Invoker(typeof(NotifyDBItem), nameof(NotifyDBItem.Id), typeof(NotifyDBItem.IdInvoker))]
[assembly: Invoker(typeof(NotifyDBItem), nameof(NotifyDBItem.Value), typeof(NotifyDBItem.ValueInvoker))]
namespace DataWF.Data
{
    public class NotifyDBItem : IComparable<NotifyDBItem>
    {
        public DBLogType Command { get; set; }
        public int UserId { get; set; }
        public object Id { get; set; }

        [XmlIgnore, JsonIgnore]
        public DBItem Value { get; set; }

        public int CompareTo(NotifyDBItem other)
        {
            var res = ListHelper.Compare(Id, other.Id, (IComparer)null);
            return res != 0 ? res : Command.CompareTo(Command);
        }

        public class CommandInvoker : Invoker<NotifyDBItem, DBLogType>
        {
            public override string Name => nameof(Command);

            public override bool CanWrite => true;

            public override DBLogType GetValue(NotifyDBItem target) => target.Command;

            public override void SetValue(NotifyDBItem target, DBLogType value) => target.Command = value;
        }

        public class UserIdInvoker : Invoker<NotifyDBItem, int>
        {
            public override string Name => nameof(UserId);

            public override bool CanWrite => true;

            public override int GetValue(NotifyDBItem target) => target.UserId;

            public override void SetValue(NotifyDBItem target, int value) => target.UserId = value;
        }

        public class IdInvoker : Invoker<NotifyDBItem, object>
        {
            public override string Name => nameof(Id);

            public override bool CanWrite => true;

            public override object GetValue(NotifyDBItem target) => target.Id;

            public override void SetValue(NotifyDBItem target, object value) => target.Id = value;
        }

        public class ValueInvoker : Invoker<NotifyDBItem, DBItem>
        {
            public override string Name => nameof(Value);

            public override bool CanWrite => true;

            public override DBItem GetValue(NotifyDBItem target) => target.Value;

            public override void SetValue(NotifyDBItem target, DBItem value) => target.Value = value;
        }
    }
}
