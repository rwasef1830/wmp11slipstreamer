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
                    new string[] { "nocats", "slipstream", "closeonsuccess" },
                    new string[] { "installer", "winsource", "customicon", "output", 
                        "hotfix", "customiconpath" },
                    0,
                    false
                );

                string installer;
                argParser.ParamsTable.TryGetValue("installer", out installer);
                string winsource;
                argParser.ParamsTable.TryGetValue("winsource", out winsource);
                string output;
                argParser.ParamsTable.TryGetValue("output", out output);
                string hotfixes;
                argParser.ParamsTable.TryGetValue("hotfix", out hotfixes);
                string customicon;
                argParser.ParamsTable.TryGetValue("customicon", out customicon);
                bool nocats = argParser.ParamsTable.ContainsKey("nocats");
                bool slipstream = argParser.ParamsTable.ContainsKey("slipstream");
                bool closeonsuccess =
                    argParser.ParamsTable.ContainsKey("closeonsuccess") && slipstream;
                string customiconpath;
                argParser.ParamsTable.TryGetValue("customiconpath", out customiconpath);

                Application.Run(new MainForm(installer, winsource, hotfixes,
                    output, customicon, nocats, slipstream, closeonsuccess, 
                    customiconpath));
                return errorlevel;
            }
            catch (ArgumentException Ex)
            {
                MessageBox.Show(Ex.Message
                    + Environment.NewLine + Environment.NewLine
                    + "Click \"OK\" to view usage information."
                    , "Argument Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                string text = Properties.Resources.UsageInformation;
                MessageBox.Show(text, "Usage Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return errorlevel;
            }
            catch (ShowUsageException)
            {
                string text = Properties.Resources.UsageInformation;
                MessageBox.Show(text, "Usage Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return errorlevel;
            }
            catch (Exception Ex)
            {
                MessageBox.Show(Ex.ToString(), "Unhandled Exception in UI Thread");
                errorlevel = 1;
                return errorlevel;
            }
        }

        internal static int errorlevel = 0;
    }
}