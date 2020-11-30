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
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

[assembly: Invoker(typeof(SRItem), nameof(SRItem.Command), typeof(SRItem.CommandInvoker))]
[assembly: Invoker(typeof(SRItem), nameof(SRItem.UserId), typeof(SRItem.UserIdInvoker))]
[assembly: Invoker(typeof(SRItem), nameof(SRItem.Value), typeof(SRItem.ValueInvoker))]
namespace DataWF.Data
{
    public class SRItem
    {
        public DBLogType Command { get; set; }

        public int UserId { get; set; }
        
        [ElementSerializer(typeof(DBItemSRSerializer))]
        public DBItem Value { get; set; }

        [XmlIgnore, JsonIgnore]
        public List<DBColumn> Columns { get; set; }

        public class CommandInvoker : Invoker<SRItem, DBLogType>
        {
            public override string Name => nameof(Command);

            public override bool CanWrite => true;

            public override DBLogType GetValue(SRItem target) => target.Command;

            public override void SetValue(SRItem target, DBLogType value) => target.Command = value;
        }

        public class UserIdInvoker : Invoker<SRItem, int>
        {
            public override string Name => nameof(UserId);

            public override bool CanWrite => true;

            public override int GetValue(SRItem target) => target.UserId;

            public override void SetValue(SRItem target, int value) => target.UserId = value;
        }

        public class ValueInvoker : Invoker<SRItem, DBItem>
        {
            public override string Name => nameof(Value);

            public override bool CanWrite => true;

            public override DBItem GetValue(SRItem target) => target.Value;

            public override void SetValue(SRItem target, DBItem value) => target.Value = value;
        }
    }
}
