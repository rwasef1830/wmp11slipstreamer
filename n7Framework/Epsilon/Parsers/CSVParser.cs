using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Epsilon.Parsers
{
    public class CSVParser : DelimValueParser
    {
        const bool c_TrimEnd = true;

        public CSVParser() 
            : this(c_DefaultBufferSize) { }
        public CSVParser(int initalBufferCapacity)
            : base(false, ',', initalBufferCapacity, c_DefaultCommentChars) { }

        public string[] Parse(string line)
        {
            return this.Parse(line, null);
        }

        public string[] Parse(string line, out int lastCommentCharPos)
        {
            return this.Parse(line, null, out lastCommentCharPos);
        }

        public string[] Parse(string line, IDictionary<string, string> varDict)
        {
            int lastCommentCharPos;
            return this.Parse(line, varDict, out lastCommentCharPos);
        }

        public string[] Parse(string line, IDictionary<string, string> varDict,
            out int lastCommentCharPos)
        {
            return this.Parse(line, varDict, out lastCommentCharPos, false);
        }

        new string[] Parse(string line, IDictionary<string, string> varDict,
            out int lastCommentCharPos, bool ignoreCommentChar)
        {
            base._output = new List<string>();
            return base.Parse(line, varDict, out lastCommentCharPos, ignoreCommentChar);
        }

        public string[] Parse(string line, int maxElements)
        {
            int lastCommentCharPos;
            return this.Parse(line, maxElements, out lastCommentCharPos);
        }

        public string[] Parse(string line, int maxElements, 
            out int lastCommentCharPos)
        {
            return this.Parse(line, maxElements, null, out lastCommentCharPos);
        }

        public string[] Parse(string line, int maxElements,
            IDictionary<string, string> varDict)
        {
            int lastCommentCharPos;
            return this.Parse(line, maxElements, varDict, out lastCommentCharPos);
        }

        public string[] Parse(string line, int maxElements, 
            IDictionary<string, string> varDict, 
            out int lastCommentCharPos)
        {
            return this.Parse(line, maxElements, varDict, out lastCommentCharPos, false);
        }

        string[] Parse(string line, int maxElements,
            IDictionary<string, string> varDict,
            out int lastCommentCharPos, bool ignoreCommentChar)
        {
            base._output = new List<string>(maxElements);
            return base.Parse(line, varDict, out lastCommentCharPos, ignoreCommentChar);
        }

        public string ParseAndRejoin(string csvLine)
        {
            int lastCommentCharPos;
            return this.Join(this.Parse(csvLine, null, out lastCommentCharPos, 
                true), c_TrimEnd);
        }

        public string Join(IEnumerable<string> components)
        {
            return this.Join(components, c_TrimEnd);
        }

        public string Join(IEnumerable<string> components, bool trimEnd)
        {
            return this.Join(components, trimEnd, 0);
        }

        public string Join(IEnumerable<string> components, bool trimEnd,
            int minElements)
        {
            return this.Join(components, trimEnd, minElements, false);
        }

        public string Join(IEnumerable<string> components, bool trimEnd, 
            int minElements, bool lastValQuotes)
        {
            if (components == null) return null;

            int currentElements = 0;
            StringBuilder builder = new StringBuilder(60);
            foreach (string str in components)
            {
                builder.Append(base.ComponentToString(str));
                builder.Append(',');
                currentElements++;
            }
            
            // If empty buffer return empty string
            if (builder.Length == 0) return String.Empty;

            if (currentElements < minElements)
            {
                // Putting an extra comma here is intentional, it will be removed later
                int extraNeeded = minElements - currentElements;
                for (int i = 0; i < extraNeeded; i++)
                {
                    builder.Append(',');
                }
                currentElements = minElements;
            }

            // Trim excess comma
            int length = builder.Length - 1;

            if (trimEnd)
            {
                while (length > 0 && currentElements > minElements
                    && builder[length - 1] == ',')
                {
                    currentElements--;
                    length--;
                }
            }
            builder.Length = length;

            if (lastValQuotes && builder.Length > 0 && builder[builder.Length - 1] == ',')
            {
                string quotes = "\"\"";
                builder.Append(quotes);
            }

            Debug.Assert(length >= 0, "Length is less than zero.");
            return builder.ToString();
        }
    }
}
