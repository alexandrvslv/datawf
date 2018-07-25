using System.ComponentModel.DataAnnotations;

namespace DataWF.Web.Common
{
    public class TokenModel
    {
        public string Email { get; set; }

        [DataType(DataType.Password)]
        public string Token { get; set; }
    }
}
