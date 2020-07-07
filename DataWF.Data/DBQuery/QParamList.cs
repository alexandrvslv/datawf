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

namespace DataWF.Data
{
    public class QParamList : QItemList<QParam>
    {
        public QParamList()
        {
        }

        public QParamList(IQItemList owner) : base(owner)
        {
            //Indexes.Add(groupInvoker);
        }
    }

    [Invoker(typeof(QParam), nameof(QParam.Group))]
    public class QParamGroupInvoker : Invoker<QParam, QParam>
    {
        public static readonly QParamGroupInvoker Instance = new QParamGroupInvoker();
        public override string Name => nameof(QParam.Group);

        public override bool CanWrite => true;

        public override QParam GetValue(QParam target) => target.Group;

        public override void SetValue(QParam target, QParam value) => target.Group = value;
    }

    [Invoker(typeof(QParam), "Column.Name")]
    public class QParamColumnNameInvoker : Invoker<QParam, string>
    {
        public static readonly QParamColumnNameInvoker Instance = new QParamColumnNameInvoker();
        public override string Name => "Column.Name";

        public override bool CanWrite => false;

        public override string GetValue(QParam target) => target.Column?.Name;

        public override void SetValue(QParam target, string value) { }
    }
}
