using DataWF.Collections;
using DataWF.Collections.Query;
using DataWF.Invokers;
using System.Net;

namespace DataWF
{
    [Invoker(Instance = true)]
    public partial class EndPointReference<T>
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
        public EndPointReferenceList()
        {
            Indexes.Add(EndPointReference<T>.EndPointInvokerInstance);
        }

        public EndPointReference<T> this[IPEndPoint point]
        {
            get { return SelectOne(nameof(EndPointReference<T>.EndPoint), CompareOperator.Equal, point); }
        }
    }




}
