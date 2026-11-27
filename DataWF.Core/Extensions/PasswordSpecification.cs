using System;

namespace DataWF;

[Flags]
public enum PasswordSpecification
{
    None = 0,
    CharNumbers = 2,
    CharUppercase = 4,
    CharLowercase = 8,
    CharSpecial = 16,
    CharRepet = 32,
    Login = 64,
    Lenght6 = 128,
    Lenght8 = 256,
    Lenght10 = 512,
    CheckOld = 1024,
}


