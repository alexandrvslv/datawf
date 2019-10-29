using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace DataWF.Common
{
    public class JwtSetting
    {
        public static JwtSetting Current { get; private set; }

        public JwtSetting()
        {
            Current = this;
        }

        public string SecurityKey { get; set; }
        public string ValidIssuer { get; set; }
        public string ValidAudience { get; set; }
        public int LifeTime { get; set; } = 30;

        public SymmetricSecurityKey SymmetricSecurityKey => new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecurityKey));
        public SigningCredentials SigningCredentials => new SigningCredentials(SymmetricSecurityKey, SecurityAlgorithms.HmacSha256);

    }
}
