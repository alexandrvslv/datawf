using DataWF.Common;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Xml.Serialization;

[assembly: Invoker(typeof(SMBase), nameof(SMBase.Type), typeof(SMBase.TypeInvoker<>))]
[assembly: Invoker(typeof(SMBase), nameof(SMBase.Id), typeof(SMBase.IdInvoker<>))]
[assembly: Invoker(typeof(SMBase), nameof(SMBase.EndPoint), typeof(SMBase.EndPointInvoker<>))]
[assembly: Invoker(typeof(SMBase), nameof(SMBase.Data), typeof(SMBase.DataInvoker<>))]
[assembly: Invoker(typeof(SMRequest), nameof(SMRequest.RequestType), typeof(SMRequest.RequestTypeInvoker<>))]
[assembly: Invoker(typeof(SMResponce), nameof(SMResponce.ResponceType), typeof(SMResponce.ResponceTypeInvoker<>))]
[assembly: Invoker(typeof(SMResponce), nameof(SMResponce.RequestId), typeof(SMResponce.RequestIdInvoker<>))]
namespace DataWF.Common
{
    public enum SMType : byte
    {
        Request,
        Responce,
        Notify
    }

    public enum SMRequestType : byte
    {
        Login,
        Logout,
        Data
    }

    public enum SMResponceType : byte
    {
        Confirm,
        Decline,
        Data
    }

    public abstract class SMBase
    {
        private static long idSequence;

        public static long NewId()
        {
            return Interlocked.Increment(ref idSequence);
        }

        [Display(Order = -3)]
        public SMType Type { get; set; }

        [Display(Order = -2)]
        public long Id { get; set; }

        [Display(Order = -1)]
        [ElementSerializer(typeof(IPEndPointSerializer))]
        public IPEndPoint EndPoint { get; set; }

        [Display(Order = 100)]
        public object Data { get; set; }

        public override string ToString()
        {
            return string.Format("Type:{0} Id:{1} Data:{2}", Type, EndPoint, Data);
        }

        public class TypeInvoker<T> : Invoker<T, SMType> where T : SMBase
        {
            public override string Name => nameof(Type);

            public override bool CanWrite => true;

            public override SMType GetValue(T target) => target.Type;

            public override void SetValue(T target, SMType value) => target.Type = value;
        }

        public class IdInvoker<T> : Invoker<T, long> where T : SMBase
        {
            public override string Name => nameof(Id);

            public override bool CanWrite => true;

            public override long GetValue(T target) => target.Id;

            public override void SetValue(T target, long value) => target.Id = value;
        }

        public class EndPointInvoker<T> : Invoker<T, IPEndPoint> where T : SMBase
        {
            public override string Name => nameof(EndPoint);

            public override bool CanWrite => true;

            public override IPEndPoint GetValue(T target) => target.EndPoint;

            public override void SetValue(T target, IPEndPoint value) => target.EndPoint = value;
        }

        public class DataInvoker<T> : Invoker<T, object> where T : SMBase
        {
            public override string Name => nameof(Data);

            public override bool CanWrite => true;

            public override object GetValue(T target) => target.Data;

            public override void SetValue(T target, object value) => target.Data = value;
        }
    }

    public class SMNotify : SMBase
    {
        public SMNotify()
        {
            Type = SMType.Notify;
        }
    }

    public class SMRequest : SMBase
    {
        public SMRequest()
        {
            Type = SMType.Request;
        }

        [XmlIgnore, JsonIgnore]
        public SMResponce Responce { get; set; }

        [Display(Order = 1)]
        public SMRequestType RequestType { get; set; }

        public class RequestTypeInvoker<T> : Invoker<T, SMRequestType> where T : SMRequest
        {
            public override string Name => nameof(RequestType);

            public override bool CanWrite => true;

            public override SMRequestType GetValue(T target) => target.RequestType;

            public override void SetValue(T target, SMRequestType value) => target.RequestType = value;
        }
    }

    public class SMResponce : SMBase
    {
        public SMResponce()
        {
            Type = SMType.Responce;
        }

        [Display(Order = 1)]
        public SMResponceType ResponceType { get; set; }

        [Display(Order = 2)]
        public long? RequestId { get; set; }

        public class ResponceTypeInvoker<T> : Invoker<T, SMResponceType> where T : SMResponce
        {
            public override string Name => nameof(ResponceType);

            public override bool CanWrite => true;

            public override SMResponceType GetValue(T target) => target.ResponceType;

            public override void SetValue(T target, SMResponceType value) => target.ResponceType = value;
        }

        public class RequestIdInvoker<T> : Invoker<T, long?> where T : SMResponce
        {
            public override string Name => nameof(RequestId);

            public override bool CanWrite => true;

            public override long? GetValue(T target) => target.RequestId;

            public override void SetValue(T target, long? value) => target.RequestId = value;
        }
    }
}
