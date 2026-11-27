using DataWF.Invokers;
using System;

namespace DataWF.Invokers
{
    public abstract class InvokeBinder : IDisposable
    {
        public IInvoker DataInvoker { get; set; }
        public IInvoker ViewInvoker { get; set; }

        public abstract void Bind(object data, object view);
        public abstract void Unbind();

        public abstract void Dispose();

        public abstract object GetData();
        public abstract object GetView();

    }
}

