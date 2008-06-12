using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using Epsilon;
#if DEBUG
using Epsilon.DebugServices;
#endif

namespace WMP11Slipstreamer
{
    static class EntryPoint
    {
        static int errorlevel = 0;

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
#if DEBUG
                string filename = CM.SaveFileDialogStandard(
                    "Choose location to save debug log...", "Log files (*.log)|*.log|All files (*.*)|*.*");
                Epsilon.DebugServices.HelperConsole.Initialize(
                    "WMP11Slipstreamer Debug Console");
                Epsilon.DebugServices.HelperConsole.RedirectDebugOutput(false, 
                    ConsoleColor.White, false);
                string title = "** Windows Media Player 11 Slipstreamer v" 
                    + Globals.Version.ToString();
                HelperConsole.InfoWriteLine(title);
                if (!String.IsNullOrEmpty(filename))
                {
                    Epsilon.IO.FileSystem.Delete(filename);
                    ((DefaultTraceListener)Debug.Listeners["Default"]).LogFileName = filename;
                    Debug.Listeners["Default"].WriteLine(title);
                    Debug.WriteLine(String.Format("** Log: \"{0}\"", filename));
                    Debug.WriteLine("** Log created on: "
                        + DateTime.Now.ToUniversalTime().ToString(
                        "dddd dd/MM/yyyy - HH:MM:ss tt \\G\\M\\T"));
                }
                else
                {
                    HelperConsole.WarnWriteLine("** Messages here will be discarded after program terminates.");
                }
                Debug.WriteLine(null);
                HelperConsole.WarnWriteLine("** Closing this window will terminate the application.");
                Debug.WriteLine(null);
#endif

                ArgumentParser argParser = new ArgumentParser(args);
                argParser.Parse(
                    0,
                    9,
                    0,
                    0,
                    new string[] { "nocats", "slipstream", "closeonsuccess" },
                    new string[] { "installer", "winsource", "customicon", "output", 
                        "hotfix", "customiconpath" },
                    false
                );

                string installer = argParser.GetValue("installer");
                string winsource = argParser.GetValue("winsource");
                string output = argParser.GetValue("output");
                string hotfixes = argParser.GetValue("hotfix");
                string customicon = argParser.GetValue("customicon");
                bool nocats = argParser.IsSpecified("nocats");
                bool slipstream = argParser.IsSpecified("slipstream");
                bool closeonsuccess = argParser.IsSpecified("closeonsuccess") && slipstream;
                string customiconpath = argParser.GetValue("customiconpath");

                Application.Run(new MainForm(installer, winsource, hotfixes,
                    output, customicon, nocats, slipstream, closeonsuccess, 
                    customiconpath));
                return errorlevel;
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(ex.Message
                    + Environment.NewLine + Environment.NewLine
                    + "Click \"OK\" to view usage information."
                    , "Argument Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                string text = Properties.Resources.UsageInformation;
                MessageBox.Show(text, "Usage Information", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return errorlevel;
            }
            catch (ShowUsageException)
            {
                string text = Properties.Resources.UsageInformation;
                MessageBox.Show(text, "Usage Information", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return errorlevel;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Unhandled Exception in UI Thread");
                errorlevel = 1;
                return errorlevel;
            }
        }
    }
}