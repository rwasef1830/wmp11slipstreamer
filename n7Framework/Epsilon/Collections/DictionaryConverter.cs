using System;
using System.Collections.Generic;
using System.Text;

namespace Epsilon.Collections
{
    public static class DictionaryConverter
    {
        public static Dictionary<TOutKey, TOutValue> 
            Convert<TInKey, TInValue, TOutKey, TOutValue>(
            IDictionary<TInKey, TInValue> dictionary,
            Converter<TInKey, TOutKey> keyConverter,
            Converter<TInValue, TOutValue> valueConverter)
        {
            Dictionary<TOutKey, TOutValue> convertedDictionary
                = new Dictionary<TOutKey, TOutValue>(dictionary.Count);

            foreach (KeyValuePair<TInKey, TInValue> pair in dictionary)
            {
                TOutKey outputKey = keyConverter(pair.Key);
                TOutValue outputValue = valueConverter(pair.Value);

                convertedDictionary.Add(outputKey, outputValue);
            }

            return convertedDictionary;
        }
    }
}
