using System;

namespace DataWF.Common
{
    public abstract class InvokeBinder : IDisposable
    {
        public IInvoker DataInvoker { get; set; }
        public IInvoker ViewInvoker { get; set; }

        public abstract void Bind(object data, object view);
        public abstract void Dispose();

        public abstract object GetData();
        public abstract object GetView();
    }
}

