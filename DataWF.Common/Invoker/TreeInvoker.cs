using System;

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
        public override IQueryParameter CreateParameter(Type type)
        {
            var parameter = base.CreateParameter(type);
            parameter.Value = true;
            parameter.IsGlobal = true;
            parameter.FormatIgnore = true;
            return parameter;
        }

        public override QueryParameter<TT> CreateParameter<TT>()
        {
            var parameter = base.CreateParameter<TT>();
            parameter.Value = true;
            parameter.IsGlobal = true;
            parameter.FormatIgnore = true;
            return parameter;
        }
        public override InvokerComparer CreateComparer(Type type)
        {
            type = type ?? typeof(T);
            return (InvokerComparer)Activator.CreateInstance(typeof(TreeComparer<>).MakeGenericType(type));
        }
        public override InvokerComparer<TT> CreateComparer<TT>()
        {
            return (InvokerComparer<TT>)CreateComparer(typeof(TT));
        }
    }

    public class TreeInvoker : TreeInvoker<IGroup>
    {
    }
}

