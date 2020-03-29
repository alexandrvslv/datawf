using System;

namespace DataWF.Common
{
    public class GroupInvoker<T> : Invoker<T, IGroup> where T : IGroup
    {
        public static readonly IInvoker<T, IGroup> Instance = new GroupInvoker<T>();

        public GroupInvoker()
        { }

        public override bool CanWrite => true;

        public override string Name { get => nameof(IGroup.Group); }

        public override IGroup GetValue(T target)
        {
            return target.Group;
        }

        public override void SetValue(T target, IGroup value)
        {
            target.Group = value;
        }
    }
}

