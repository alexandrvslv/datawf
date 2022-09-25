using DataWF.Common;
using System;
using System.ComponentModel.DataAnnotations;

[assembly: Invoker(typeof(TokenModel), nameof(TokenModel.Email), typeof(TokenModel.EmailInvoker<>))]
[assembly: Invoker(typeof(TokenModel), nameof(TokenModel.AccessToken), typeof(TokenModel.AccessTokenInvoker<>))]
[assembly: Invoker(typeof(TokenModel), nameof(TokenModel.RefreshToken), typeof(TokenModel.RefreshTokenInvoker<>))]
namespace DataWF.Common
{
    public class TokenModel : DefaultItem
    {
        private int? id;
        private string email;
        private string accessToken;
        private string refreshToken;

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

        [DataType(DataType.Password)]
        public string AccessToken
        {
            get => accessToken;
            set
            {
                if (!string.Equals(accessToken, value, StringComparison.Ordinal))
                {
                    var oldValue = accessToken;
                    accessToken = value;
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        public string RefreshToken
        {
            get => refreshToken;
            set
            {
                if (!string.Equals(refreshToken, value, StringComparison.Ordinal))
                {
                    var oldValue = refreshToken;
                    refreshToken = value;
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        public int? Id
        {
            get => id;
            set
            {
                var temp = id;
                id = value;
                OnPropertyChanged(temp, value);
            }
        }
        public class IdInvoker<T> : Invoker<T, int?> where T : TokenModel
        {
            public override string Name => nameof(Id);

            public override bool CanWrite => true;

            public override int? GetValue(T target) => target.Id;

            public override void SetValue(T target, int? value) => target.Id = value;
        }

        public class EmailInvoker<T> : Invoker<T, string> where T : TokenModel
        {
            public override string Name => nameof(Email);

            public override bool CanWrite => true;

            public override string GetValue(T target) => target.Email;

            public override void SetValue(T target, string value) => target.Email = value;
        }

        public class AccessTokenInvoker<T> : Invoker<T, string> where T : TokenModel
        {
            public override string Name => nameof(AccessToken);

            public override bool CanWrite => true;

            public override string GetValue(T target) => target.AccessToken;

            public override void SetValue(T target, string value) => target.AccessToken = value;
        }

        public class RefreshTokenInvoker<T> : Invoker<T, string> where T : TokenModel
        {
            public override string Name => nameof(RefreshToken);

            public override bool CanWrite => true;

            public override string GetValue(T target) => target.RefreshToken;

            public override void SetValue(T target, string value) => target.RefreshToken = value;
        }

    }
}
