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
using Excel = DocumentFormat.OpenXml.Spreadsheet;

//using DataControl;
[assembly: Invoker(typeof(StringKey), nameof(StringKey.Key), typeof(StringKey.KeyInvoker))]
namespace DataWF.Data
{
    public class StringKey
    {
        public StringKey(Excel.SharedStringItem item)
        {
            Value = item;
            Key = item.InnerText;
        }

        public StringKey(string key)
        {
            Value = new Excel.SharedStringItem { Text = new Excel.Text(key) };
            Key = key;
        }

        public Excel.SharedStringItem Value { get; set; }

        public string Key { get; set; }

        public class KeyInvoker : Invoker<StringKey, String>
        {
            public static readonly KeyInvoker Instance = new KeyInvoker();
            public override string Name => nameof(StringKey.Key);

            public override bool CanWrite => true;

            public override string GetValue(StringKey target) => target.Key;

            public override void SetValue(StringKey target, string value) => target.Key = value;
        }
    }


}