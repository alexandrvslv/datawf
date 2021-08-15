using DataWF.Common;
using System;
using System.ComponentModel.DataAnnotations;

namespace DataWF.Common
{
    [InvokerGenerator]
    public partial class TokenModel : DefaultItem
    {
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
    }
}
