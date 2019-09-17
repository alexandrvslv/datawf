using System;

namespace DataWF.Common
{
    public class TreeInvoker : IInvoker
    {
        public Type DataType => typeof(bool);

        public virtual Type TargetType => typeof(IGroup);

        public bool CanWrite => true;

        public string Name { get => nameof(IGroup.IsExpanded); set { } }

        public virtual IListIndex CreateIndex(bool concurrent)
        {
            throw new NotImplementedException();
        }

        public virtual object GetValue(object target)
        {
            return ((IGroup)target).IsExpanded;
        }

        public virtual void SetValue(object target, object value)
        {
            ((IGroup)target).Expand = (bool)value;
        }
    }

    public class TreeInvoker<T> : TreeInvoker, IInvoker<T, bool> where T : IGroup
    {
        public static readonly IInvoker<T, bool> Instance = new TreeInvoker<T>();

        public TreeInvoker()
        { }

        public override Type TargetType { get { return typeof(T); } }

        public override IListIndex CreateIndex(bool concurrent)
        {
            return ListIndexFabric.Create<T, bool>(this, concurrent);
        }

        public bool GetValue(T target)
        {
            return target.IsExpanded;
        }

        public void SetValue(T target, bool value)
        {
            target.Expand = value;
        }
    }

}

