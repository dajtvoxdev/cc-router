using System;
using System.Security.Cryptography;
using System.Text;

namespace CCRouter.Services;

public static class SecretProtector
{
    public static string Protect(string plainText)
    {
        var bytes = Encoding.UTF8.GetBytes(plainText);
        var encrypted = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(encrypted);
    }

    public static string Unprotect(string base64Cipher)
    {
        var encrypted = Convert.FromBase64String(base64Cipher);
        var bytes = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(bytes);
    }
}
