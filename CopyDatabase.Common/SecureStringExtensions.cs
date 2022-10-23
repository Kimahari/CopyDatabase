using System.Runtime.InteropServices;

namespace CopyDatabase.Common;

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