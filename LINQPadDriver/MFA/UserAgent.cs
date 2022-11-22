using System;
using System.IO;
using System.Runtime.InteropServices;

namespace MFA.Chrome
{
    internal class UserAgent
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct OsVersionInfo
        {
            private readonly uint OsVersionInfoSize;

            internal readonly uint MajorVersion;
            internal readonly uint MinorVersion;

            private readonly uint BuildNumber;

            private readonly uint PlatformId;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            private readonly string CSDVersion;
        }

        private const String USER_AGENT = "Mozilla/5.0 (Windows NT {0}; {1}) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{2}.0.0.0 Safari/537.36";

        public static String GetUserAgent()
        {
            OsVersionInfo versionInfo;
            uint result = RtlGetVersion(out versionInfo);

            if (result != 0) {
                throw new Exception("UserAgent: Unable to retrieve operating system version.");
            }

            switch (versionInfo.MajorVersion) {
                case 6:
                    return String.Format(USER_AGENT,
                        String.Format("{0}.{1}",
                            versionInfo.MajorVersion,
                            versionInfo.MinorVersion),
                        GetOSArchitecture(),
                        GetChromeVersion());
                case 10:
                    return String.Format(USER_AGENT,
                        String.Format("{0}.{1}",
                           versionInfo.MajorVersion,
                           versionInfo.MinorVersion),
                        "WOW64",
                        GetChromeVersion());
            }

            throw new NotImplementedException("UserAgent: does not support this operating system version.");
        }

        private static String GetChromeVersion()
        {
            String path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Google\Chrome\User Data\Last Version");

            if (!File.Exists(path)) {
                throw new FileNotFoundException(@"Unable to read Chrome\User Data\LastVersion");
            }

            String version = File.ReadAllText(path);
            return version.Substring(0, version.IndexOf('.'));
        }

        private static String GetOSArchitecture()
        {
            // Determine if we are a WoW64 process
            bool isWow64;
            IsWow64Process(GetCurrentProcess(), out isWow64);

            if (isWow64) {
                return "WOW64";
            }

            if (Environment.Is64BitOperatingSystem) {
                return "Win64; x64";
            }

            return "Win32; x32";
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWow64Process(IntPtr hProcess, out bool wow64Process);

        [DllImport("ntdll.dll", SetLastError = true)]
        internal static extern uint RtlGetVersion(out OsVersionInfo versionInformation); // return type should be the NtStatus enum
    }
}
