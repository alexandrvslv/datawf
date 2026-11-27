using DataWF.Invokers;
using DataWF.Serialization;
using System;
using System.ComponentModel.DataAnnotations;

namespace DataWF;

[ElementSerializer]
public partial class TokenModel : DefaultItem
{
    private string email;
    private string accessToken;
    private string refreshToken;

    public string Email
    {
        get => email;
        set => ChangeProperty(ref email, value);
    }

    [DataType(DataType.Password)]
    public string AccessToken
    {
        get => accessToken;
        set => ChangeProperty(ref accessToken, value);
    }

    public string RefreshToken
    {
        get => refreshToken;
        set => ChangeProperty(ref refreshToken, value);
    }
}
