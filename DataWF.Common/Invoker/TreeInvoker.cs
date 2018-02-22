using System;

namespace DataWF.Common
{
    public class TreeInvoker : IInvoker<IGroup, bool>
    {
        public TreeInvoker()
        {
            Name = nameof(IGroup.IsExpanded);
            DataType = typeof(bool);
        }

        public bool CanWrite { get { return false; } }

        public Type DataType { get; set; }

        public Type TargetType { get { return typeof(IGroup); } }

        public string Name { get; set; }

        public bool Get(IGroup target)
        {
            return target.IsExpanded;
        }

        public object Get(object target)
        {
            return Get((IGroup)target);
        }

        public void Set(IGroup target, bool value)
        {
            throw new NotSupportedException();
        }

        public void Set(object target, object value)
        {
            throw new NotImplementedException();
        }
    }

}

