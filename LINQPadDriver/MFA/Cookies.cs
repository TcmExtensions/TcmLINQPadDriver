using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using MFA.Chrome.Parse;
using MFA.Chrome.Parse.SQLite;

namespace MFA.Chrome
{
    public class EncryptedValue
    {
        private const int SIG_LEN = 3;
        private const int IV_LEN = 12;
        private const int TAG_LEN = 16;

        public byte[] Signature { get; private set; }
        public byte[] IV { get; private set; }
        public byte[] CipherText { get; private set; }
        public byte[] AuthTag { get; private set; }

        internal EncryptedValue(byte[] encryptedData)
        {
            if (encryptedData.Length < 31)
                return;

            this.Signature = new byte[SIG_LEN];
            this.AuthTag = new byte[TAG_LEN];
            this.IV = new byte[IV_LEN];
            this.CipherText = new byte[encryptedData.Length - (SIG_LEN + IV_LEN + TAG_LEN)];

            Array.Copy(encryptedData, 0, this.Signature, 0, SIG_LEN);
            Array.Copy(encryptedData, SIG_LEN, this.IV, 0, IV_LEN);
            Array.Copy(encryptedData, SIG_LEN + IV_LEN, this.CipherText, 0, this.CipherText.Length);
            Array.Copy(encryptedData, encryptedData.Length - TAG_LEN, this.AuthTag, 0, TAG_LEN);
        }
    }

    public class Cookie
    {
        internal const int FIELD_HOST = 1;
        internal const int FIELD_NAME = 3;
        internal const int FIELD_VALUE = 5;
        internal const int FIELD_PATH = 6;
        internal const int FIELD_EXPIRES = 7;
        internal const int FIELD_ISSECURE = 8;
        internal const int FIELD_ISHTTPONLY = 9;

        public String Host { get; private set; }
        public String Name { get; private set; }

        public EncryptedValue EncryptedValue { get; private set; }

        public String Value { get; internal set; }

        public Boolean IsSecure { get; private set; }
        public Boolean IsHttpOnly { get; private set; }

        public DateTime Expires { get; private set; }

        internal Cookie(String host, String name, String path, byte[] encryptedValue, int isSecure, int isHttpOnly, ulong expiresUtc)
        {
            this.Host = host;
            this.Name = name;

            this.EncryptedValue = new EncryptedValue(encryptedValue);

            this.IsSecure = isSecure != 0;
            this.IsHttpOnly = isHttpOnly != 0;

            
            this.Expires = DateTime.MaxValue;

            if (expiresUtc > 0) {
                expiresUtc = (expiresUtc / 1000000) - 11644473600;

                // Handle dates till 01-01-2100
                if (expiresUtc < 4102430400) {
                    DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    this.Expires = epoch.AddSeconds(expiresUtc);
                }
            }
        }
    }

    public static class Cookies
    {
        private static byte[] GetKey()
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Google\Chrome\User Data\Local State");

            Dictionary<String, Object> json = (Dictionary<String, Object>)JSONParser.FromJson<Object>(File.ReadAllText(path));

            Object value;

            if (json.TryGetValue("os_crypt", out value)) {

                Dictionary<String, Object> crypt = (Dictionary<String, Object>)value;

                if (crypt.TryGetValue("encrypted_key", out value)) {
                    String key = (String)value;

                    byte[] src = Convert.FromBase64String(key);
                    byte[] encryptedKey = src.Skip(5).ToArray();

                    return ProtectedData.Unprotect(encryptedKey, null, DataProtectionScope.CurrentUser);
                }
            }

            return null;
        }

        public static Dictionary<String, Cookie> Get(String host)
        {
            String cookiesDatabase = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Google\Chrome\User Data\Default\Network\Cookies");

            Dictionary<String, Cookie> cookies = new Dictionary<String, Cookie>(StringComparer.InvariantCultureIgnoreCase);
            byte[] key = GetKey();

            if (File.Exists(cookiesDatabase) && key != null) {

                using (SqliteFileParser parser = new SqliteFileParser(cookiesDatabase)) {
                    parser.ReportBlobSizesOnly = false;

                    using (SqliteFileReader reader = new SqliteFileReader(parser)) {
                        reader.TableRecordRead += (s, e) => {

                            if (e.Fields[Cookie.FIELD_VALUE].Type == FieldType.Blob) {
                                Cookie cookie = new Cookie(
                                    (string)e.Fields[Cookie.FIELD_HOST].Value,
                                    (string)e.Fields[Cookie.FIELD_NAME].Value,
                                    (string)e.Fields[Cookie.FIELD_PATH].Value,
                                    (byte[])e.Fields[Cookie.FIELD_VALUE].Value,
                                    (int)e.Fields[Cookie.FIELD_ISSECURE].Value,
                                    (int)e.Fields[Cookie.FIELD_ISHTTPONLY].Value,
                                    e.Fields[Cookie.FIELD_EXPIRES].Type == FieldType.Integer64 ? (ulong)e.Fields[Cookie.FIELD_EXPIRES].Value : 0);

                                if (cookie.Host.EndsWith(host, StringComparison.OrdinalIgnoreCase)) {

                                    if (cookie.EncryptedValue.Signature.SequenceEqual(new byte[] { 0x76, 0x31, 0x30 })) {
                                        byte[] value = Crypt.AesGcm.Decrypt(
                                            key,
                                            cookie.EncryptedValue.IV,
                                            null,
                                            cookie.EncryptedValue.CipherText,
                                            cookie.EncryptedValue.AuthTag);

                                        cookie.Value = Encoding.ASCII.GetString(value);
                                        cookies.Add(cookie.Name, cookie);
                                    }
                                }
                            }
                            
                        };

                        reader.ReadTable("cookies");

                    }
                }
            }

            return cookies;
        }
    }
}

