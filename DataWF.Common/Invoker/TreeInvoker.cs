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

        public IListIndex CreateIndex()
        {
            return new ListIndex<IGroup, bool>(this);
        }

        public bool GetValue(IGroup target)
        {
            return target.IsExpanded;
        }

        public object GetValue(object target)
        {
            return GetValue((IGroup)target);
        }

        public void SetValue(IGroup target, bool value)
        {
            throw new NotSupportedException();
        }

        public void SetValue(object target, object value)
        {
            throw new NotImplementedException();
        }
    }

}

