using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Threading;
using System.Windows.Forms;
using Epsilon.DebugServices;
using Epsilon.IO;
using Epsilon.n7Framework.Epsilon.Parsers;
using Epsilon.n7Framework.Epsilon.Slipstreamers;
using Epsilon.WMP11Slipstreamer.Localization;

namespace Epsilon.WMP11Slipstreamer
{
    static class EntryPoint
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static int Main(string[] args)
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                StartDebugConsole();

                var argParser = new ArgumentParser(args);
                argParser.Parse(
                    0,
                    10,
                    0,
                    0,
                    new[] { "nocats", "slipstream", "closeonsuccess" },
                    new[]
                    {
                        "installer", "winsource", "customicon", "output",
                        "hotfix", "customiconpath", "resdir", "culture"
                    },
                    false
                    );

                string installer = argParser.GetValue("installer");
                string winsource = argParser.GetValue("winsource");
                string output = argParser.GetValue("output");
                string hotfixes = argParser.GetValue("hotfix");
                string customicon = argParser.GetValue("customicon");
                bool nocats = argParser.IsSpecified("nocats");
                bool slipstream = argParser.IsSpecified("slipstream");
                bool closeonsuccess
                    = argParser.IsSpecified("closeonsuccess") && slipstream;
                string customiconpath = argParser.GetValue("customiconpath");

                string culture = argParser.GetValue("culture");
                string resDir = argParser.GetValue("resdir");

                if (!String.IsNullOrEmpty(resDir)
                    && !String.IsNullOrEmpty(culture))
                {
                    throw new ArgumentParserException(
                        "/culture and /resDir cannot be specified together.");
                }

                if (!String.IsNullOrEmpty(resDir))
                {
                    if (!Directory.Exists(resDir))
                    {
                        throw new DirectoryNotFoundException(
                            "The resource directory could not be found.");
                    }

                    var resClassTypes = new[]
                    {
                        typeof(Msg),
                        typeof(SlipstreamersMsg),
                        typeof(ParsersMsg)
                    };

                    foreach (Type resClassType in resClassTypes)
                    {
                        if (File.Exists(
                            FileSystem.CreatePathString(
                                resDir, resClassType.Name + ".resources")))
                        {
                            ResourceManager resMan
                                = ResourceManager.CreateFileBasedResourceManager(
                                    resClassType.Name, resDir, null);
                            FieldInfo resManInfo = resClassType.GetField(
                                "resourceMan",
                                BindingFlags.Static | BindingFlags.NonPublic);
                            resManInfo.SetValue(resClassType, resMan);
                        }
                    }
                }
                else if (!String.IsNullOrEmpty(culture))
                {
                    try
                    {
                        CultureInfo overrideCulture = CultureInfo.GetCultureInfo(culture);
                        Assembly.GetExecutingAssembly()
                            .GetSatelliteAssembly(
                                overrideCulture,
                                new Version(
                                    ((SatelliteContractVersionAttribute)(
                                                                            Assembly.GetExecutingAssembly().
                                                                            GetCustomAttributes(
                                                                                typeof(SatelliteContractVersionAttribute
                                                                                    ),
                                                                                false)[0]))
                                        .Version));
                        Thread.CurrentThread.CurrentUICulture = overrideCulture;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            "An error occurred while overriding the culture of the application. Maybe the culture string is invalid."
                            + Environment.NewLine + Environment.NewLine + ex.Message,
                            "Culture override failed",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }

                ValidateResource();
                Application.Run(
                    new MainForm(
                        installer,
                        winsource,
                        hotfixes,
                        output,
                        customicon,
                        nocats,
                        slipstream,
                        closeonsuccess,
                        customiconpath));

                return 0;
            }
            catch (ShowUsageException)
            {
                Globals.ShowUsageInformation();
                return 1;
            }
            catch (ArgumentParserException ex)
            {
                MessageBox.Show(
                    ex.Message
                    + Environment.NewLine + Environment.NewLine
                    + "Click \"OK\" to view usage information.",
                    "Argument Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                Globals.ShowUsageInformation();
                return 1;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Unhandled Exception in UI Thread");
                return 2;
            }
        }

        [Conditional("DEBUG")]
        static void StartDebugConsole()
        {
            string filename
                = CM.SaveFileDialogStandard(
                    "Choose location to save debug log...",
                    "Log files (*.log)|*.log|All files (*.*)|*.*");
            HelperConsole.InitializeDefaultConsole(filename);
        }

        static void ValidateResource()
        {
            if (!String.Equals(
                Msg.LocalizerKey,
                Globals.LocalizerKey,
                StringComparison.Ordinal))
            {
                MessageBox.Show(
                    "Localizer key mismatch. Switching back to default culture.",
                    "Satellite assembly error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                    );
                ResetCultureToDefault();
            }
        }

        static void ResetCultureToDefault()
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(
                ((NeutralResourcesLanguageAttribute)(
                                                        Assembly.GetExecutingAssembly().GetCustomAttributes(
                                                            typeof(NeutralResourcesLanguageAttribute), false)[0])).
                    CultureName);
        }
    }
}