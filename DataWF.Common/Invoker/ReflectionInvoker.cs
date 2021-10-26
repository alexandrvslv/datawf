using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DataWF.Common
{
    public class ReflectionInvoker : IInvoker
    {
        public ReflectionInvoker(Type type, string name)
        {
            Name = name;
            List = TypeHelper.GetMemberInfoList(type, name);
            Last = List.Last();
            DataType = TypeHelper.GetMemberType(Last.Info);
            TargetType = type;
        }

        public bool CanWrite => Last.Info is FieldInfo || (Last.Info is PropertyInfo && ((PropertyInfo)Last.Info).CanWrite);

        public Type DataType { get; set; }

        public Type TargetType { get; set; }

        public string Name { get; set; }

        public List<MemberParseInfo> List { get; private set; }

        public MemberParseInfo Last { get; private set; }

        public object GetValue(object target)
        {
            var temp = target;
            foreach (var info in List)
            {
                temp = GetValue(info, temp);
                if (temp == null)
                    break;
            }
            return temp;
        }

        public void SetValue(object target, object value)
        {
            object temp = null;
            foreach (var info in List)
            {
                if (info == Last)
                {
                    SetValue(info, target, value);
                    if (info.Info.DeclaringType.IsValueType && temp != null)
                    {
                        SetValue(List[List.Count - 2], temp, target);
                    }
                }
                else
                {
                    temp = target;
                    target = GetValue(info, target);
                    if (target == null)
                        break;
                }
            }
        }

        public void SetValue(MemberParseInfo info, object target, object value)
        {
            switch (info.Info)
            {
                case PropertyInfo pInfo:
                    if (info.Index != null)
                    {
                        pInfo.SetValue(target, value, new object[] { info.Index });
                    }
                    else
                    {
                        pInfo.SetValue(target, value);
                    }
                    break;
                case FieldInfo fInfo:
                    fInfo.SetValue(target, value);
                    break;
            }
        }

        public object GetValue(MemberParseInfo info, object target)
        {
            switch (info.Info)
            {
                case PropertyInfo pInfo:
                    return info.Index != null ? pInfo.GetValue(target, new object[] { info.Index }) : pInfo.GetValue(target);
                case FieldInfo fInfo:
                    return fInfo.GetValue(target);
                case MethodInfo mInfo:
                    return mInfo.Invoke(target, null);
            }
            return null;
        }

        public IListIndex CreateIndex(bool concurrent)
        {
            throw new NotImplementedException();
        }

        public IQueryParameter CreateParameter()
        {
            throw new NotImplementedException();
        }

        public InvokerComparer CreateComparer()
        {
            throw new NotImplementedException();
        }

        public bool CheckItem(object item, object typedValue, CompareType comparer, IComparer comparision)
        {
            return ListHelper.CheckItem(GetValue(item), typedValue, comparer, comparision);
        }
    }
}
