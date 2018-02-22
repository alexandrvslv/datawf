using System;
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
            DataType = TypeHelper.GetMemberType(Last);
            TargetType = type;
        }

        public bool CanWrite { get { return Last is FieldInfo || Last is PropertyInfo && ((PropertyInfo)Last).CanWrite; } }

        public Type DataType { get; set; }

        public Type TargetType { get; set; }

        public string Name { get; set; }

        public List<MemberInfo> List { get; private set; }

        public MemberInfo Last { get; private set; }

        public object Get(object target)
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

        public void Set(object target, object value)
        {
            object temp = null;
            foreach (var info in List)
            {
                if (info == Last)
                {
                    SetValue(info, target, value);
                    if (info.DeclaringType.IsValueType && temp != null)
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

        public void SetValue(MemberInfo info, object target, object value)
        {
            if (info is PropertyInfo)
            {
                ((PropertyInfo)info).SetValue(target, value);
            }
            if (info is FieldInfo)
            {
                ((FieldInfo)info).SetValue(target, value);
            }
        }

        public object GetValue(MemberInfo info, object target)
        {
            if (info is PropertyInfo)
            {
                return ((PropertyInfo)info).GetValue(target);
            }
            if (info is FieldInfo)
            {
                return ((FieldInfo)info).GetValue(target);
            }
            if (info is MethodInfo)
            {
                return ((MethodInfo)info).Invoke(target, null);
            }
            return null;
        }
    }
}
