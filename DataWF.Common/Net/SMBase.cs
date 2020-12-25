using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Xml.Serialization;

namespace DataWF.Common
{
    public enum SMType : byte
    {
        Request,
        Response,
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

    [InvokerGenerator]
    public abstract partial class SMBase
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
    }

    public class SMNotify : SMBase
    {
        public SMNotify()
        {
            Type = SMType.Notify;
        }
    }

    [InvokerGenerator]
    public partial class SMRequest : SMBase
    {
        public SMRequest()
        {
            Type = SMType.Request;
        }

        [XmlIgnore, JsonIgnore]
        public SMResponce Responce { get; set; }

        [Display(Order = 1)]
        public SMRequestType RequestType { get; set; }
        
    }

    [InvokerGenerator]
    public partial class SMResponce : SMBase
    {
        public SMResponce()
        {
            Type = SMType.Response;
        }

        [Display(Order = 1)]
        public SMResponceType ResponceType { get; set; }

        [Display(Order = 2)]
        public long? RequestId { get; set; }
        
    }
}
