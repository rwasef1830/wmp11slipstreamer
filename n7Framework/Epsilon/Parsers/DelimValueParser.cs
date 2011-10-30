using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Epsilon.n7Framework.Epsilon.Parsers;

namespace Epsilon.Parsers
{
    public abstract class DelimValueParser : ParserBase
    {
        #region Private fields
        StringBuilder _buffer;
        StringBuilder _compStrConvBuffer;
        char _delimiter;
        bool _mergeExtra;
        char[] _commentChars;
        char[] _dangerousChars;
        #endregion

        #region Protected members
        /// <summary>
        /// Must be initialized by inheriting class, initial capacity 
        /// is maximum number of allowed components, 0 for no limit.
        /// </summary>
        protected List<string> _output;

        /// <summary>
        /// Must be reset by the inheriting class
        /// </summary>
        protected int _posLastDelimiter;
        #endregion

        #region Public constant
        public const int c_DefaultBufferSize = 50;
        public const string c_DefaultCommentChars = ";";
        #endregion

        #region Constructors
        protected DelimValueParser(bool mergeExtra,
            char delimiter) : this(mergeExtra, 
            delimiter, c_DefaultBufferSize, c_DefaultCommentChars) { }

        protected DelimValueParser(bool mergeExtra,
            char delimiter, int initialBufferCapacity, 
            string commentChars)
        {
            this._buffer = new StringBuilder(initialBufferCapacity);
            this._compStrConvBuffer = new StringBuilder(50);
            this._delimiter = delimiter;
            this._mergeExtra = mergeExtra;
            this._commentChars = commentChars.ToCharArray();
            this._dangerousChars = new char[] { ' ', '"', '\t',
                this._delimiter, '\\', '>', '<', '=', '[', ']', 
                (char)20445, (char)21152 };
        }
        #endregion

        #region Parser core
        /// <summary>
        /// Parses a line of delimited values.
        /// Attempts to ignore malformed values without throwing errors.
        /// </summary>
        /// <param name="line">Line containing the values to parse</param>
        /// <param name="varDict">Dictionary containing values 
        /// to replace, can be null.</param>
        /// <param name="ignoreAfterMaxElems">true to ignore values more than
        /// maximum delimited elements limit, false to throw 
        /// <see cref="IndexOutOfRangeException" />.</param>
        /// <param name="lastCommentCharPos">Last position of a comment character, 
        /// get substring from this value to end of string to get comment text.</param>
        /// <param name="ignoreCommentChar">Ignores comment characters
        /// and treats them as normal literal characters.</param>
        /// <returns>String array of values, quotes stripped.</returns>
        protected string[] Parse(string line, IDictionary<string, string> varDict, 
            out int lastCommentCharPos, bool ignoreCommentChar)
        {
            return this.Parse(line, varDict, false, out lastCommentCharPos, 
                ignoreCommentChar);
        }

        /// <summary>
        /// Parses a line of delimited values.
        /// Attempts to ignore malformed values without throwing errors.
        /// </summary>
        /// <param name="line">Line containing the values to parse</param>
        /// <param name="varDict">Dictionary containing values 
        /// to replace, can be null.</param>
        /// <param name="ignoreAfterMaxElems">true to ignore values after the max
        /// components, false to throw <see cref="IndexOutOfRangeException" />.</param>
        /// <param name="lastCommentCharPos">Last position of a comment character, 
        /// get substring from this value to end of string to get comment text.</param>
        /// <param name="ignoreCommentChar">Ignores comment characters
        /// and treats them as normal literal characters.</param>
        /// <returns>String array of values, quotes stripped.</returns>
        string[] Parse(string line, 
            IDictionary<string, string> varDict, bool ignoreAfterMaxElems, 
            out int lastCommentCharPos, bool ignoreCommentChar)
        {
            int initialCapacity = this._output.Capacity;
            while (this._output.Count < this._output.Capacity) this._output.Add(null);
            int elementNum = 0;
            bool mergeAllSubsequentValues = false;

            lastCommentCharPos = -1;

            for (int i = 0; i < line.Length; i++)
            {
                char chr = line[i];
                if (chr == '"')
                {
                    int j = i + 1;
                    bool foundMatchingQuote = false;
                    while (j < line.Length)
                    {
                        char chr2 = line[j];
                        if (chr2 == '"')
                        {
                            if ((j + 1) >= line.Length)
                            {
                                // End of quoted string and end of line
                                this.FlushBufferToList(ref elementNum,
                                    ref mergeAllSubsequentValues,
                                    ref i, initialCapacity);
                                foundMatchingQuote = true;
                                this._posLastDelimiter = -1;
                                // In order to cause outer loop to break
                                // on next cycle
                                i = j;
                                break;
                            }
                            else
                            {
                                char chr3 = line[++j];
                                if (chr3 == '"')
                                {
                                    // Quote in string value
                                    this._buffer.Append(chr3);
                                }
                                else if (chr3 == '\0')
                                {
                                    base.ThrowNullCharEncountered(line);
                                }
                                else
                                {
                                    // End of string value
                                    this.FlushBufferToList(ref elementNum,
                                        ref mergeAllSubsequentValues,
                                        ref i, initialCapacity);
                                    foundMatchingQuote = true;

                                    // Skip to next delimiter, ignore invalid 
                                    // data after string
                                    i = line.IndexOf(this._delimiter, j);

                                    // In case there are no more delimiters
                                    if (i < 0)
                                    {
                                        i = line.Length - 1;
                                    }

                                    // Set position of last delimiter
                                    this._posLastDelimiter = i;

                                    // Cause outer loop to break since we found the
                                    // last element in the allowed and we're going to
                                    // ignore all the rest.
                                    if (initialCapacity > 0
                                        && elementNum == this._output.Count
                                        && ignoreAfterMaxElems)
                                    {
                                        i = line.Length - 1;
                                    }
                                    break;
                                }
                            }
                        }
                        else if (chr2 == '\0')
                        {
                            base.ThrowNullCharEncountered(line);
                        }
                        else
                        {
                            // Normal character in string
                            this._buffer.Append(chr2);
                        }
                        j++;
                    }
                    if (!foundMatchingQuote)
                    {
                        this._posLastDelimiter = line.Length - 1;
                        break;
                    }
                }
                else if (chr == this._delimiter && !mergeAllSubsequentValues)
                {
                    this.FlushBufferToList(ref elementNum,
                        ref mergeAllSubsequentValues, ref i, 
                        initialCapacity);
                    this._posLastDelimiter = i;
                    /* This will break the loop immediately
                     * when we just added the last element
                     * if we choose to ignore elements after
                     * the limit, otherwise will throw exception
                     * next time a component tries to be added.
                     */
                    if (initialCapacity > 0 
                        && elementNum == this._output.Count
                        && ignoreAfterMaxElems)
                    {
                        break;
                    }
                }
                else if (!ignoreCommentChar && 
                    Array.IndexOf<char>(this._commentChars, chr) >= 0)
                {
                    lastCommentCharPos = i;
                    break;
                }
                else if (chr == '\0')
                {
                    base.ThrowNullCharEncountered(line);
                }
                else
                {
                    this._buffer.Append(chr);
                }

                // Don't add any code here
            }

            // Flush item to the list if buffer is not empty
            if (this._buffer.Length > 0)
            {
                int i = -1;
                this.FlushBufferToList(ref elementNum,
                    ref mergeAllSubsequentValues, ref i,
                    initialCapacity);
            }
            
            // Trim all values - ugly but no other way to do it directly
            // with StringBuilder without copying the string for each operation
            for (int i = 0; i < this._output.Count; i++)
            {
                if (!String.IsNullOrEmpty(this._output[i]))
                {
                    this._output[i] = this._output[i].Trim();
                }
            }

            // Substitute variables
            if (varDict != null)
            {
                for (int i = 0; i < this._output.Count; i++)
                {
                    this._output[i] = this.SubstituteVariables(varDict, this._output[i]);
                }
            }

            Debug.Assert(this._buffer.Length == 0, 
                "Value buffer is not empty but returning from parse.",
                String.Format("Buffer contains: [{0}]", this._buffer.ToString()));

            string[] outputArray = (this._output.Count > 0) ? 
                this._output.ToArray() : null;
            this._output = null;
            return outputArray;
        }
        #endregion

        public virtual string ComponentToString(string component)
        {
            if (String.IsNullOrEmpty(component)) return String.Empty;
            this._compStrConvBuffer.Append(component);
            this._compStrConvBuffer.Replace("\"", "\"\"");
            if (this.NeedsQuotes(component))
            {
                this._compStrConvBuffer.Insert(0, "\"");
                this._compStrConvBuffer.Append("\"");
            }
            string result = this._compStrConvBuffer.ToString();
            this._compStrConvBuffer.Length = 0;
            return result;
        }

        public virtual bool NeedsQuotes(string component)
        {
            return component.IndexOfAny(this._dangerousChars) >= 0
                || component.IndexOfAny(this._commentChars) >= 0;
        }

        void FlushBufferToList(ref int elementNum, 
            ref bool mergeAllSubsequentValues, ref int pos,
            int initalOutputCapacity)
        {
            if (this._output.Count > 0 && this._output.Count == initalOutputCapacity)
            {
                // this._output will have been null-filled by the Parse function
                if (elementNum >= this._output.Count)
                {
                    if (this._mergeExtra)
                    {
                        int lastIndex = this._output.Count - 1;
                        this._buffer.Insert(0, this._delimiter);
                        this._buffer.Insert(0, this._output[lastIndex]);
                        this._output[lastIndex] = null;
                        elementNum = lastIndex;
                        if (pos > 0)
                        {
                            pos--;
                            mergeAllSubsequentValues = true;
                            return;
                        }
                    }
                    else
                    {
                        throw new Exceptions.DelimitedValuesExceededException();
                    }
                }
                else
                {
                    this._output[elementNum] = this._buffer.ToString();
                }
            }
            else
            {
                this._output.Add(this._buffer.ToString());
            }
            this._buffer.Length = 0;
            elementNum++;
        }

        public static class Exceptions
        {
            public class DelimValueParserException : Exception
            {
                static string s_DefaultMessage = ParsersMsg.errDelimValueParserError;

                public DelimValueParserException() : base(s_DefaultMessage) { }
                public DelimValueParserException(string message) : base(message) { }
            }

            public class DelimitedValuesExceededException : DelimValueParserException
            {
                static string s_DefaultMessage = ParsersMsg.errDelimitedValuesExceeded;

                public DelimitedValuesExceededException() : base(s_DefaultMessage) { }
                public DelimitedValuesExceededException(string message) : base(message) { }
            }
        }
    }
}
