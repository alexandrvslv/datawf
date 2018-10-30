using DataWF.Module.Common;
using Newtonsoft.Json;
using System.IO;
using System.Security.Claims;

namespace DataWF.Web.Common
{
    public class ClaimsJsonTextWriter : JsonTextWriter
    {
        private User user;
        public ClaimsJsonTextWriter(TextWriter textWriter) : base(textWriter)
        { }

        public ClaimsPrincipal UserPrincipal { get; set; }

        public User User => user ?? (user = UserPrincipal?.GetCurrentUser());
    }

}
