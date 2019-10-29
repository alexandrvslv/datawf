using DataWF.Common;
using Newtonsoft.Json;
using System.IO;
using System.Security.Claims;

namespace DataWF.WebService.Common
{
    public class ClaimsJsonTextWriter : JsonTextWriter
    {
        private IUserIdentity user;
        public ClaimsJsonTextWriter(TextWriter textWriter) : base(textWriter)
        { }

        public ClaimsPrincipal UserPrincipal { get; set; }

        public IUserIdentity User
        {
            get => user ?? (user = UserPrincipal?.GetCommonUser());
            set => user = value;
        }

        public bool IncludeReferencing { get; set; } = true;

        public bool IncludeReferences { get; set; } = false;
    }

}
