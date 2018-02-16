using System;

namespace DataWF.Common
{
    public class GroupIsExpandedInvoker : IInvoker<IGroup, bool>
    {
        public GroupIsExpandedInvoker()
        {
            Name = nameof(IGroup.IsExpanded);
            DataType = typeof(bool);
        }

        public bool CanWrite { get { return false; } }

        public Type DataType { get; set; }

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

