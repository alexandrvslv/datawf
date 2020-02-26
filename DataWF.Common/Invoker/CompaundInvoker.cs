using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DataWF.Common
{
    public class CompaundInvoker<T, V> : Invoker<T, V>
    {
        private string property;
        private List<IInvoker> invokers = new List<IInvoker>();
        private IInvoker lastInvoker;
        public CompaundInvoker(string property)
           : this(property, TypeHelper.GetMemberInfoList(typeof(T), property))
        {
        }

        public CompaundInvoker(string property, List<MemberInfo> list)
        {
            this.property = property;
            foreach (var info in list)
            {
                invokers.Add(EmitInvoker.Initialize(info, true));
            }
            lastInvoker = invokers.LastOrDefault();
        }

        public override string Name => property;

        public override bool CanWrite => lastInvoker.CanWrite;

        public override V GetValue(T target)
        {
            var temp = (object)null;
            for (var i = 0; i < invokers.Count; i++)
            {
                var invoker = invokers[i];
                temp = invoker.GetValue(temp ?? target);
                if (temp == null)
                    return default(V);
            }
            return (V)temp;
        }

        public override void SetValue(T target, V value)
        {
            var temp = (object)null;
            for (var i = 0; i < invokers.Count - 1; i++)
            {
                var invoker = invokers[i];
                temp = invoker.GetValue(temp ?? target);
                if (temp == null)
                    return;
            }
            lastInvoker.SetValue(temp, value);
        }
    }

}
