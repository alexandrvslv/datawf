using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DataWF.Web.Common
{
    public class WebNotifyRegistration
    {
        public static readonly WebNotifyRegistration Default = new WebNotifyRegistration { Platform = "Default", Application = "Default", Version = "1.0.0.0" };

        public string Platform { get; set; }

        public string Application { get; set; }

        public string Version { get; set; }

    }
}