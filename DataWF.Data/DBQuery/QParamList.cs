/*
 Query.cs
 
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
