using DataWF.Common;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace DataWF.Common
{
    [InvokerGenerator]
    public partial class LoginModel : DefaultItem
    {
        private string email;
        private string password;
        private bool? online;
        private string platform;
        private string application;
        private string version;

        [Required(ErrorMessage = "Email is Required!")]
        public string Email
        {
            get => email;
            set
            {
                if (!string.Equals(email, value, StringComparison.Ordinal))
                {
                    var oldValue = email;
                    email = value;
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        [Required(ErrorMessage = "Password Is Required!")]
        [DataType(DataType.Password)]
        public string Password
        {
            get => password;
            set
            {
                if (!string.Equals(password, value, StringComparison.Ordinal))
                {
                    var oldValue = password;
                    password = value;
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        [DefaultValue(false)]
        public bool? Online
        {
            get => online;
            set
            {
                if (online != value)
                {
                    var oldValue = online;
                    online = value;
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        public string Platform
        {
            get => platform;
            set
            {
                if (!string.Equals(platform, value, StringComparison.Ordinal))
                {
                    var oldValue = platform;
                    platform = value;
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        public string Application
        {
            get => application;
            set
            {
                if (!string.Equals(application, value, StringComparison.Ordinal))
                {
                    var oldValue = application;
                    application = value;
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        public string Version
        {
            get => version;
            set
            {
                if (string.Equals(version, value, StringComparison.Ordinal))
                {
                    var oldValue = version;
                    version = value;
                    OnPropertyChanged(oldValue, value);
                }
            }
        }
    }
}
