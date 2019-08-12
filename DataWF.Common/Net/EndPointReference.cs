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
        public EndPointReferenceList()
        {
            Indexes.Add(EndPointReferenceEndPoint<T>.Instance);
        }

        public EndPointReference<T> this[IPEndPoint point]
        {
            get { return SelectOne(nameof(EndPointReference<T>.EndPoint), CompareType.Equal, point); }
        }
    }

    [Invoker(typeof(EndPointReferenceEndPoint<>), nameof(EndPoint))]
    public class EndPointReferenceEndPoint<T> : Invoker<EndPointReference<T>, IPEndPoint>
    {
        public static readonly EndPointReferenceEndPoint<T> Instance = new EndPointReferenceEndPoint<T>();
        public EndPointReferenceEndPoint()
        {
            Name = nameof(EndPointReference<T>.EndPoint);
        }

        public override bool CanWrite => true;

        public override IPEndPoint GetValue(EndPointReference<T> target) => target.EndPoint;

        public override void SetValue(EndPointReference<T> target, IPEndPoint value) => target.EndPoint = value;
    }

}
