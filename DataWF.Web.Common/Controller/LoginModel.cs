using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace DataWF.Web.Common
{
    public class LoginModel
    {
        [Required(ErrorMessage = "Email Not specified")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password Not Specified")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
