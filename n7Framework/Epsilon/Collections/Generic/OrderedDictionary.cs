using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Collections;

namespace Epsilon.Collections.Generic
{
    public class OrderedDictionary<TKey, TValue> 
        : IDictionary<TKey, TValue>, IList<KeyValuePair<TKey, TValue>>
    {
        #region IndexedObject
        class IndexedObject
        {
            internal int Index;
            internal TKey Key;
            internal TValue Value;

            internal IndexedObject(int index, TKey key, TValue value)
            {
                this.Index = index;
                this.Key = key;
                this.Value = value;
            }
        }
        #endregion

        #region Private members
        int _maxIndex;
        Dictionary<TKey, IndexedObject> _dictionary;
        List<IndexedObject> _list;
        const int _defaultCapacity = 4;
        #endregion

        #region Public members
        public readonly IEqualityComparer<TKey> KeyComparer;
        public readonly IEqualityComparer<TValue> ValueComparer;
        #endregion

        #region Constructors
        public OrderedDictionary() : this(_defaultCapacity) { }

        public OrderedDictionary(IEqualityComparer<TKey> keyComparer)
            : this(_defaultCapacity, keyComparer) { }

        public OrderedDictionary(IEqualityComparer<TKey> keyComparer,
            IEqualityComparer<TValue> valueComparer) 
            : this(_defaultCapacity, keyComparer, valueComparer) { }

        public OrderedDictionary(int initialCapacity)
            : this(initialCapacity, EqualityComparer<TKey>.Default) { }

        public OrderedDictionary(int initialCapacity, IEqualityComparer<TKey> keyComparer)
            : this(initialCapacity, keyComparer, EqualityComparer<TValue>.Default) { }

        public OrderedDictionary(int initialCapacity, 
            IEqualityComparer<TKey> keyComparer, 
            IEqualityComparer<TValue> valueComparer)
        {
            this._dictionary = new Dictionary<TKey, IndexedObject>(initialCapacity, 
                keyComparer);
            this._list = new List<IndexedObject>(initialCapacity);
            this.KeyComparer = keyComparer;
            this.ValueComparer = valueComparer;
            this._maxIndex = -1;
        }

        public OrderedDictionary(IDictionary<TKey, TValue> existingDictionary)
            : this(existingDictionary, EqualityComparer<TKey>.Default) { }

        public OrderedDictionary(IDictionary<TKey, TValue> existingDictionary, 
            IEqualityComparer<TKey> keyComparer)
            : this(existingDictionary, keyComparer, EqualityComparer<TValue>.Default) { }

        public OrderedDictionary(IDictionary<TKey, TValue> existingDictionary, 
            IEqualityComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer)
            : this(existingDictionary.Count, keyComparer, valueComparer)
        {
            foreach (KeyValuePair<TKey, TValue> pair in existingDictionary)
            {
                this.Add(pair.Key, pair.Value);
            }
        }
        #endregion

        #region IDictionary<TKey, TValue> Members
        public void Add(TKey key, TValue value)
        {
            this._maxIndex++;
            IndexedObject io = new IndexedObject(this._maxIndex, key, value);
            this._dictionary.Add(key, io);
            this._list.Add(io);
        }

        public bool ContainsKey(TKey key)
        {
            return this._dictionary.ContainsKey(key);
        }

        public ICollection<TKey> Keys
        {
            get { return this._dictionary.Keys; }
        }

        public bool Remove(TKey key)
        {
            return this.Remove(key, default(TValue), true);
        }

        public bool Remove(TKey key, TValue value)
        {
            return this.Remove(key, value, false);
        }

        bool Remove(TKey key, TValue value, bool ignoreValue)
        {
            IndexedObject io;
            if (this._dictionary.TryGetValue(key, out io))
            {
                if (!ignoreValue && value != null && 
                    !this.ValueComparer.Equals(io.Value, value)) return false;
                bool result = this._dictionary.Remove(key);
                this._list.RemoveAt(io.Index);
                this._maxIndex--;
                this.DecrementIndicesInList(io.Index);
                return result;
            }
            else return false;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            IndexedObject io;
            if (this._dictionary.TryGetValue(key, out io))
            {
                value = io.Value;
                return true;
            }
            else
            {
                value = default(TValue);
                return false;
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                List<TValue> values = new List<TValue>(this._dictionary.Count);
                foreach (IndexedObject io in this._list)
                {
                    values.Add(io.Value);
                }
                return values.AsReadOnly();
            }
        }

        public TValue this[TKey key]
        {
            get { return this._dictionary[key].Value; }
            set { this._dictionary[key].Value = value; }
        }
        #endregion

        #region ICollection<KeyValuePair<TKey,TValue>> Members
        public void Add(KeyValuePair<TKey, TValue> item)
        {
            this.Add(item.Key, item.Value);
        }

        public void Clear()
        {
            this._maxIndex = -1;
            this._dictionary.Clear();
            this._list.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            IndexedObject io;
            if (this._dictionary.TryGetValue(item.Key, out io))
            {
                return this.ValueComparer.Equals(io.Value, item.Value);
            }
            else return false;
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            int maxLength = Math.Min(this._list.Count, array.Length);
            int listCounter = 0;
            for (int i = arrayIndex; i < maxLength; i++)
            {
                IndexedObject io = this._list[listCounter];
                array[i] = new KeyValuePair<TKey, TValue>(io.Key, io.Value);
                listCounter++;
            }
        }

        public int Count
        {
            get { return this._dictionary.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return this.Remove(item.Key);
        }
        #endregion

        #region IEnumerable<KeyValuePair<TKey,TValue>> Members
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            foreach (IndexedObject io in this._list.AsReadOnly())
            {
                yield return new KeyValuePair<TKey, TValue>(io.Key, io.Value);
            }
        }
        #endregion

        #region IEnumerable Members
        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (IndexedObject io in this._list.AsReadOnly())
            {
                yield return new KeyValuePair<TKey, TValue>(io.Key, io.Value);
            }
        }
        #endregion

        #region IList<KeyValuePair<TKey,TValue>> Members
        public int IndexOf(KeyValuePair<TKey, TValue> item)
        {
            IndexedObject io;
            if (this._dictionary.TryGetValue(item.Key, out io))
            {
                if (item.Value != null && !this.ValueComparer.Equals(io.Value, 
                    item.Value))
                    return -1;
                return io.Index;
            }
            else return -1;
        }

        public void Insert(int index, KeyValuePair<TKey, TValue> item)
        {
            IndexedObject io = new IndexedObject(index, item.Key, item.Value);
            this._dictionary.Add(io.Key, io);
            this._list.Insert(index, io);
            this.IncrementIndicesInList(index + 1);
            this._maxIndex++;
        }

        public void RemoveAt(int index)
        {
            IndexedObject io = this._list[index];
            if (!this.Remove(io.Key))
                throw new KeyNotFoundException();
        }

        public KeyValuePair<TKey, TValue> this[int index]
        {
            get
            {
                IndexedObject io = this._list[index];
                return new KeyValuePair<TKey, TValue>(io.Key, io.Value);
            }
            set
            {
                IndexedObject io = this._list[index];
                io.Key = value.Key;
                io.Value = value.Value;
            }
        }
        #endregion

        #region List reindexing methods
        void DecrementIndicesInList(int startIndex)
        {
            for (int i = startIndex; i < this._list.Count; i++)
            {
                this._list[i].Index--;
            }
        }

        void IncrementIndicesInList(int startIndex)
        {
            for (int i = startIndex; i < this._list.Count; i++)
            {
                this._list[i].Index++;
            }
        }
        #endregion

        #region Public methods
        public int IndexOf(TKey key)
        {
            IndexedObject io;
            if (this._dictionary.TryGetValue(key, out io))
            {
                return io.Index;
            }
            else
            {
                return -1;
            }
        }

        public bool TryChangeKey(TKey key, TKey newKey)
        {
            TValue oldValue;
            if (this.TryGetValue(key, out oldValue))
            {
                return this.TryChangeKey(key, newKey, oldValue);
            }
            else return false;
        }

        public bool TryChangeKey(TKey key, TKey newKey, TValue newValue)
        {
            IndexedObject io;
            if (this._dictionary.TryGetValue(key, out io))
            {
                io.Key = newKey;
                io.Value = newValue;

                this._dictionary.Remove(key);
                this._dictionary.Add(key, io);

                return true;
            }
            else return false;
        }

        public bool TryChangeValue(TKey key, TValue newValue)
        {
            IndexedObject io;
            if (this._dictionary.TryGetValue(key, out io))
            {
                io.Value = newValue;
                return true;
            }
            else return false;
        }
        #endregion
    }
}
