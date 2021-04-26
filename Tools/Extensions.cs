using System;
using System.Runtime.InteropServices;
using System.Security;

namespace DataBaseCompare.Tools {

    public static class Extensions {

        #region Public Methods

        public static string SecureStringToString(this SecureString value) {
            if (value == null) {
                return string.Empty;
            }

            IntPtr valuePtr = IntPtr.Zero;

            try {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(value);
                return Marshal.PtrToStringUni(valuePtr);
            } finally {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }

        public static SecureString ToSecureString(this string @string) {
            SecureString secure = new SecureString();
            foreach (char c in @string) {
                secure.AppendChar(c);
            }

            return secure;
        }

        #endregion Public Methods
    }
}
