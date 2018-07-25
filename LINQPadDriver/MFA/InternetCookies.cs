using System;
using System.ComponentModel;
using System.Net;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Windows.Controls;

namespace TcmLINQPadDriver.MFA
{

    public class WebBrowserCookies
    {


        [SecurityCritical]
        public static string GetCookieInternal(Uri url, bool throwIfNoCookie)
        {
            uint pchCookieData = 0;
            uint flag = (uint)NativeMethods.InternetFlags.INTERNET_COOKIE_HTTPONLY;

            // Determine required output size
            if (NativeMethods.InternetGetCookieEx(url.ToString(), null, null, ref pchCookieData, flag, IntPtr.Zero)) {
                pchCookieData++; // 0 terminated data
                StringBuilder cookieData = new StringBuilder((int)pchCookieData);

                // Read the cookie   
                if (NativeMethods.InternetGetCookieEx(url.ToString(), null, cookieData, ref pchCookieData, flag, IntPtr.Zero)) {
                    return cookieData.ToString();
                }
            }

            int lastErrorCode = Marshal.GetLastWin32Error();

            if (throwIfNoCookie || (lastErrorCode != (int)NativeMethods.ErrorFlags.ERROR_NO_MORE_ITEMS)) {
                throw new Win32Exception(lastErrorCode);
            }

            return null;
        }
    }
}
