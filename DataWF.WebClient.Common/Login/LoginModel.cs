using System.ComponentModel.DataAnnotations;

namespace DataWF.Common
{
    public class LoginModel
    {
        [Required(ErrorMessage = "Email Not specified")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password Not Specified")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public bool Online { get; set; }
        
        public string Platform { get; set; }
        
        public string Application { get; set; }
        
        public string Version { get; set; }
    }
}
