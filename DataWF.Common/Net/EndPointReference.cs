using DataWF.Common;
using System;
using System.ComponentModel;
using System.Net;

[assembly: Invoker(typeof(EndPointReference<>), nameof(EndPointReference<object>.EndPoint), typeof(EndPointReference<>.EndPointInvoker))]
[assembly: Invoker(typeof(EndPointReference<>), nameof(EndPointReference<object>.Reference), typeof(EndPointReference<>.ReferenceInvoker))]
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

        public class EndPointInvoker : Invoker<EndPointReference<T>, IPEndPoint>
        {
            public static readonly EndPointInvoker Instance = new EndPointInvoker();
            public override string Name => nameof(EndPointReference<T>.EndPoint);

            public override bool CanWrite => true;

            public override IPEndPoint GetValue(EndPointReference<T> target) => target.EndPoint;

            public override void SetValue(EndPointReference<T> target, IPEndPoint value) => target.EndPoint = value;
        }

        public class ReferenceInvoker : Invoker<EndPointReference<T>, T>
        {
            public static readonly ReferenceInvoker Instance = new ReferenceInvoker();
            public override string Name => nameof(EndPointReference<T>.Reference);

            public override bool CanWrite => true;

            public override T GetValue(EndPointReference<T> target) => target.Reference;

            public override void SetValue(EndPointReference<T> target, T value) => target.Reference = value;
        }

        public class CountInvoker : Invoker<EndPointReference<T>, int>
        {
            public static readonly CountInvoker Instance = new CountInvoker();
            public override string Name => nameof(EndPointReference<T>.Count);

            public override bool CanWrite => true;

            public override int GetValue(EndPointReference<T> target) => target.Count;

            public override void SetValue(EndPointReference<T> target, int value) => target.Count = value;
        }
    }

    public class EndPointReferenceList<T> : SelectableList<EndPointReference<T>>
    {
        public EndPointReferenceList()
        {
            Indexes.Add(EndPointReference<T>.EndPointInvoker.Instance);
        }

        public EndPointReference<T> this[IPEndPoint point]
        {
            get { return SelectOne(nameof(EndPointReference<T>.EndPoint), CompareType.Equal, point); }
        }
    }




}
