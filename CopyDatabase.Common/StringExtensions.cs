namespace CopyDatabase.Common;

public static class StringExtensions {
    public static SecureString ToSecureString(this string value) {
        if (value == null) return new SecureString();

        var securePassword = new SecureString();

        foreach (char c in value)
            securePassword.AppendChar(c);

        securePassword.MakeReadOnly();

        return securePassword;
    }
}
