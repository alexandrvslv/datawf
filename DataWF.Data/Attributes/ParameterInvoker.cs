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
using System.Reflection;

namespace DataWF.Data
{
    public class ParameterInvoker
    {
        public ParameterInvoker(ParameterAttribute parameter, MemberInfo member)
        {
            Parameter = parameter;
            Member = member;
            MemberInvoker = EmitInvoker.Initialize(member, true);
        }

        public ParameterAttribute Parameter { get; }

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
