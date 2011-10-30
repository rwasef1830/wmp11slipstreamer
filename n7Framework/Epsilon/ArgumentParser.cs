/* 
 * Argument Parser: More sane
 */

#region Using statements
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
#endregion

namespace Epsilon
{
    public class ArgumentParser
    {
        #region Messages
        const string c_versionString = "4.0";
        const string c_msgTooFew = "Too few parameters!";
        const string c_msgTooMany = "Too many parameters!";
        const string c_msgTooFewSwitchless = "Too few switchless parameters!";
        const string c_msgTooManySwitchless = "Too many switchless parameters!";
        const string c_msgNoSwitchless 
            = "This application does not accept any switchless parameters.";
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="args">Array containing arguments to process</param>
        public ArgumentParser(string[] args)
        {
            this._args = args;
            for (int i = 0; i < args.Length; i++)
                this._args[i] = this._args[i].Trim();

            this._switchlessList = new List<string>(5);
            this._argDict = new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);
        }
        #endregion

        #region Public members
        public readonly Version Version;

        public ReadOnlyCollection<string> SwitchlessParams
        {
            get { return this._switchlessList.AsReadOnly(); }
        }
        #endregion

        #region Private members
        string[] _args;
        Dictionary<string, string> _argDict;
        List<string> _switchlessList;
        #endregion

        /// <summary>
        /// Simpler method for parsing arguments.
        /// </summary>
        /// <param name="minParams">Minimum no. of mandatory arguments 
        /// that should be present, any less on the command line will throw 
        /// an exception.</param>
        /// <param name="maxParams">Absolute maximum no. of arguments that could 
        /// be possibly present, any more on the command line will throw 
        /// an exception.</param>
        /// <param name="minSwitchless">Minimum number of switchless arguments allowed.</param>
        /// <param name="maxSwitchless">Maximum number of switchless arguments allowed.</param>
        /// <param name="normalAllowed">Array of allowed parameters that do not 
        /// accept a value</param>
        /// <param name="complexAllowed">Array of allowed parameters that 
        /// must accept a value</param>
        /// <param name="throwOnZeroParameters">true to throw ShowUsageException 
        /// when no parameters are specified.</param>
        public void Parse(int minParams, int maxParams, int minSwitchless, int maxSwitchless, 
            string[] normalAllowed, string[] complexAllowed, bool throwOnZeroParameters)
        {
            Parse(minParams, maxParams, minSwitchless, maxSwitchless, normalAllowed, 
                complexAllowed, throwOnZeroParameters, '/', ':');
        }

        /// <summary>
        /// Advanced method for parsing arguments.
        /// </summary>
        /// <param name="minParams">Minimum no. of mandatory arguments 
        /// that should be present, any less on the command line will throw 
        /// an exception.</param>
        /// <param name="maxParams">Absolute maximum no. of arguments that could 
        /// be possibly present, any more on the command line will throw 
        /// an exception.</param>
        /// <param name="minSwitchless">Minimum number of switchless arguments allowed.</param>
        /// <param name="maxSwitchless">Maximum number of switchless arguments allowed.</param>
        /// <param name="normalAllowed">Array of allowed parameters that do not 
        /// accept a value</param>
        /// <param name="complexAllowed">Array of allowed parameters that 
        /// must accept a value</param>
        /// <param name="throwOnZeroParameters">true to throw ShowUsageException 
        /// when no parameters are specified</param>
        /// <param name="argSwitch">The character used to indicate a 
        /// paramter eg: '/' as in '/arg' (Hint: For Linux use '-')</param>
        /// <param name="argValueDelimiter">The character that separates 
        /// the variable from its value in a complex parameter eg: ':' 
        /// as in '/var:value'</param>
        public void Parse(int minParams, int maxParams, int minSwitchless, int maxSwitchless, 
            string[] normalAllowed, string[] complexAllowed, bool throwOnZeroParameters, 
            char argSwitch, char argValueDelimiter)
        {
            if (this._args.Length == 0)
                if (throwOnZeroParameters)
                    throw new ShowUsageException();

            foreach (string helpArg in new string[] { "/?", "-?", "--help" })
                if (Array.Exists<string>(this._args, delegate(string p)
                {
                    return String.Equals(p, helpArg, 
                        StringComparison.OrdinalIgnoreCase);
                }))
                {
                    throw new ShowUsageException();
                }

            // Check length
            if (this._args.Length < minParams) 
                throw new ArgumentParserException(c_msgTooFew);
            if (maxParams > 0 && this._args.Length > maxParams) 
                throw new ArgumentParserException(c_msgTooMany);

            // Process parameters
            for (int i = 0; i < this._args.Length; i++)
            {
                string currentValue = Environment.ExpandEnvironmentVariables(this._args[i]);

                if (currentValue[0] == argSwitch && currentValue.Length > 1)
                {
                    string argName, argValue;
                    argName = argValue = null;
                    int argVDelimPos = currentValue.IndexOf(argValueDelimiter, 1);
                    if (argVDelimPos > 0)
                    {
                        argName = currentValue.Substring(1, argVDelimPos - 1);
                        argValue = currentValue.Substring(argVDelimPos + 1);
                    }
                    else
                    {
                        argName = currentValue.Substring(1);
                        argValue = String.Empty;
                    }

                    if (!Array.Exists<string>(complexAllowed,
                        delegate(string p) { return String.Equals(p, argName, 
                            StringComparison.OrdinalIgnoreCase); })
                        && 
                        !Array.Exists<string>(normalAllowed,
                        delegate(string p) { return String.Equals(p, argName,
                            StringComparison.OrdinalIgnoreCase); }))
                    {
                        throw new ArgumentParserException(String.Format(
                            "Invalid argument \"{0}\" on the command line.",
                            argName));
                    }

                    if (!this._argDict.ContainsKey(argName))
                    {
                        this._argDict.Add(argName, argValue);
                    }
                    else
                    {
                        throw new ArgumentParserException(String.Format(
                            "Duplicate argument \"{0}\" on the command line.",
                            argName));
                    }
                }
                else
                {
                    this._switchlessList.Add(currentValue);
                }
            }

            // Check number of switchless parameters
            if (minSwitchless == 0 && maxSwitchless == 0 && this.SwitchlessParams.Count > 0)
            {
                throw new ArgumentParserException(c_msgNoSwitchless);
            }
            else if (maxSwitchless > 0 && this.SwitchlessParams.Count > maxSwitchless)
            {
                throw new ArgumentParserException(c_msgTooManySwitchless);
            }
            else if (this.SwitchlessParams.Count < minSwitchless)
            {
                throw new ArgumentParserException(c_msgTooFewSwitchless);
            }
        }

        /// <summary>
        /// Gets the value of a complex parameter.
        /// </summary>
        /// <param name="paramName">Name of complex parameter.</param>
        /// <returns>Value if found else an empty string.</returns>
        public string GetValue(string paramName)
        {
            string value;
            if (this._argDict.TryGetValue(paramName, out value)) return value;
            else return String.Empty;
        }

        /// <summary>
        /// Checks if a switched parameter has been specified.
        /// </summary>
        /// <param name="paramName">Name of the parameter to look for.</param>
        public bool IsSpecified(string paramName)
        {
            return this._argDict.ContainsKey(paramName);
        }
    }

    #region Exceptions
    public class ArgumentParserException : ArgumentException
    {
        const string c_seeUsageInfo 
            = "Run application with /? to see usage information.";

        public ArgumentParserException(string message) : base(message) { }

        public override string Message
        {
            get
            {
                return base.Message + " " + c_seeUsageInfo;
            }
        }
    }

    public class ShowUsageException : ArgumentParserException
    {
        const string c_DefaultMessage
            = "No usage information has been defined for this application.";

        public ShowUsageException() : this(c_DefaultMessage) { }

        public ShowUsageException(string message) : base(message) { }
    }
    #endregion
}