using DataWF;
using DataWF.Invokers;
using DataWF.Serialization;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace DataWF;

[ElementSerializer]
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
        set => ChangeProperty(ref email, value);
    }

    [Required(ErrorMessage = "Password Is Required!")]
    [DataType(DataType.Password)]
    public string Password
    {
        get => password;
        set => ChangeProperty(ref password, value);
    }

    [DefaultValue(false)]
    public bool? Online
    {
        get => online;
        set => ChangeProperty(ref online, value);
    }

    public string Platform
    {
        get => platform;
        set => ChangeProperty(ref platform, value);
    }

    public string Application
    {
        get => application;
        set => ChangeProperty(ref application, value);
    }

    public string Version
    {
        get => version;
        set => ChangeProperty(ref version, value);
    }
}
