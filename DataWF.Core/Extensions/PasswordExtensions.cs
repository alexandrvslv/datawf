using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DataWF;

public static partial class PasswordExtensions
{
    [GeneratedRegex(@"(?=.*\d)^.*")]
    public static partial Regex NumberRegex();

    [GeneratedRegex(@"(?=.*[A-Z])^.*")]
    public static partial Regex UppercaseRegex();

    [GeneratedRegex(@"(?=.*[a-z])^.*")]
    public static partial Regex LowercaseRegex();

    [GeneratedRegex(@"(?=.*[\W_])^.*")]
    public static partial Regex SpecialCharRegex();

    [GeneratedRegex(@"(.)\1{2,}")]
    public static partial Regex CharRepetRegex();

    public static IEnumerable<string> PasswordVerification(this string password, string? login = null,
        PasswordSpecification specification = PasswordSpecification.Lenght6 | PasswordSpecification.CharSpecial | PasswordSpecification.CharNumbers)
    {
        if (password == null)
        {
            yield return "Must be not NULL";
            yield break;
        }
        if (specification.HasFlag(PasswordSpecification.Lenght6)
            && password.Length < 6)
            yield return Locale.Get("Login", " Must be more than 6 characters long.");
        if (specification.HasFlag(PasswordSpecification.Lenght8)
            && password.Length < 8)
            yield return Locale.Get("Login", " Must be more than 8 characters long.");
        if (specification.HasFlag(PasswordSpecification.Lenght10)
            && password.Length < 10)
            yield return Locale.Get("Login", " Must be more than 10 characters long.");
        if (specification.HasFlag(PasswordSpecification.CharNumbers)
            && !NumberRegex().IsMatch(password))
            yield return Locale.Get("Login", " Should contain a number.");
        if (specification.HasFlag(PasswordSpecification.CharUppercase)
            && !UppercaseRegex().IsMatch(password))
            yield return Locale.Get("Login", " Should contain a uppercase.");
        if (specification.HasFlag(PasswordSpecification.CharLowercase)
            && !LowercaseRegex().IsMatch(password))
            yield return Locale.Get("Login", " Should contain a lowercase letters.");
        if (specification.HasFlag(PasswordSpecification.CharSpecial)
            && !SpecialCharRegex().IsMatch(password))
            yield return Locale.Get("Login", " Should contain a special character.");
        if (specification.HasFlag(PasswordSpecification.CharRepet)
            && CharRepetRegex().IsMatch(password))
            yield return Locale.Get("Login", " Remove repeted characters.");
        if (specification.HasFlag(PasswordSpecification.Login)
            && login != null
            && password.IndexOf(login, StringComparison.OrdinalIgnoreCase) >= 0)
            yield return Locale.Get("Login", " Should not contain your Login.");
    }
}

