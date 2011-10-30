// SimpleIniEditor V5 - A new generation
// Renamed project to IniParser
// By: Raif Atef Wasef
// Version: 5.38
// Date: 17/07/2008

using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using Epsilon.Collections.Generic;
using Epsilon.Parsers;
using System.Collections.ObjectModel;

namespace Epsilon.Parsers
{
    public class IniParser : ParserBase
    {
        #region Private members
        OrderedDictionary<string, OrderedDictionary<string, List<string>>> _dataStore;
        Dictionary<string, Dictionary<string, List<string>>> _dataInlineComments;
        Dictionary<string, List<CommentInfo>> _dataNormalComments;
        Dictionary<string, Dictionary<string, List<int>>> _dataRepeatKeys;
        Dictionary<string, SectionOverrides> _sectOverrides;

        KVParser _kvParser;
        CSVParser _csvParser;
        FileInfo _fileInfo;
        Encoding _fileEncoding;

        const string c_CommentChars = ";#";
        const bool c_TrimEnd = true;
        const KeyExistsPolicy c_AddPolicy = KeyExistsPolicy.Ignore;
        const bool c_ThrowIfCsvLineExists = false;
        #endregion

        #region Public members
        /// <summary>
        /// Gets the names of all sections currently defined in a read-only collection
        /// </summary>
        public ICollection<string> Sections
        {
            get { return (ICollection<string>)(this._dataStore.Keys); }
        }

        public FileInfo IniFileInfo
        {
            get { return this._fileInfo; }
        }

        public bool EnableLastValueQuotes;
        #endregion

        #region Constructors
        public IniParser(string filePath, bool throwForMalformedLines)
        {
            this._fileInfo = new FileInfo(filePath);
            StreamReader reader = new StreamReader(filePath, Encoding.Default, true);
            try
            {
                this.Init(reader, throwForMalformedLines);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                this._fileEncoding = reader.CurrentEncoding;
                reader.Close();
            }
        }

        public IniParser(TextReader reader, bool throwForMalformedLines,
            string fileNameToSave)
        {
            this._fileInfo = new FileInfo(fileNameToSave);
            try
            {
                this.Init(reader, throwForMalformedLines);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                reader.Close();
            }
        }

        public IniParser(string contents, bool throwForMalformedLines,
            string filenameToSave) : this(new StringReader(contents), throwForMalformedLines, 
            filenameToSave) { }
        #endregion

        #region Main parser initialiser
        void Init(TextReader iniStream, 
            bool throwFormatExceptionForMalformedLines)
        {
            this._kvParser = new KVParser(200, c_CommentChars);
            this._csvParser = this._kvParser.GetInternalCSVParser();
            this._dataStore
                = new OrderedDictionary<string, OrderedDictionary<string, List<string>>>(
                StringComparer.OrdinalIgnoreCase);
            this._dataNormalComments
                = new Dictionary<string, List<CommentInfo>>(
                StringComparer.OrdinalIgnoreCase);
            this._dataInlineComments
                = new Dictionary<string, Dictionary<string, List<string>>>(
                StringComparer.OrdinalIgnoreCase);
            this._dataRepeatKeys
                = new Dictionary<string, Dictionary<string, List<int>>>(
                StringComparer.OrdinalIgnoreCase);
            this._sectOverrides
                = new Dictionary<string, SectionOverrides>(
                StringComparer.OrdinalIgnoreCase);
            this.ParseIniCore(iniStream, throwFormatExceptionForMalformedLines,
                null, true);
        }
        #endregion

        #region Public methods
        public void SetSectionOutputOverrides(string section, 
            SectionOverrides overrides)
        {
            if (!this._dataStore.ContainsKey(section))
            {
                throw new Exceptions.SectionNotFoundException(section);
            }

            if (!this._sectOverrides.ContainsKey(section))
            {
                this._sectOverrides.Add(section, overrides);
            }
            else
            {
                this._sectOverrides[section] = overrides;
            }
        }

        public bool LineExists(string section, string line)
        {
            string[] components = this._csvParser.Parse(line);
            return this.CsvLineExists(section, components);
        }

        public bool CsvLineExists(string section, params string[] lineComponents)
        {
            return this.KeyExists(section, this.JoinMultiValue(lineComponents, true));
        }

        public bool KeyExists(string section, string key)
        {
            OrderedDictionary<string, List<string>> sectionDictionary;
            if (this.TryGetRef(section, out sectionDictionary))
            {
                return sectionDictionary.ContainsKey(key);
            }
            else
            {
                return false;
            }
        }

        public bool SectionExists(string section)
        {
            return this._dataStore.ContainsKey(section);
        }

        public bool TryGetRef(string section, 
            out OrderedDictionary<string, List<string>> sectionData)
        {
            return this._dataStore.TryGetValue(section, out sectionData);
        }

        public bool TryGetRef(string section, string key, 
            out List<string> values)
        {
            OrderedDictionary<string, List<string>> sectionData;
            if (this.TryGetRef(section, out sectionData))
            {
                return sectionData.TryGetValue(key, out values);
            }
            else
            {
                values = null;
                return false;
            }
        }

        public bool TryReadSection(string section, 
            out OrderedDictionary<string, List<string>> sectDataCopy)
        {
            OrderedDictionary<string, List<string>> sectData;
            if (this.TryGetRef(section, out sectData))
            {
                sectDataCopy = new OrderedDictionary<string, List<string>>(
                    sectData.Count, sectData.KeyComparer, sectData.ValueComparer);
                foreach (KeyValuePair<string, List<string>> sectEntry in sectData)
                {
                    List<string> valuesCopy = (sectEntry.Value != null) ?
                        new List<string>(sectEntry.Value) : null;
                    sectDataCopy.Add(sectEntry.Key, valuesCopy);
                }
                return true;
            }
            else
            {
                sectDataCopy = null;
                return false;
            }
        }

        public bool TryReadAllValues(string section, string key,
            out List<string> values)
        {
            return this.TryReadAllValues(section, key, null, out values);
        }

        public bool TryReadAllValues(string section, string key,
            IDictionary<string, string> varDict, out List<string> values)
        {
            List<string> valuesRef;
            if (this.TryGetRef(section, key, out valuesRef))
            {
                values = new List<string>(valuesRef.Count);
                if (varDict != null)
                {
                    foreach (string value in valuesRef)
                    {
                        values.Add(this._csvParser.SubstituteVariables(varDict, value));
                    }
                }
                else
                {
                    values.AddRange(valuesRef);
                }
                return true;
            }
            else
            {
                values = null;
                return false;
            }
        }

        public bool TryReadValue(string section, string key, out string joinedValue)
        {
            return this.TryReadValue(section, key, c_TrimEnd, out joinedValue);
        }

        public bool TryReadValue(string section, string key, bool trimEnd,
            out string joinedValue)
        {
            return this.TryReadValue(section, key, trimEnd, null, out joinedValue);
        }

        public bool TryReadValue(string section, string key, bool trimEnd, 
            IDictionary<string, string> varDict, out string joinedValue)
        {
            List<string> values;
            if (this.TryReadAllValues(section, key, varDict, out values))
            {
                joinedValue = this.JoinMultiValue(values);
                return true;
            }
            else
            {
                joinedValue = null;
                return false;
            }
        }

        public bool TryChangeAllValues(string section, string key, 
            IEnumerable<string> values)
        {
            OrderedDictionary<string, List<string>> sectionData;
            if (this.TryGetRef(section, out sectionData))
            {
                return sectionData.TryChangeValue(key, new List<string>(values));
            }
            else
            {
                return false;
            }
        }

        public bool TryChangeValue(string section, string key, string joinedValues)
        {
            string[] values = this._csvParser.Parse(joinedValues);
            return this.TryChangeAllValues(section, key, values);
        }

        public bool TryChangeKey(string section, string key, string newKey)
        {
            List<string> originalValues;
            if (this.TryGetRef(section, key, out originalValues))
            {
                return this.TryChangeKey(section, key, newKey, originalValues);
            }
            else return false;
        }

        public bool TryChangeKey(string section, string key,
            string newKey, string newJoinedValue)
        {
            string[] newValues = this._csvParser.Parse(newJoinedValue);
            return this.TryChangeKey(section, key, newKey, newValues);
        }

        public bool TryChangeKey(string section, string key, 
            string newKey, IEnumerable<string> newValues)
        {
            OrderedDictionary<string, List<string>> sectionData;
            if (this.TryGetRef(section, out sectionData))
            {
                return sectionData.TryChangeKey(key, newKey, 
                    new List<string>(newValues));
            }
            else return false;
        }

        public OrderedDictionary<string, List<string>> ReadSection(string section)
        {
            OrderedDictionary<string, List<string>> sectDataCopy;
            if (this.TryReadSection(section, out sectDataCopy))
            {
                return sectDataCopy;
            }
            else 
            {
                this.ThrowException(new Exceptions.SectionNotFoundException(section));
                return null;
            }
        }

        public OrderedDictionary<string, List<string>> ReadSection(string section,
            IDictionary<string, string> varDict)
        {
            // Substituting variables implicitly makes a copy of the section data
            OrderedDictionary<string, List<string>> sectDict = this.GetRef(section);
            return this.SubstituteVariables(sectDict, varDict);
        }

        public ICollection<KeyValuePair<string, List<string>>> ReadSection(
            string section, IDictionary<string, string> varDict, bool noMergeDupKeys)
        {
            Dictionary<string, List<int>> repeatKeysDict;
            if (noMergeDupKeys
                && this._dataRepeatKeys.TryGetValue(section, out repeatKeysDict))
            {
                OrderedDictionary<string, List<string>> sectDict = this.GetRef(section);

                if (varDict != null)
                    sectDict = this.SubstituteVariables(sectDict, varDict);

                List<KeyValuePair<string, List<string>>> sectDataNoMerge
                    = new List<KeyValuePair<string, List<string>>>(sectDict.Count);

                foreach (KeyValuePair<string, List<string>> kvPair in sectDict)
                {
                    List<int> repeatValueIndices;
                    if (repeatKeysDict.TryGetValue(kvPair.Key, out repeatValueIndices))
                    {
                        List<string[]> splitValues = SplitList<string>(
                            kvPair.Value, repeatValueIndices);

                        foreach (string[] values in splitValues)
                        {
                            sectDataNoMerge.Add(new KeyValuePair<string, List<string>>(
                                kvPair.Key, new List<string>(values)));
                        }
                    }
                    else
                    {
                        sectDataNoMerge.Add(kvPair);
                    }
                }

                return sectDataNoMerge;
            }
            else
            {
                return this.ReadSection(section, varDict);
            }
        }

        public List<string> ReadAllValues(string section, string key)
        {
            return this.ReadAllValues(section, key, null);
        }

        public List<string> ReadAllValues(string section, string key, 
            IDictionary<string, string> varDict)
        {
            List<string> values;
            if (this.TryReadAllValues(section, key, varDict, out values)) return values;
            else if (!this.SectionExists(section))
            {
                this.ThrowException(new Exceptions.SectionNotFoundException(section));
                return null;
            }
            else
            {
                this.ThrowException(new Exceptions.KeyNotFoundException(section, key));
                return null;
            }
        }

        public string ReadValue(string section, string key)
        {
            return this.ReadValue(section, key, c_TrimEnd);
        }

        public string ReadValue(string section, string key, bool trimEnd)
        {
            return this.ReadValue(section, key, trimEnd, null);
        }

        public string ReadValue(string section, string key, bool trimEnd,
            IDictionary<string, string> varDict)
        {
            string values;
            // This is better than just wrapping ReadAllValues(string,string) to avoid
            // making an unnecessary ref array copy before joining by CSVParser
            if (this.TryReadValue(section, key, trimEnd, null, out values))
            {
                return values;
            }
            else if (!this.SectionExists(section))
            {
                this.ThrowException(new Exceptions.SectionNotFoundException(section));
                return null;
            }
            else
            {
                this.ThrowException(new Exceptions.KeyNotFoundException(section, key));
                return null;
            }
        }

        public void ChangeAllValues(string section, string key, 
            IEnumerable<string> values)
        {
            if (!this.TryChangeAllValues(section, key, values))
            {
                if (!this.SectionExists(section))
                    this.ThrowException(new Exceptions.SectionNotFoundException(section));
                else
                    this.ThrowException(new Exceptions.KeyNotFoundException(section, key));
            }
        }

        public void ChangeValue(string section, string key, string joinedValues)
        {
            string[] newValues = this._csvParser.Parse(joinedValues);
            this.ChangeAllValues(section, key, newValues);
        }

        public void Add(string section)
        {
            OrderedDictionary<string, List<string>> sectionContents
                = new OrderedDictionary<string, List<string>>(
                StringComparer.OrdinalIgnoreCase);
            Dictionary<string, List<string>> sectionInlineComments
                = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            List<CommentInfo> sectionNormalComments
                = new List<CommentInfo>(5);
            Dictionary<string, List<int>> sectionRepeatKey
                = new Dictionary<string, List<int>>(
                StringComparer.OrdinalIgnoreCase);

            // No "already exists" check because this will be
            // automatically done by the underlying IDictionary(s)
            this._dataStore.Add(section, sectionContents);
            this._dataNormalComments.Add(section, sectionNormalComments);
            this._dataInlineComments.Add(section, sectionInlineComments);

            if (sectionRepeatKey.Count > 0)
            {
                this._dataRepeatKeys.Add(section, sectionRepeatKey);
            }
        }

        public void Add(string section, string line)
        {
            this.Add(section, line, c_ThrowIfCsvLineExists);
        }

        public void Add(string section, string line, bool throwIfAlreadyExists)
        {
            string[] fakeValueListReference = null;
            this.Add(section, line, fakeValueListReference, 
                (throwIfAlreadyExists) ?
                    KeyExistsPolicy.ThrowException : KeyExistsPolicy.Ignore);
        }

        public void Add(string section, string key, string value)
        {
            this.Add(section, key, value, c_AddPolicy);
        }

        public void Add(string section, string key, string value, KeyExistsPolicy policy)
        {
            this.Add(section, key, this._csvParser.Parse(value), policy);
        }

        public void Add(string section, IEnumerable<IEnumerable<string>> csvLines)
        {
            this.Add(section, csvLines, c_ThrowIfCsvLineExists);
        }

        public void Add(string section, IEnumerable<IEnumerable<string>> csvLines,
            bool throwIfAlreadyExists)
        {
            foreach (IEnumerable<string> csv in csvLines)
            {
                this.Add(section, this.JoinMultiValue(csv), throwIfAlreadyExists);
            }
        }

        public void Add(string section, IEnumerable<string> csvLines,
            bool parseAndRejoin)
        {
            this.Add(section, csvLines, parseAndRejoin, c_ThrowIfCsvLineExists);
        }

        public void Add(string section, IEnumerable<string> csvLines, 
            bool parseAndRejoin, bool throwIfAlreadyExists)
        {
            foreach (string csv in csvLines)
            {
                this.Add(section, (parseAndRejoin) ?
                    this._csvParser.ParseAndRejoin(csv) : csv, 
                    throwIfAlreadyExists);
            }
        }

        public void Add(string section, IDictionary<string, List<string>> contents)
        {
            this.Add(section, contents, c_AddPolicy);
        }

        public void Add(string section, IDictionary<string, List<string>> contents,
            KeyExistsPolicy addPolicy)
        {
            foreach (KeyValuePair<string, List<string>> entry in contents)
            {
                this.Add(section, entry.Key, entry.Value, addPolicy);
            }
        }

        public void Add(string section, IDictionary<string, string> contents)
        {
            this.Add(section, contents, c_AddPolicy);
        }

        public void Add(string section, IDictionary<string, string> contents,
            KeyExistsPolicy addPolicy)
        {
            foreach (KeyValuePair<string, string> entry in contents)
            {
                this.Add(section, entry.Key, this._csvParser.Parse(entry.Value),
                    addPolicy);
            }
        }

        public void Add(string section, string key, IEnumerable<string> values)
        {
            this.Add(section, key, values, c_AddPolicy);
        }

        public void Add(string section, string key, IEnumerable<string> values,
            KeyExistsPolicy addPolicy)
        {
            OrderedDictionary<string, List<string>> sectionDictionary = this.GetRef(section);

            // If null value then key is actually a CSV line
            // TODO: DelimValueParser does not give any indication that key is malformed!
            if (values == null)
            {
                key = this._csvParser.ParseAndRejoin(key);

                // Some protection from adding String.Empty to the dictionary.
                // This can only happen if CSVParser tries to parse this: ""someText
                // It will terminate processing after the second quote giving an empty string.
                if (String.IsNullOrEmpty(key))
                {
                    this.ThrowException(
                        new Exceptions.MalformedInputException(
                        "Malformed quotes at beginning of line being added.",
                        section, key, -1));
                }
            }

            // Try to see if we are going to need to merge or not
            List<string> existingValues;
            if (!sectionDictionary.TryGetValue(key, out existingValues))
            {
                // Case 1: Key was never in the section before
                sectionDictionary.Add(key,
                    (values == null) ? null : new List<string>(values));
            }
            else if (addPolicy == KeyExistsPolicy.Ignore)
            {
                // Case 2: Key already exists and we are ignoring
                return;
            }
            else if (addPolicy == KeyExistsPolicy.Append 
                || addPolicy == KeyExistsPolicy.Merge)
            {
                // Case 3: We need to append or merge some values
                if (values == null) return;
                else if (existingValues == null)
                {
                    // Case 3-1: Original had none - Forbidden, throw exception.
                    this.ThrowCSVKVPairConversion();
                }
                else
                {
                    // Case 3-3: Original had some and new values present
                    // then append or merge the new values to the old list
                    if (addPolicy == KeyExistsPolicy.Append)
                    {
                        existingValues.AddRange(values);
                    }
                    else
                    {
                        // Remove all null and empty values values
                        existingValues.RemoveAll(String.IsNullOrEmpty);

                        // Search for dups in the original array, use a dictionary
                        Dictionary<string, object> uniqueKeysDict
                            = new Dictionary<string, object>(
                            StringComparer.OrdinalIgnoreCase);
                        foreach (string item in existingValues)
                        {
                            if (!uniqueKeysDict.ContainsKey(item))
                                uniqueKeysDict.Add(item, null);
                        }
                        foreach (string item in values)
                        {
                            if (!uniqueKeysDict.ContainsKey(item))
                                uniqueKeysDict.Add(item, null);
                        }
                        existingValues.Clear();
                        existingValues.AddRange(uniqueKeysDict.Keys);
                    }
                }
            }
            else if (addPolicy == KeyExistsPolicy.Discard)
            {
                // Case 4: Discard old values and force the new values
                if (values == null)
                {
                    sectionDictionary.TryChangeKey(key,
                        this._csvParser.ComponentToString(key), null);
                }
                else
                {
                    if (existingValues == null)
                    {
                        this.ThrowCSVKVPairConversion();
                    }
                    else
                    {
                        sectionDictionary[key] = new List<string>(values);
                    }
                }
            }
            else if (addPolicy == KeyExistsPolicy.CreateNewList)
            {
                // Case 5: Create a key repetition with its own CSV list
                if (values == null) return;
                else if (existingValues == null)
                {
                    this.ThrowCSVKVPairConversion();
                }
                else 
                {
                    Dictionary<string, List<int>> sectRepeatKeys;
                    bool gotRepeatedKeyInfo = this._dataRepeatKeys.TryGetValue(
                        section, out sectRepeatKeys);

                    // Lazy addition to repeated keys table
                    if (!gotRepeatedKeyInfo)
                    {
                        sectRepeatKeys = new Dictionary<string, List<int>>();
                        this._dataRepeatKeys.Add(section, sectRepeatKeys);
                    }

                    List<int> keyRepeatIndices;
                    if (!sectRepeatKeys.TryGetValue(key, out keyRepeatIndices))
                    {
                        keyRepeatIndices = new List<int>();
                        sectRepeatKeys.Add(key, keyRepeatIndices);
                    }
                    keyRepeatIndices.Add(existingValues.Count);

                    existingValues.AddRange(values);
                }
            }
            else if (addPolicy == KeyExistsPolicy.ThrowException)
            {
                this.ThrowException(new Exceptions.KeyAlreadyExistsException(section, key));
            }
        }

        public bool Remove(string section)
        {
            bool sectionRemoved = this._dataStore.Remove(section);
            bool normalCommentsRemoved = this._dataNormalComments.Remove(section);
            bool inlineCommentsRemoved = this._dataInlineComments.Remove(section);
            bool repeatingKeysRemoved = this._dataRepeatKeys.Remove(section);
            bool result = sectionRemoved && normalCommentsRemoved 
                && inlineCommentsRemoved && repeatingKeysRemoved;
            Debug.Assert(result, "Section dictionaries are not in sync.");
            return result;
        }

        public bool Remove(string section, string key)
        {
            return this.Remove(section, key, null);
        }

        public bool RemoveLine(string section, string line)
        {
            line = this._csvParser.ParseAndRejoin(line);
            return this.Remove(section, line);
        }

        public bool Remove(string section, string key, string value)
        {
            // This will throw exception if section is not found
            OrderedDictionary<string, List<string>> sectionDictionary 
                = this.GetRef(section);

            List<string> values;
            if (sectionDictionary.TryGetValue(key, out values))
            {
                if (value == null)
                {
                    return this.RemoveKeyReorderComments(section, sectionDictionary, key);
                }

                // Key exists so I need to search through
                // the value list, and this can get slow on large lists > 10
                int numValuesRemoved = values.RemoveAll(delegate(string valueItem)
                {
                    return String.Equals(value, valueItem, 
                        StringComparison.OrdinalIgnoreCase); 
                });

                // If element removed was the only in the list, then
                // we will remove the key as well.
                if (numValuesRemoved > 0)
                {
                    if (values.Count == 0)
                    {
                        // Should always be true
                        return this.RemoveKeyReorderComments(section, sectionDictionary, key);
                    }
                    else
                    {
                        // Removal success, values still remain 
                        // even after removing

                        // TODO: Get the repeat keys data and go through it backwards
                        // and make sure each entry is still valid (ie: entry < currentList.Count)
                        // and remove they keys that aren't. Then go to OutputSection and turn
                        // off ignoring out of bounds repeating info data and make it 
                        // assert instead to catch problems related to this.
                        return true;
                    }
                }
                else
                {
                    // Value not found so
                    return false;
                }
            }
            else
            {
                // Key doesn't exist so
                return false;
            }
        }

        public void ReplaceSection(string section, 
            OrderedDictionary<string, List<string>> newData)
        {
            if (!this._dataStore.TryChangeValue(section, newData))
            {
                this.ThrowException(new Exceptions.SectionNotFoundException(section));
            }
        }

        public void ParseAndAdd(string section, string rawData,
            bool throwForMalformedLines)
        {
            StringReader reader = new StringReader(rawData);
            if (!this._dataStore.ContainsKey(section)) this.Add(section);
            this.ParseIniCore(reader, throwForMalformedLines, section, false);
        }

        public void AddPresectionsComment(string commentText)
        {
            List<CommentInfo> sectionComments = this._dataNormalComments[String.Empty];
            int lineNumber = Math.Max(0, this._dataStore[String.Empty].Count);
            CommentInfo newComment = new CommentInfo(lineNumber, commentText);
            sectionComments.Add(newComment);
        }

        /// <summary>
        /// Gets a reference to the data structures for an entire section.
        /// Anything changed here is reflected immediately in the output.
        /// </summary>
        /// <param name="section">Name of the section</param>
        public OrderedDictionary<string, List<string>> GetRef(string section)
        {
            OrderedDictionary<string, List<string>> sectDataRef;
            if (this.TryGetRef(section, out sectDataRef))
            {
                return sectDataRef;
            }
            else
            {
                this.ThrowException(new Exceptions.SectionNotFoundException(section));
                return null;
            }
        }

        public void OutputIni(Action<string> lineReceiverMethod, bool stripComments)
        {
            if (lineReceiverMethod == null) 
                throw new ArgumentNullException("lineReceiverMethod");

            // This builder is to reduce overhead of repeated String.Format
            StringBuilder formatBuilder = new StringBuilder(200);

            for (int i = 0; i < this._dataStore.Count; i++)
            {
                KeyValuePair<string, OrderedDictionary<string, List<string>>> sectData
                    = this._dataStore[i];
                this.OutputSection(sectData.Key, lineReceiverMethod, stripComments,
                    formatBuilder);
            }
        }

        public void OutputSection(string sectionName, Action<string> lineReceiverMethod,
            bool stripComments)
        {
            StringBuilder builder = new StringBuilder(200);
            this.OutputSection(sectionName, lineReceiverMethod, stripComments, builder);
        }

        public void OutputSection(string sectionName, Action<string> lineReceiverMethod, 
            bool stripComments, StringBuilder formatBuffer)
        {
            OrderedDictionary<string, List<string>> sectData;

            if (!this.TryGetRef(sectionName, out sectData))
            {
                this.ThrowException(new Exceptions.SectionNotFoundException(sectionName));
            }

            List<CommentInfo> sectNormalComments
                    = this._dataNormalComments[sectionName];
            Dictionary<string, List<string>> sectInlineComments
                = this._dataInlineComments[sectionName];

            // Try to get section policy overrides struct
            SectionOverrides overrides = default(SectionOverrides);
            this._sectOverrides.TryGetValue(sectionName, out overrides);

            // Use the double allocation strategy for the output lines list
            List<string> outputLines = new List<string>();
            if (sectData != null)
                outputLines.Capacity = sectData.Count * 2;

            // Output section header directly for non-empty section name only
            // This effectively skips writing [] for the presection data/comments.
            if (!String.IsNullOrEmpty(sectionName))
            {
                formatBuffer.AppendFormat("[{0}]",
                    (overrides.SectionNameQuotes == QuotePolicy.On) ? 
                    this._csvParser.ComponentToString(sectionName) : sectionName);
                lineReceiverMethod(formatBuffer.ToString());
                formatBuffer.Length = 0;
            }

            int commentIndex = 0;
            // Write section data
            if (sectData != null)
            {
                // Try to get repetitive key information for this section
                Dictionary<string, List<int>> sectRepeatKeys = null;
                this._dataRepeatKeys.TryGetValue(sectionName, out sectRepeatKeys);

                int keyIndex = 0;
                int keyIndexShift = 0;
                foreach (KeyValuePair<string, List<string>> entry in sectData)
                {
                    if (entry.Value != null)
                    {
                        // Try to get repeating key info for this key
                        // only if it ever had a value (to save unnecessary lookup)
                        List<int> repeatKeyValIndices = null;
                        if (sectRepeatKeys != null)
                        {
                            sectRepeatKeys.TryGetValue(entry.Key,
                                out repeatKeyValIndices);
                        }

                        if (repeatKeyValIndices == null)
                        {
                            repeatKeyValIndices = new List<int>(1);
                            repeatKeyValIndices.Add(entry.Value.Count);
                        }
                        List<string[]> dividedValues = SplitList<string>(
                            entry.Value, repeatKeyValIndices);

                        foreach (string[] csvSplit in dividedValues)
                        {
                            formatBuffer.AppendFormat("{0} = {1}",
                                this._csvParser.ComponentToString(entry.Key),
                                this._csvParser.Join(csvSplit, c_TrimEnd, 
                                overrides.MinimumCsv, this.EnableLastValueQuotes));
                            formatBuffer.AppendLine();
                        }

                        formatBuffer.Length -= Environment.NewLine.Length;
                    }
                    else
                    {
                        formatBuffer.Append(entry.Key);
                    }

                    // Throw in the inline comments
                    if (sectInlineComments != null && !stripComments
                        && sectInlineComments.ContainsKey(entry.Key))
                    {
                        List<string> inlineComments = sectInlineComments[entry.Key];
                        if (inlineComments.Count > 0)
                        {
                            formatBuffer.AppendFormat(" ;{0}", inlineComments[0]);
                        }
                        for (int j = 1; j < inlineComments.Count; j++)
                        {
                            formatBuffer.AppendFormat(" - {0}", inlineComments[j]);
                        }
                    }

                    outputLines.Add(formatBuffer.ToString());
                    formatBuffer.Length = 0;

                    // Push in the normal comments

                    // FIXED: The problem is that adding a comment from before
                    // pushes the keys downwards so its index is no longer equal
                    // to the index in the dictionary.
                    int startIndex = -1;
                    int number = 0;
                    for (; commentIndex < sectNormalComments.Count; commentIndex++)
                    {
                        CommentInfo currentComment = sectNormalComments[commentIndex];
                        if (currentComment.KeyIndex < keyIndex) continue;
                        else if (currentComment.KeyIndex == keyIndex)
                        {
                            if (startIndex < 0) startIndex = commentIndex;
                            number++;
                        }
                        else break;
                    }

                    if (startIndex >= 0)
                    {
                        CommentInfo[] array = new CommentInfo[number];
                        sectNormalComments.CopyTo(startIndex, array, 0, number);
                        Array.Reverse(array);

                        foreach (CommentInfo info in array)
                        {
                            formatBuffer.AppendFormat(";{0}", info.CommentText);
                            outputLines.Insert(keyIndex + keyIndexShift,
                                formatBuffer.ToString());
                            formatBuffer.Length = 0;
                        }

                        keyIndexShift += array.Length;
                    }

                    // Increment the artifical key counter
                    keyIndex++;
                }
            }

            // Write comments that are attached to the final "ghost" key
            for (; commentIndex < sectNormalComments.Count; commentIndex++)
            {
                CommentInfo currentComment = sectNormalComments[commentIndex];
                Debug.Assert(currentComment.KeyIndex ==
                    ((sectData != null) ? sectData.Count : 0));

                formatBuffer.AppendFormat(";{0}", currentComment.CommentText);
                outputLines.Add(formatBuffer.ToString());
                formatBuffer.Length = 0;
            }

            // Write an empty line after the section only in case of
            // non-empty section name and in case of presections comment or data.
            if (!String.IsNullOrEmpty(sectionName)
                || (String.IsNullOrEmpty(sectionName) && sectData != null &&
                (sectData.Count > 0 || sectNormalComments.Count > 0)))
            {
                outputLines.Add(String.Empty);
            }

            // Flush all the lines via the delegate
            foreach (string line in outputLines)
            {
                lineReceiverMethod(line);
            }

            Debug.Assert(formatBuffer.Length == 0, 
                "Returning without emptying the buffer.");
        }

        public void SaveIni()
        {
            this.SaveIni(false);
        }

        public void SaveIni(bool stripComments)
        {
            this.SaveIni(this._fileEncoding, stripComments);
        }

        public void SaveIni(Encoding encoding, bool stripComments)
        {
            this.SaveIni(this._fileInfo.FullName, this._fileEncoding, stripComments);
        }

        public void SaveIni(string filename)
        {
            this.SaveIni(filename, false);
        }

        public void SaveIni(string filename, bool stripComments)
        {
            this.SaveIni(filename, this._fileEncoding, stripComments);
        }

        public void SaveIni(string filename, Encoding encoding, bool stripComments)
        {
            StreamWriter writer = new StreamWriter(filename, false, encoding);
            this.SaveIni(writer, encoding, stripComments);
            writer.Close();
        }

        public void SaveIni(TextWriter writer, Encoding encoding, bool stripComments)
        {
            this.OutputIni(writer.WriteLine, stripComments);
            writer.Flush();
        }

        public OrderedDictionary<string, string> ReadSectionJoinedValues(string section)
        {
            return this.ReadSectionJoinedValues(section, null);
        }

        public OrderedDictionary<string, string> ReadSectionJoinedValues(string section,
            IDictionary<string, string> varDict)
        {
            OrderedDictionary<string, List<string>> sectDict = this.GetRef(section);

            if (varDict != null)
                sectDict = this.SubstituteVariables(sectDict, varDict);

            return this.JoinDictValues(sectDict);
        }

        public ICollection<KeyValuePair<string, string>> ReadSectionJoinedValues(
            string section, IDictionary<string, string> varDict, bool noMergeDupKeys)
        {
            Dictionary<string, List<int>> repeatKeysDict;
            if (noMergeDupKeys 
                && this._dataRepeatKeys.TryGetValue(section, out repeatKeysDict))
            {
                return this.JoinDictValues(
                    this.ReadSection(section, varDict, noMergeDupKeys));
            }
            else
            {
                return this.ReadSectionJoinedValues(section, varDict);
            }
        }

        public OrderedDictionary<string, string> JoinDictValues(
            OrderedDictionary<string, List<string>> originalDict)
        {
            OrderedDictionary<string, string> joinedValsDict
                = new OrderedDictionary<string, string>(originalDict.Count,
                originalDict.KeyComparer, originalDict.KeyComparer);
            foreach (KeyValuePair<string, List<string>> pair in originalDict)
            {
                joinedValsDict.Add(pair.Key, this.JoinMultiValue(pair.Value));
            }
            return joinedValsDict;
        }

        public List<KeyValuePair<string, string>> JoinDictValues(
            ICollection<KeyValuePair<string, List<string>>> originalDict)
        {
            List<KeyValuePair<string, string>> joinedValsDict
                = new List<KeyValuePair<string, string>>(originalDict.Count);
            foreach (KeyValuePair<string, List<string>> pair in originalDict)
            {
                joinedValsDict.Add(new KeyValuePair<string, string>(
                    pair.Key, this.JoinMultiValue(pair.Value)));
            }
            return joinedValsDict;
        }

        public List<string[]> ReadCsvLines(string section)
        {
            return this.ReadCsvLines(section, 0);
        }

        public List<string[]> ReadCsvLines(string section,
            IDictionary<string, string> variableDictionary)
        {
            return this.ReadCsvLines(section, 0, variableDictionary);
        }

        public List<string[]> ReadCsvLines(string section, int maxElems)
        {
            return this.ReadCsvLines(section, maxElems, null);
        }

        public List<string[]> ReadCsvLines(string section, int maxElems,
            IDictionary<string, string> variableDictionary)
        {
            OrderedDictionary<string, List<string>> sectionData = this.GetRef(section);
            List<string[]> entries = new List<string[]>(sectionData.Count);
            foreach (KeyValuePair<string, List<string>> sectionEntry in sectionData)
            {
                entries.Add(this._csvParser.Parse(sectionEntry.Key, maxElems, 
                    variableDictionary));
            }
            return entries;
        }

        public OrderedDictionary<string, List<string>> SubstituteVariables(
            OrderedDictionary<string, List<string>> sectionData,
            IDictionary<string, string> variableDictionary)
        {
            OrderedDictionary<string, List<string>> newSectionData
                = new OrderedDictionary<string, List<string>>(sectionData.Count);
            foreach (KeyValuePair<string, List<string>> entry in sectionData)
            {
                List<string> newValues = null;
                if (entry.Value != null)
                {
                    newValues = new List<string>(entry.Value.Count);
                    foreach (string value in entry.Value)
                    {
                        newValues.Add(
                            this._csvParser.SubstituteVariables(variableDictionary,
                            value));
                    }
                }
                newSectionData.Add(
                    this._csvParser.SubstituteVariables(variableDictionary, entry.Key),
                    newValues);
            }
            return newSectionData;
        }
        #endregion

        #region Private methods
        void ThrowCSVKVPairConversion()
        {
            this.ThrowException(new Exceptions.NotSupportedException(
                "Converting a CSV line to a normal key."
                ));
        }

        bool RemoveKeyReorderComments(string section,
            OrderedDictionary<string, List<string>> sectionDictionary, string keyOrLine)
        {
            int removedIndex = sectionDictionary.IndexOf(keyOrLine);
            if (removedIndex >= 0)
            {
                bool removed = sectionDictionary.Remove(keyOrLine);
                if (removed)
                {
                    // Indices to which comments are tied after the key removed
                    // must be decremented.
                    foreach (CommentInfo commentInfo in this._dataNormalComments[section])
                    {
                        if (commentInfo.KeyIndex > removedIndex)
                            commentInfo.KeyIndex--;
                    }

                    // Try to remove from repeating key dict as well (to save memory)
                    this._dataRepeatKeys.Remove(keyOrLine);
                }
                return removed;
            }
            else
            {
                return false;
            }
        }

        string JoinMultiValue(IEnumerable<string> multiValue)
        {
            return this.JoinMultiValue(multiValue, c_TrimEnd);
        }

        string JoinMultiValue(IEnumerable<string> multiValue, bool trimEnd)
        {
            int count = 0;
            string firstItem = String.Empty;

            if (multiValue is ICollection<string>) count = ((ICollection<string>)multiValue).Count;
            else if (multiValue is string[]) count = ((string[])multiValue).Length;
            else
            {
                foreach (string item in multiValue)
                {
                    count++;
                }
            }

            if (count > 1) return this._csvParser.Join(multiValue, trimEnd);
            else
            {
                foreach (string item in multiValue)
                {
                    firstItem = item;
                    break;
                }

                return firstItem;
            }
        }

        void ThrowException(Exceptions.IniParserException ex)
        {
            ex.Data.Add("Identifier", this._fileInfo.Name);
            throw ex;
        }
        #endregion

        #region Parser core
        void ParseIniCore(TextReader textReader,
            bool throwForMalformedLines, string firstSectionName, bool allowSections)
        {
            Debug.WriteLine(String.Format(
                "Entering parseIniText: throwOnMalformed: {0} - allowSect: {1}",
                throwForMalformedLines, allowSections));

            // Dictionary cannot take null keys
            if (firstSectionName == null) firstSectionName = String.Empty;

            // Local vars declarations
            string line = null;
            int lineCounter = 0;
            string currentSection = firstSectionName;
            StringBuilder lineBuffer = new StringBuilder(200);

            // Section data and section comments
            OrderedDictionary<string, List<string>> sectionContents = null;
            List<CommentInfo> sectionNormalComments = null;
            Dictionary<string, List<string>> sectionInlineComments = null;
            Dictionary<string, List<int>> sectionRepeatingKeys = null;

            // If we are reading a new INI file, all 3 MUST return false
            bool dataFetched = this._dataStore.TryGetValue(firstSectionName, 
                out sectionContents);
            bool normalCommentsFetched = this._dataNormalComments.TryGetValue(
                firstSectionName, out sectionNormalComments);
            bool inlineCommentsFetched = this._dataInlineComments.TryGetValue(
                firstSectionName, out sectionInlineComments);
            bool repeatingKeysFetched = this._dataRepeatKeys.TryGetValue(
                firstSectionName, out sectionRepeatingKeys);

            Debug.Assert(AreEqual(dataFetched, normalCommentsFetched, 
                inlineCommentsFetched, repeatingKeysFetched),
                String.Format("[{0}]: Dictionaries not in sync.", firstSectionName),
                "Each section must have all dicts, even if their Count is 0.");
            if (!dataFetched)
            {
                this._dataStore.Add(firstSectionName, null);
                sectionContents = new OrderedDictionary<string, List<string>>(
                    StringComparer.OrdinalIgnoreCase);

                this._dataNormalComments.Add(firstSectionName, null);
                sectionNormalComments = new List<CommentInfo>(5);

                this._dataInlineComments.Add(firstSectionName, null);
                sectionInlineComments = new Dictionary<string, List<string>>(
                    StringComparer.OrdinalIgnoreCase);

                this._dataRepeatKeys.Add(firstSectionName, null);
                sectionRepeatingKeys = new Dictionary<string, List<int>>(
                    StringComparer.OrdinalIgnoreCase);
            }

            // Reader loop, line by line
            while ((line = textReader.ReadLine()) != null)
            {
                // Increment global ini line number
                lineCounter++;
                line = line.Trim();

                if (line.Length == 0) continue;

                // Comment on separate line
                if (c_CommentChars.IndexOf(line[0]) >= 0)
                {
                    CommentInfo comment = new CommentInfo(
                        Math.Max(0, sectionContents.Count), line.Substring(1));

                    Debug.WriteLine(String.Format("Adding comment: [{0}], position: {1}",
                        line.Substring(1), sectionContents.Count));

                    sectionNormalComments.Add(comment);
                    continue;
                }

                // Section header
                if (line[0] == '[')
                {
                    if (allowSections)
                    {
                        string newSectionName = this.ParseSectionHeader(line, 
                            lineBuffer);
                        Debug.WriteLine(String.Format("Starting new section: [{0}]",
                            newSectionName));

                        if (String.IsNullOrEmpty(newSectionName))
                        {
                            this.ThrowException(
                                new Exceptions.MalformedSectionHeaderException(
                                currentSection, line, lineCounter));
                        }
                        else
                        {
                            // Set the data of the current section 
                            // (thus "terminating" it)
                            this._dataStore[currentSection] = sectionContents;
                            this._dataNormalComments[currentSection] 
                                = sectionNormalComments;
                            this._dataInlineComments[currentSection]
                                = sectionInlineComments;
                            this._dataRepeatKeys[currentSection]
                                = sectionRepeatingKeys;

                            // Add null entries to the dictionaries for the new section
                            if (!this._dataStore.ContainsKey(newSectionName))
                            {
                                this._dataStore.Add(newSectionName, null);
                                this._dataNormalComments.Add(newSectionName, null);
                                this._dataInlineComments.Add(newSectionName, null);
                                this._dataRepeatKeys.Add(newSectionName, null);

                                // Set the data holder references to new instances
                                // (Thus starting data for a new section)
                                sectionContents
                                    = new OrderedDictionary<string, List<string>>(
                                    StringComparer.OrdinalIgnoreCase);
                                sectionNormalComments = new List<CommentInfo>(5);
                                sectionInlineComments
                                    = new Dictionary<string, List<string>>(
                                    StringComparer.OrdinalIgnoreCase);
                                sectionRepeatingKeys
                                    = new Dictionary<string,List<int>>(
                                    StringComparer.OrdinalIgnoreCase);
                            }
                            else
                            {
                                // Section already exists, set the data holder
                                // references to the ones in the dictionaries. This
                                // will effectively "merge" duplicate sections.
                                Debug.WriteLine(String.Format("Merging section: [{0}]",
                                    newSectionName));
                                sectionContents = this._dataStore[newSectionName];
                                sectionNormalComments 
                                    = this._dataNormalComments[newSectionName];
                                sectionInlineComments
                                    = this._dataInlineComments[newSectionName];
                                sectionRepeatingKeys
                                    = this._dataRepeatKeys[newSectionName];
                            }

                            // Change last section name ref param to the newly
                            // parsed section name.
                            currentSection = newSectionName;
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException(String.Format(
                            "Sections not allowed in data fragment. Encountered at line {0}.",
                            lineCounter));
                    }
                }
                else
                {
                    bool isMalformed;
                    lineBuffer.Append(line);
                    ResolveMultilineValues(textReader, lineBuffer);
                    line = lineBuffer.ToString();
                    lineBuffer.Length = 0;

                    int firstCommentCharPos = -1;
                    KeyValuePair<string, string[]> pair;
                    isMalformed = this._kvParser.Parse(line, out pair, 
                        out firstCommentCharPos);

                    if (isMalformed && throwForMalformedLines)
                    {
                        this.ThrowException(
                            new Exceptions.MalformedLineException(currentSection,
                            line, lineCounter));
                    }
                    else if (sectionContents != null)
                    {
                        // Take key and value out of readonly wrapper
                        string key = pair.Key;
                        string[] values = pair.Value;

                        // If we're dealing with a valueless key, 
                        // it's probably a CSV line, so we parse 
                        // and rejoin to obtain a more predictable form
                        if (values == null || values.Length == 0)
                        {
                            string csvLine = (firstCommentCharPos >= 0) ?
                                line.Substring(0, firstCommentCharPos) : line;
                            key = this._csvParser.ParseAndRejoin(csvLine);
                        }

                        // Extract inline comment text if position returned
                        // by KVParser is not negative
                        if (firstCommentCharPos > 0)
                        {
                            string commentText = line.Substring(firstCommentCharPos + 1);
                            if (!sectionInlineComments.ContainsKey(key))
                            {
                                List<string> inlineComments = new List<string>();
                                inlineComments.Add(commentText);
                                sectionInlineComments.Add(key, inlineComments);
                            }
                            else
                            {
                                // Add to existing
                                sectionInlineComments[key].Add(commentText);
                            }
                        }

                        // Perf killer:
                        // Debug.WriteLine(String.Format("Adding key: \"{0}\"", key));

                        if (!sectionContents.ContainsKey(key))
                        {
                            List<string> listString = 
                                (values != null)? new List<string>(values) : null;
                            sectionContents.Add(key, listString);
                        }
                        else if (values != null)
                        {
                            if (sectionContents[key] == null)
                            {
                                sectionContents[key] = new List<string>();
                            }
                            else
                            {
                                List<int> dupKeyValBeginIndices;
                                if (!sectionRepeatingKeys.TryGetValue(key,
                                    out dupKeyValBeginIndices))
                                {
                                    dupKeyValBeginIndices = new List<int>();
                                    sectionRepeatingKeys.Add(key, dupKeyValBeginIndices);
                                }
                                dupKeyValBeginIndices.Add(sectionContents[key].Count);
                            }
                            sectionContents[key].AddRange(values);
                        }
                    }
                }
            }

            // Add the data and comments of the last section
            this._dataStore[currentSection] = sectionContents;
            this._dataNormalComments[currentSection] = sectionNormalComments;
            this._dataInlineComments[currentSection] = sectionInlineComments;
            this._dataRepeatKeys[currentSection] = sectionRepeatingKeys;
        }

        string ParseSectionHeader(string line, StringBuilder buffer)
        {
            // String reference to hold final output
            int i = 1;
            string sectionName = null;
            bool foundLastBracket = false;

            // Start from 1 to skip over the '['
            for (; i < line.Length; i++)
            {
                char chr = line[i];
                if (chr == ']')
                {
                    sectionName = buffer.ToString();
                    buffer.Length = 0;
                    foundLastBracket = true;
                    break;
                }
                else if (chr == '"')
                {
                    int j = i + 1;
                    while (j < line.Length)
                    {
                        char chr2 = line[j];
                        if (chr2 == '"')
                        {
                            if ((j + 1) >= line.Length)
                            {
                                // End of quote and end of line 
                                // Malformed section header
                                buffer.Length = 0;
                                return null;
                            }
                            else
                            {
                                char chr3 = line[++j];
                                if (chr3 == '"')
                                {
                                    // Quote in string value
                                    buffer.Append(chr3);
                                }
                                else if (chr3 == ']')
                                {
                                    i = --j;
                                    break;
                                }
                                else if (chr3 == '\0')
                                {
                                    base.ThrowNullCharEncountered(line);
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
                            buffer.Append(chr2);
                        }
                        j++;
                    }
                }
                else if (c_CommentChars.IndexOf(chr) >= 0)
                {
                    // malformed section header
                    buffer.Length = 0;
                    return null;
                }
                else if (chr == '\0')
                {
                    base.ThrowNullCharEncountered(line);
                }
                else
                {
                    buffer.Append(chr);
                }
            }

            if (++i < line.Length || !foundLastBracket)
            {
                // Other data on line after closing section header bracket
                // Comments on same line as section header are not supported yet.
                return null;
            }
            else
            {
                // Return the section name
                return sectionName;
            }
        }

        static void ResolveMultilineValues(TextReader textReader, 
            StringBuilder lineBuffer)
        {
            string nextLine;
            if (lineBuffer[lineBuffer.Length - 1] == '\\' 
                && (nextLine = textReader.ReadLine()) != null)
            {
                nextLine = nextLine.Trim();
                lineBuffer.Remove(lineBuffer.Length - 1, 1);
                lineBuffer.Append(nextLine);
                ResolveMultilineValues(textReader, lineBuffer);
            }
        }

        static List<T[]> SplitList<T>(List<T> list, IList<int> splitIndices)
        {
            List<T[]> dividedValues = new List<T[]>(splitIndices.Count);

            int lastIndex = 0;
            foreach (int index in splitIndices)
            {
                if (index >= list.Count) break;
                else
                {
                    int lengthToCopy = index - lastIndex;
                    T[] divValue = new T[lengthToCopy];
                    list.CopyTo(lastIndex, divValue, 0, divValue.Length);
                    dividedValues.Add(divValue);
                    lastIndex = index;
                }
            }

            // Copy all after lastIndex upto the end into a new part
            T[] finalPart = new T[list.Count - lastIndex];
            list.CopyTo(lastIndex, finalPart, 0, finalPart.Length);
            dividedValues.Add(finalPart);

            return dividedValues;
        }

        static bool AreEqual(params bool[] args)
        {
            bool finalResult = true;
            bool tester = args[0];
            foreach (bool val in args)
            {
                finalResult &= tester == val;
                if (!finalResult) break;
            }
            return finalResult;
        }
        #endregion

        #region Enumerations
        public enum KeyExistsPolicy
        {
            /// <summary>
            /// Ignore any new values and return.
            /// </summary>
            Ignore,
            /// <summary>
            /// Throw a "Key already exists" exception.
            /// </summary>
            ThrowException,
            /// <summary>
            /// Discard the old values and use the new ones.
            /// </summary>
            Discard,
            /// <summary>
            /// Append the new values to the old ones 
            /// as part of the original CSV list. Duplicate
            /// values are not checked.
            /// </summary>
            Append,
            /// <summary>
            /// Merges the new values with the old ones
            /// making sure that the resulting CSV list does
            /// not contain any repeating values.
            /// </summary>
            Merge,
            /// <summary>
            /// Store the values in a new CSV list. Key will
            /// be written an additional time with this new 
            /// CSV list in the ouput.
            /// </summary>
            CreateNewList
        }

        public enum QuotePolicy : byte
        {
            Off = 0x0,
            On = 0x1,
            Default = Off,
        }
        #endregion

        #region Private classes
        class CommentInfo
        {
            public int KeyIndex;
            public readonly string CommentText;

            public CommentInfo(int keyIndex, string commentText)
            {
                this.KeyIndex = keyIndex;
                this.CommentText = commentText;
            }
        }
        #endregion

        #region Per-section override struct
        public struct SectionOverrides
        {
            public readonly byte MinimumCsv;
            public readonly QuotePolicy SectionNameQuotes;

            public SectionOverrides(byte minCsv)
            {
                this.MinimumCsv = minCsv;
                this.SectionNameQuotes = QuotePolicy.Default;
            }

            public SectionOverrides(byte minCsv, QuotePolicy sectNameQuotes)
            {
                this.MinimumCsv = minCsv;

                if (sectNameQuotes != QuotePolicy.On && sectNameQuotes != QuotePolicy.Off)
                {
                    throw new ArgumentOutOfRangeException("sectNameQuotes");
                }

                this.SectionNameQuotes = sectNameQuotes;
            }
        }
        #endregion

        #region Exceptions and error handling
        public class Exceptions
        {
            public class IniParserException : ParserBase.ParserException
            {
                CSVParser _csvParser;

                public IniParserException(string message) : this(message, null) { }

                public IniParserException(string message, Exception innerException)
                    : base(message, innerException)
                {
                    this._csvParser = new CSVParser();
                }

                protected string ComponentToString(string component)
                {
                    return _csvParser.ComponentToString(component);
                }
            }

            public class NotSupportedException : IniParserException
            {
                new const string c_DefaultMessage = "This operation is not supported.";

                public NotSupportedException(string opDesc)
                    : base(c_DefaultMessage)
                {
                    base.Data.Add("Operation", opDesc);
                }
            }

            public class KeyException : IniParserException
            {
                public KeyException(string message, string section, string key)
                    : base(message)
                {
                    base.Data.Add("Section", section);
                    base.Data.Add("Key", key);
                }
            }

            public class KeyAlreadyExistsException : KeyException
            {
                public KeyAlreadyExistsException(string section, string key)
                    : base("The section already contains this key or line.", section, key) { }
            }

            public class KeyNotFoundException : KeyException
            {
                public KeyNotFoundException(string section, string key)
                    : base("The section does not contain this key or line.", section, key) { }
            }

            public class SectionNotFoundException : IniParserException
            {
                public SectionNotFoundException(string section)
                    : base("The section does not exist in this ini.")
                {
                    base.Data.Add("Section", section);
                }
            }

            public class MalformedInputException : IniParserException
            {
                new const string c_DefaultMessage = "Malformed input encountered.";

                public MalformedInputException(string section, string input, int lineNumber)
                    : this(c_DefaultMessage, section, input, lineNumber) { }

                public MalformedInputException(string message, 
                    string section, string input, int lineNumber) : base(message)
                {
                    base.Data.Add("Section", section);
                    base.Data.Add("Input", input);
                    base.Data.Add("Line number", lineNumber);
                }
            }

            public class MalformedLineException : MalformedInputException
            {
                const string c_Message = "Malformed line encountered.";

                public MalformedLineException(string section, string line, int lineNumber)
                    : base(c_Message, section, line, lineNumber) { }
            }

            public class MalformedSectionHeaderException : MalformedInputException
            {
                const string c_Message 
                    = "Malformed section header encountered. Last section name included in report.";

                public MalformedSectionHeaderException(string section, string header, 
                    int lineNumber) : base(c_Message, section, header, lineNumber) { }
            }
        }
        #endregion
    }
}
