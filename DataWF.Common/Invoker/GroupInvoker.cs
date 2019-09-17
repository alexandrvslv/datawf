using System;

namespace DataWF.Common
{
    public class GroupInvoker : IInvoker
    {
        public GroupInvoker()
        { }

        public Type DataType => typeof(IGroup);

        public virtual Type TargetType => typeof(IGroup);

        public bool CanWrite => true;

        public string Name { get => nameof(IGroup.Group); set { } }

        public virtual IListIndex CreateIndex(bool concurrent)
        {
            throw new NotImplementedException();
        }

        public object GetValue(object target)
        {
            return ((IGroup)target).Group;
        }

        public void SetValue(object target, object value)
        {
            ((IGroup)target).Group = (IGroup)value;
        }
    }

    public class GroupInvoker<T> : GroupInvoker, IInvoker<T, IGroup> where T : IGroup
    {
        public static readonly IInvoker<T, IGroup> Instance = new GroupInvoker<T>();

        public GroupInvoker()
        { }

        public override Type TargetType { get { return typeof(T); } }

        public override IListIndex CreateIndex(bool concurrent)
        {
            return ListIndexFabric.Create<T, IGroup>(this, concurrent);
        }

        public IGroup GetValue(T target)
        {
            return target.Group;
        }

        public void SetValue(T target, IGroup value)
        {
            target.Group = value;
        }
    }



}

