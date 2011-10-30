using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Epsilon.Parsers
{
    public class KVParser : ParserBase
    {
        CSVParser _csvParser;
        StringBuilder _buffer;
        string commentChars;

        const char csvDelim = ',';
        const char kvDelim = '=';
        const char quoteDelim = '"';
        const string defaultCommentChars = ";#";
        const int defaultBufferCapacity = 50;

        public KVParser() : this(defaultBufferCapacity, defaultCommentChars) { }

        public KVParser(string commentChars) 
            : this(defaultBufferCapacity, commentChars) { }

        public KVParser(int initialBufferCapacity) 
            : this(initialBufferCapacity, defaultCommentChars) { }

        public KVParser(int initialBufferCapacity, string commentChars)
        {
            this._csvParser = new CSVParser(initialBufferCapacity);
            this._buffer = new StringBuilder(initialBufferCapacity);
            this.commentChars = defaultCommentChars;
        }

        public bool Parse(string line, out KeyValuePair<string, string> pair)
        {
            int lastCommentChar;
            return this.Parse(line, out pair, out lastCommentChar, null);
        }

        public bool Parse(string line, out KeyValuePair<string, string> pair,
            out int commentCharPos)
        {
            return this.Parse(line, out pair, out commentCharPos, null);
        }

        public bool Parse(string line, out KeyValuePair<string, string> pair,
            IDictionary<string, string> varDict)
        {
            int commentCharPos;
            return this.Parse(line, out pair, out commentCharPos, varDict);
        }

        public bool Parse(string line, out KeyValuePair<string, string> pair,
            out int commentCharPos, IDictionary<string, string> varDict)
        {
            KeyValuePair<string, string[]> csvValuePair;
            bool result = this.Parse(line, out csvValuePair, out commentCharPos);
            string value = (csvValuePair.Value.Length > 1) ?
                this._csvParser.Join(csvValuePair.Value, true) : csvValuePair.Value[0];
            pair = new KeyValuePair<string, string>(csvValuePair.Key, value);
            return result;
        }

        public bool Parse(string line, out KeyValuePair<string, string[]> pair)
        {
            int commentCharPos;
            return this.Parse(line, out pair, out commentCharPos);
        }

        public bool Parse(string line, out KeyValuePair<string, string[]> pair,
            IDictionary<string, string> varDict)
        {
            int commentCharPos;
            return this.Parse(line, out pair, out commentCharPos, varDict);
        }

        public bool Parse(string line, out KeyValuePair<string, string[]> pair,
            out int commentCharPos)
        {
            return this.Parse(line, out pair, out commentCharPos, null);
        }

        public bool Parse(string line, out KeyValuePair<string, string[]> pair,
            out int commentCharPos, IDictionary<string, string> varDict)
        {
            StringBuilder buffer = this._buffer;
            List<string> componentList = new List<string>();
            commentCharPos = -1;

            bool isMalformed = false;
            bool isKVline = false;
            bool bufferHasWhitespaceOnly = true;
            bool bufferHasQuotedComponent = false;

            for (int i = 0; i < line.Length; i++)
            {
                char chr = line[i];
                if (chr == quoteDelim)
                {
                    if (!bufferHasWhitespaceOnly)
                    {
                        // MALFORMED: Literal quote inside a non-quoted component
                        // or start of quoted component after non-whitespace.
                        // eg: (key") <-- [3]
                        isMalformed = true;

                        // TODO: Decide whether to ignore extra quotes or add
                        // them as literal characters into the component.
                        buffer.Append(chr);
                    }
                    else
                    {
                        bufferHasWhitespaceOnly = false;
                        int j;
                        for (j = ++i; j < line.Length; j++)
                        {
                            char chr2 = line[j];
                            if (chr2 == quoteDelim)
                            {
                                // BUG FIX: Must increment j now and not later
                                // because we will break out of the loop
                                // and at the end we will try to decrement j to
                                // re-process the last char using the external
                                // loop's logic instead of the "in quotes" logic.
                                // This bug leads to " being processed twice when
                                // it is at the end of a line giving a false malformed
                                // status.
                                j++;
                                if (j == line.Length)
                                {
                                    // Closing quote at end of string
                                    bufferHasQuotedComponent = true;
                                    break;
                                }

                                chr2 = line[j];
                                if (chr2 == quoteDelim)
                                {
                                    // Quote inside quoted component
                                    buffer.Append(quoteDelim);
                                }
                                else if (chr2 == '\0')
                                {
                                    base.ThrowNullCharEncountered(line);
                                }
                                else
                                {
                                    // Closing quote
                                    bufferHasQuotedComponent = true;
                                    break;
                                }
                            }
                            else if (chr2 == '\0')
                            {
                                base.ThrowNullCharEncountered(line);
                            }
                            else
                            {
                                // Normal char inside quoted component
                                buffer.Append(chr2);
                            }
                        }

                        // Only overwrite malformed flag if it is false
                        // This is to prevent overwriting it if true with a false
                        // and prevent false negatives

                        // MALFORMED: End of line before closing quote
                        // eg: ("key) <-- Tailing " expected
                        if (!isMalformed)
                        {
                            isMalformed = !bufferHasQuotedComponent;
                        }

                        // Continue outer loop from where inner loop left off
                        // Decrement 1 as outer loop will increment i by 1 on 
                        // next loop
                        i = --j;
                    }
                }
                else if (chr == csvDelim)
                {
                    addComponentToList(buffer, componentList, 
                        ref bufferHasWhitespaceOnly, 
                        ref bufferHasQuotedComponent,
                        ref isMalformed);
                }
                else if (chr == kvDelim && !isKVline)
                {
                    isKVline = true;
                    addComponentToList(buffer, componentList,
                        ref bufferHasWhitespaceOnly, 
                        ref bufferHasQuotedComponent,
                        ref isMalformed);
                    componentList.Add(null);
                }
                else if (chr == ' ' || chr == '\t')
                {
                    if (!bufferHasWhitespaceOnly && !bufferHasQuotedComponent)
                    {
                        buffer.Append(chr);
                    }
                }
                else if (commentChars.IndexOf(chr) >= 0)
                {
                    commentCharPos = i;
                    break;
                }
                else if (chr == '\0')
                {
                    base.ThrowNullCharEncountered(line);
                }
                else
                {
                    bufferHasWhitespaceOnly = false;

                    // MALFORMED: Data after a closed quoted component
                    // eg: ("key"x) <-- Unexpected tailing 'x'
                    if (bufferHasQuotedComponent)
                    {
                        isMalformed = true;
                    }

                    // I don't know if I should ignore the char, or terminate...
                    // so I just add it as a literal part of the component.
                    buffer.Append(chr);
                }
            }

            // If there is still something in the buffer, add it to the list
            addComponentToList(buffer, componentList, ref bufferHasWhitespaceOnly,
                ref bufferHasQuotedComponent, ref isMalformed);

            // Substitute variables
            if (varDict != null)
            {
                for (int i = 0; i < componentList.Count; i++)
                {
                    componentList[i] = this.SubstituteVariables(varDict, 
                        componentList[i]);
                }
            }

            // Now to make sense of the components list
            int indexOfKVdelim = componentList.IndexOf(null);
            int lengthOfKeyComponents = 0;

            if (indexOfKVdelim < 0)
            {
                lengthOfKeyComponents = componentList.Count;
            }
            else
            {
                lengthOfKeyComponents = indexOfKVdelim;

                // MALFORMED: Empty keyname
                // (eg: = value) <-- Expected char at [1]
                if (componentList[0].Length == 0)
                {
                    isMalformed = true;
                }
                else if (lengthOfKeyComponents > 1)
                {
                    // MALFORMED: Key has more than one component and
                    // one of them must be quoted (due to quote or whitespace)
                    // eg: (key,"key 2" = value) <-- No component of key can
                    // be quoted if the entire line is a KVPair and not a CSVLine.
                    for (int i = 0; i < lengthOfKeyComponents; i++)
                    {
                        if (this._csvParser.NeedsQuotes(componentList[i]))
                        {
                            isMalformed = true;
                            break;
                        }
                    }
                }
            }

            string key = null;
            string[] keyComponents = new string[lengthOfKeyComponents];

            string[] valueComponents = null;
            if (indexOfKVdelim > 0)
                valueComponents = new string[componentList.Count - 1 - indexOfKVdelim];

            componentList.CopyTo(0, keyComponents, 0, keyComponents.Length);
            if (keyComponents.Length > 1)
            {
                key = this._csvParser.Join(keyComponents, false);
            }
            else
            {
                key = keyComponents[0];
            }

            if (valueComponents != null)
            {
                componentList.CopyTo(indexOfKVdelim + 1, valueComponents, 0,
                    valueComponents.Length);
            }

            Debug.Assert(buffer.Length == 0, "Internal buffer is not empty.");

            pair = new KeyValuePair<string, string[]>(key, valueComponents);
            return isMalformed;
        }

        public CSVParser GetInternalCSVParser()
        {
            return this._csvParser;
        }

        static void addComponentToList(StringBuilder buffer, 
            List<string> componentList, ref bool bufferHasWhitespaceOnly, 
            ref bool bufferHasQuotedComponent, ref bool isMalformed)
        {
            string component = buffer.ToString();
            if (!bufferHasQuotedComponent)
            {
                component = component.Trim();

                // (DISABLED) MALFORMED: Whitespace in a non-quoted component
                if (component.IndexOfAny(new char[] { ' ', '\t' }) >= 0)
                {
                    // TODO: Implement strictness "levels" or "exceptions"

                    // Some official ms infs have this problem and are 
                    // not considered malformed by windows inf parser,
                    // so this rule is disabled until strictness levels are 
                    // coded and enabled.

                    //isMalformed = true;
                }
            }
            componentList.Add(component);
            buffer.Length = 0;
            bufferHasWhitespaceOnly = true;
            bufferHasQuotedComponent = false;
        }
    }
}
