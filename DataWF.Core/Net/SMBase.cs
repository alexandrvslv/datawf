using DataWF.Invokers;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using System.Threading;
using System.Xml.Serialization;

namespace DataWF
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

    [Invoker]
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
        //[ElementSerializer(typeof(UriSerializer))]
        public string Url { get; set; }

        [Display(Order = 100)]
        public object Data { get; set; }

        public override string ToString()
        {
            return string.Format("Type:{0} Id:{1} Data:{2}", Type, Url, Data);
        }
    }

    public class SMNotify : SMBase
    {
        public SMNotify()
        {
            Type = SMType.Notify;
        }
    }

    [Invoker]
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

    [Invoker]
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
