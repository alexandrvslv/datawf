/*
 TemplateParcer.cs
 
 Author:
      Alexandr <alexandr_vslv@mail.ru>

 This program is free software: you can redistribute it and/or modify
 it under the terms of the GNU Lesser General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 This program is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 GNU Lesser General Public License for more details.

 You should have received a copy of the GNU Lesser General Public License
 along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using DataWF.Common;
using System;
using Excel = DocumentFormat.OpenXml.Spreadsheet;

//using DataControl;

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

        [Invoker(typeof(StringKey), nameof(StringKey.Key))]
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