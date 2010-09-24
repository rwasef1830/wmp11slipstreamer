using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Epsilon.WMP11Slipstreamer.Localization;

namespace Epsilon.WMP11Slipstreamer
{
    partial class AboutBox : Form
    {
        public AboutBox()
        {
            this.InitializeComponent();
            this.ReadLocalizedMessages();

            this.uxLabelVersion.Text = String.Format("Version {0}", Globals.Version);
            this.uxLabelTranslator.Text += " " + Msg.LocalizerName;
        }

        void ReadLocalizedMessages()
        {
            this.SuspendLayout();
            this.uxButtonOk.Text = Msg.dlgAbout_ButtonOK;
            this.uxLabelTranslator.Text = Msg.dlgAbout_uxLabelTranslated;
            this.uxLinkLabelWebSite.Text = Msg.dlgAbout_GotoSite;
            this.uxTextBoxDescription.Text = Msg.dlgAbout_InfoText;
            this.uxButtonUsageInfo.Text = Msg.uxButtonUsageInfo;
            this.ResumeLayout();
        }

        static void uxLinkLabelWebSite_LinkClicked(
            object sender,
            LinkLabelLinkClickedEventArgs e)
        {
            CM.LaunchInDefaultHandler(Globals.WebsiteUrl);
        }

        static void uxButtonUsageInfo_Click(object sender, EventArgs e)
        {
            Globals.ShowUsageInformation();
        }

        #region Assembly Attribute Accessors
        public static string AssemblyTitle
        {
            get
            {
                // Get all Title attributes on this assembly
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(
                    typeof(AssemblyTitleAttribute), false);
                // If there is at least one Title attribute
                if (attributes.Length > 0)
                {
                    // Select the first one
                    var titleAttribute = (AssemblyTitleAttribute)attributes[0];
                    // If it is not an empty string, return it
                    if (titleAttribute.Title.Length > 0)
                        return titleAttribute.Title;
                }
                // If there was no Title attribute, or if the Title attribute was the empty string, return the .exe name
                return Path.GetFileNameWithoutExtension(
                    Assembly.GetExecutingAssembly().CodeBase);
            }
        }

        public static string AssemblyVersion
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version.ToString(); }
        }

        public static string AssemblyDescription
        {
            get
            {
                // Get all Description attributes on this assembly
                object[] attributes =
                    Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
                // If there aren't any Description attributes, return an empty string
                if (attributes.Length == 0)
                    return "";
                // If there is a Description attribute, return its value
                return ((AssemblyDescriptionAttribute)attributes[0]).Description;
            }
        }

        public static string AssemblyProduct
        {
            get
            {
                // Get all Product attributes on this assembly
                object[] attributes =
                    Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                // If there aren't any Product attributes, return an empty string
                if (attributes.Length == 0)
                    return "";
                // If there is a Product attribute, return its value
                return ((AssemblyProductAttribute)attributes[0]).Product;
            }
        }

        public static string AssemblyCopyright
        {
            get
            {
                // Get all Copyright attributes on this assembly
                object[] attributes =
                    Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
                // If there aren't any Copyright attributes, return an empty string
                if (attributes.Length == 0)
                    return "";
                // If there is a Copyright attribute, return its value
                return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
            }
        }

        public static string AssemblyCompany
        {
            get
            {
                // Get all Company attributes on this assembly
                object[] attributes =
                    Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
                // If there aren't any Company attributes, return an empty string
                if (attributes.Length == 0)
                    return "";
                // If there is a Company attribute, return its value
                return ((AssemblyCompanyAttribute)attributes[0]).Company;
            }
        }
        #endregion
    }
}