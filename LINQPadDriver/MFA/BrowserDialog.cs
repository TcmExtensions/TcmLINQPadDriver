using System;
using System.Windows.Forms;

namespace TcmLINQPadDriver.MFA
{
    public partial class BrowserDialog : Form
    {
        public BrowserDialog(Uri url)
        {
            InitializeComponent();
            this.webBrowser.Url = url;
        }

        public string Cookies()
        {
            return this.webBrowser.Document.Cookie;
        }
    }
}
