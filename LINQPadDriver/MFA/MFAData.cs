using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.ServiceModel.Description;
using System.Text;
using System.Threading.Tasks;

namespace TcmLINQPadDriver.MFA
{
    internal sealed class NativeMethods
    {
        #region enums
        internal enum ErrorFlags
        {
            ERROR_INSUFFICIENT_BUFFER = 122,
            ERROR_INVALID_PARAMETER = 87,
            ERROR_NO_MORE_ITEMS = 259
        }

        internal enum InternetFlags
        {
            INTERNET_COOKIE_HTTPONLY = 8192, //Requires IE 8 or higher   
            INTERNET_COOKIE_THIRD_PARTY = 131072,
            INTERNET_FLAG_RESTRICTED_ZONE = 16
        }

        internal const int URLMON_OPTION_USERAGENT = 0x10000001;
        internal const int URLMON_OPTION_USERAGENT_REFRESH = 0x10000002;
        #endregion

        #region DLL Imports
        [SuppressUnmanagedCodeSecurity, SecurityCritical, DllImport("wininet.dll", EntryPoint = "InternetGetCookieExW", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
        internal static extern bool InternetGetCookieEx([In] string Url, [In] string cookieName, [Out] StringBuilder cookieData, [In, Out] ref uint pchCookieData, uint flags, IntPtr reserved);

        [SuppressUnmanagedCodeSecurity, SecurityCritical, DllImport("urlmon.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        internal static extern int UrlMkSetSessionOption(int dwOption, string pBuffer, int dwBufferLength, int dwReserved);
        #endregion
    }

    public class MFAData
    {
        private const String USER_AGENT = "Mozilla/5.0 (Windows NT 6.3; WOW64; Trident/7.0; Touch; rv:11.0) like Gecko";

        Uri url;
        String cookieData;
        DateTime expires;

        [SecurityCritical]
        private static void SetUserAgent(String userAgent)
        {
            NativeMethods.UrlMkSetSessionOption(NativeMethods.URLMON_OPTION_USERAGENT_REFRESH, null, 0, 0);
            NativeMethods.UrlMkSetSessionOption(NativeMethods.URLMON_OPTION_USERAGENT, userAgent, userAgent.Length, 0);
        }

        private void ObtainCookies()
        {
            // Prompt user to login through multi-factor authentication
            SetUserAgent(USER_AGENT);
            new BrowserDialog(url).ShowDialog();

            String cookies = WebBrowserCookies.GetCookieInternal(url, false);

            if (String.IsNullOrEmpty(cookies)) {
                throw new Exception("Unable to obtain multi-factor authentication cookies from the browser.");
            }

            this.cookieData = cookies;
            this.expires = DateTime.Now.AddHours(3);
        }

        private String GetAuthenticationCookies()
        {
            // Obtain new cookies if they are expired
            if (DateTime.Now > expires) {
                ObtainCookies();
            }

            return cookieData;
        }

        public MFAData(Uri url)
        {
            this.url = url;
        }

        public IContractBehavior GetContractBehavior()
        {
            return new MFA.CookieManagerMessageInspector(USER_AGENT, GetAuthenticationCookies());
        }
    }
}
