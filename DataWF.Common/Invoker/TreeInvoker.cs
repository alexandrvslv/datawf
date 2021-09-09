using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace DataWF.Common
{
    public class TreeInvoker<T> : Invoker<T, bool> where T : IGroup
    {
        public static readonly TreeInvoker<T> Instance = new TreeInvoker<T>();

        public TreeInvoker()
        { }

        public override string Name { get => nameof(IGroup.IsExpanded); }

        public override bool CanWrite => true;

        public override bool GetValue(T target)
        {
            return target.IsExpanded;
        }

        public override void SetValue(T target, bool value)
        {
            target.Expand = value;
        }
       
    }

    public class TreeInvoker : TreeInvoker<IGroup>
    {
    }
}

