using System.Windows;
using System.Xml.Linq;
using LINQPad.Extensibility.DataContext;

namespace TcmLINQPadDriver
{
    /// <summary>
    /// Interaction logic for ConnectionDialog.xaml
    /// </summary>
    public partial class ConnectionDialog : Window
	{
        
		IConnectionInfo _cxInfo;

		public ConnectionDialog (IConnectionInfo cxInfo)
		{
			_cxInfo = cxInfo;
			DataContext = cxInfo.CustomTypeInfo;
            InitializeComponent();
            if (cxInfo.DriverData.Attribute("Hostname") != null) txtTridionHost.Text = cxInfo.DriverData.Attribute("Hostname").Value;
            if (cxInfo.DriverData.Attribute("Username") != null) txtUsername.Text = cxInfo.DriverData.Attribute("Username").Value;
            if (cxInfo.DriverData.Attribute("Secure") != null) chkSecure.IsChecked = System.Boolean.Parse(cxInfo.DriverData.Attribute("Secure").Value);
            if (cxInfo.DriverData.Attribute("MFA") != null) chkMFA.IsChecked = System.Boolean.Parse(cxInfo.DriverData.Attribute("MFA").Value);
            if (cxInfo.DriverData.Attribute("Password") != null) txtPassword.Password = cxInfo.DriverData.Attribute("Password").Value;
            if (cxInfo.DriverData.Attribute("Context") != null) txtContext.Text = cxInfo.DriverData.Attribute("Context").Value;
		}

		void btnOK_Click (object sender, RoutedEventArgs e)
		{
            _cxInfo.DriverData = new XElement("TridionCoreService",
                new XAttribute("Hostname", txtTridionHost.Text),
                new XAttribute("Secure", chkSecure.IsChecked),
                new XAttribute("MFA", chkMFA.IsChecked),
                new XAttribute("Username", string.IsNullOrEmpty(txtUsername.Text) ? "" : txtUsername.Text),   
                new XAttribute("Password", string.IsNullOrEmpty(txtPassword.Password) ? "" : txtPassword.Password)
            );

            if (!string.IsNullOrEmpty(txtContext.Text)) {
                _cxInfo.DriverData.Add(new XAttribute("Context", txtContext.Text));
            }

		    DialogResult = true;
		}

        private void chkMFA_Checked(object sender, RoutedEventArgs e)
        {
            txtUsername.Text = "";
            txtUsername.IsEnabled = false;

            txtPassword.Password = "";
            txtPassword.IsEnabled = false;

            chkSecure.IsChecked = true;
            chkSecure.IsEnabled = false;
        }

        private void chkMFA_Unchecked(object sender, RoutedEventArgs e)
        {
            txtUsername.Text = "";
            txtUsername.IsEnabled = true;

            txtPassword.Password = "";
            txtPassword.IsEnabled = true;

            chkSecure.IsChecked = false;
            chkSecure.IsEnabled = true;
        }
    }
}
