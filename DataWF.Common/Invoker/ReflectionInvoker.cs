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
            switch (info)
            {
                case PropertyInfo pInfo:
                    pInfo.SetValue(target, value);
                    break;
                case FieldInfo fInfo:
                    fInfo.SetValue(target, value);
                    break;
            }
        }

        public object GetValue(MemberInfo info, object target)
        {
            switch (info)
            {
                case PropertyInfo pInfo:
                    return pInfo.GetValue(target);
                case FieldInfo fInfo:
                    return fInfo.GetValue(target);
                case MethodInfo mInfo:
                    return mInfo.Invoke(target, null);
            }
            return null;
        }

        public IListIndex CreateIndex()
        {
            throw new NotImplementedException();
        }
    }
}
