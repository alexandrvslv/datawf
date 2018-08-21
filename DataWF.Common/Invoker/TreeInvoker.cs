﻿using System;

namespace DataWF.Common
{
    public class TreeInvoker<T> : IInvoker<T, bool> where T : IGroup
    {
        public TreeInvoker()
        {
            Name = nameof(IGroup.IsExpanded);
            DataType = typeof(bool);
        }

        public bool CanWrite { get { return false; } }

        public Type DataType { get; set; }

        public Type TargetType { get { return typeof(T); } }

        public string Name { get; set; }

        public IListIndex CreateIndex()
        {
            return new ListIndex<T, bool>(this);
        }

        public bool GetValue(T target)
        {
            return target.IsExpanded;
        }

        public object GetValue(object target)
        {
            return GetValue((T)target);
        }

        public void SetValue(T target, bool value)
        {
            throw new NotSupportedException();
        }

        public void SetValue(object target, object value)
        {
            throw new NotImplementedException();
        }
    }

}

