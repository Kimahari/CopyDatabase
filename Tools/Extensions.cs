using DataBaseCompare.Models;
using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace DataBaseCompare.Tools {

    public static class Extensions {

        #region Methods

        public static String SecureStringToString(this SecureString value) {
            if (value == null) return string.Empty;

            var valuePtr = IntPtr.Zero;

            try {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(value);
                return Marshal.PtrToStringUni(valuePtr);
            } finally {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }

        public static SecureString ToSecureString(this string @string) {
            var secure = new SecureString();
            foreach (char c in @string) {
                secure.AppendChar(c);
            }

            return secure;
        }

        #endregion Methods
    }
}
