using System.Runtime.InteropServices;

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


public static class SecureStringExtensions {
    public static string FromSecureString(this SecureString value) {
        IntPtr valuePtr = IntPtr.Zero;
        try {
            valuePtr = Marshal.SecureStringToGlobalAllocUnicode(value);
            return Marshal.PtrToStringUni(valuePtr) ?? "";
        } finally {
            Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
        }
    }
}