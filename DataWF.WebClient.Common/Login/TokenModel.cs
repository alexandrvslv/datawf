using System.ComponentModel.DataAnnotations;

namespace DataWF.Common
{
    public class TokenModel
    {
        public string Email { get; set; }

        [DataType(DataType.Password)]
        public string AccessToken { get; set; }

        public string RefreshToken { get; set; }

    }
}
