using System;
using System.ComponentModel;
using System.Net;

namespace DataWF.Common
{
    public class EndPointReference<T>
    {
        public IPEndPoint EndPoint { get; set; }

        public T Reference { get; set; }

        public int Count { get; set; }
        [DefaultFormat("size")]
        public uint Length { get; set; }

        public override string ToString()
        {
            return string.Format("{0}-{1}", Reference, EndPoint);
        }
    }

    public class EndPointReferenceList<T> : SelectableList<EndPointReference<T>>
    {
        private static ActionInvoker<EndPointReference<T>, IPEndPoint> invoker = new ActionInvoker<EndPointReference<T>, IPEndPoint>(nameof(EndPointReference<T>.EndPoint), item => item.EndPoint);

        public EndPointReferenceList()
        {
            Indexes.Add(invoker);
        }

        public EndPointReference<T> this[IPEndPoint point]
        {
            get { return SelectOne(nameof(EndPointReference<T>.EndPoint), CompareType.Equal, point); }
        }
    }

}
