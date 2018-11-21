using DataBaseCompare.Models;
using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace DataBaseCompare.Tools {

    public static class Extensions {

        #region Methods

        public static string BuildConnection(this ConnectionModel con, string databaseName = "") {
            var intergrated = String.IsNullOrEmpty(con.UserName) ? "SSPI" : "False";

            StringBuilder builder = new StringBuilder($"Data Source={con.serverInstance};Integrated Security={intergrated};");

            if (!String.IsNullOrEmpty(con.UserName))
                builder.Append($"User ID={con.UserName};Password={con.Password.SecureStringToString()};");

            if (!String.IsNullOrEmpty(databaseName))
                builder.Append($"Initial Catalog={databaseName};");

            return builder.ToString();
        }

        public static String SecureStringToString(this SecureString value) {
            if (value == null) return string.Empty;

            IntPtr valuePtr = IntPtr.Zero;

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
