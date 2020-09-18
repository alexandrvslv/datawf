using DataWF.Common;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

[assembly: Invoker(typeof(LoginModel), nameof(LoginModel.Email), typeof(LoginModel.EmailInvoker<>))]
[assembly: Invoker(typeof(LoginModel), nameof(LoginModel.Password), typeof(LoginModel.PasswordInvoker<>))]
[assembly: Invoker(typeof(LoginModel), nameof(LoginModel.Online), typeof(LoginModel.OnlineInvoker<>))]
[assembly: Invoker(typeof(LoginModel), nameof(LoginModel.Platform), typeof(LoginModel.PlatformInvoker<>))]
[assembly: Invoker(typeof(LoginModel), nameof(LoginModel.Application), typeof(LoginModel.ApplicationInvoker<>))]
[assembly: Invoker(typeof(LoginModel), nameof(LoginModel.Version), typeof(LoginModel.VersionInvoker<>))]
namespace DataWF.Common
{
    public class LoginModel : DefaultItem
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

        public class EmailInvoker<T> : Invoker<T, string> where T : LoginModel
        {
            public override string Name => nameof(Email);

            public override bool CanWrite => true;

            public override string GetValue(T target) => target.Email;

            public override void SetValue(T target, string value) => target.Email = value;
        }

        public class PasswordInvoker<T> : Invoker<T, string> where T : LoginModel
        {
            public override string Name => nameof(Password);

            public override bool CanWrite => true;

            public override string GetValue(T target) => target.Password;

            public override void SetValue(T target, string value) => target.Password = value;
        }

        public class OnlineInvoker<T> : Invoker<T, bool?> where T : LoginModel
        {
            public override string Name => nameof(Online);

            public override bool CanWrite => true;

            public override bool? GetValue(T target) => target.Online;

            public override void SetValue(T target, bool? value) => target.Online = value;
        }

        public class PlatformInvoker<T> : Invoker<T, string> where T : LoginModel
        {
            public override string Name => nameof(Platform);

            public override bool CanWrite => true;

            public override string GetValue(T target) => target.Platform;

            public override void SetValue(T target, string value) => target.Platform = value;
        }

        public class ApplicationInvoker<T> : Invoker<T, string> where T : LoginModel
        {
            public override string Name => nameof(Application);

            public override bool CanWrite => true;

            public override string GetValue(T target) => target.Application;

            public override void SetValue(T target, string value) => target.Application = value;
        }

        public class VersionInvoker<T> : Invoker<T, string> where T : LoginModel
        {
            public override string Name => nameof(Version);

            public override bool CanWrite => true;

            public override string GetValue(T target) => target.Version;

            public override void SetValue(T target, string value) => target.Version = value;
        }

    }
}
