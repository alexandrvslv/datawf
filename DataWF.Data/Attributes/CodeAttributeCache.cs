/*
 ColumnConfig.cs
 
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
using System.Reflection;

namespace DataWF.Data
{
    public class CodeAttributeCache
    {
        public CodeAttributeCache(CodeAttribute attribute, MemberInfo member)
        {
            Attribute = attribute;
            Member = member;
            MemberInvoker = EmitInvoker.Initialize(member, true);
        }

        public CodeAttribute Attribute { get; }

        public MemberInfo Member { get; }

        public IInvoker MemberInvoker { get; }

        public object GetValue(object targe, DBTransaction transaction)
        {
            if (Member is PropertyInfo)
            {
                return MemberInvoker.GetValue(targe);
            }
            else if (Member is MethodInfo)
            {
                return ((IIndexInvoker)MemberInvoker).GetValue(targe, new object[] { transaction });
            }
            return null;
        }
    }
}
