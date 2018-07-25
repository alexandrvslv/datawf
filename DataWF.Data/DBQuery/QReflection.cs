using DataWF.Common;
/*
 QColumn.cs
 
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
using System;
using System.Data;

namespace DataWF.Data
{
    public class QReflection : QItem
    {
        [NonSerialized()]
        IInvoker invoker;

        public QReflection()
        { }

        public QReflection(string name)
            : base(name)
        { }

        public QReflection(IInvoker invoker)
            : this()
        {
            Invoker = invoker;
        }

        public IInvoker Invoker
        {
            get
            {
                if (invoker == null)
                    invoker = EmitInvoker.Initialize(typeof(DBItem), this.text);
                return invoker;
            }
            set
            {
                if (invoker != value)
                {
                    invoker = value;
                    Text = value == null ? null : value.Name;
                    OnPropertyChanged(nameof(Invoker));
                }
            }
        }

        public override object GetValue(DBItem row = null)
        {
            return Invoker == null ? null : Invoker.GetValue(row);
        }
        public override string Format(IDbCommand command = null)
        {
            return command != null ? string.Empty : base.Format(command);
        }
    }
}