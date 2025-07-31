using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DupTerminator.ImageHash
{
    /// <summary>
    /// A custom BitArray implementation to mimic Python's bitarray for the specific
    /// operations required by the MIHIndex class. This is not a full-featured
    /// bitarray, but provides necessary functionality like hex conversion,
    /// slicing, and individual bit access.
    /// </summary>
    public class CustomBitArray : IEquatable<CustomBitArray>, ICloneable
    {
        private byte[] _data;
        private int _lengthInBits;

        /// <summary>
        /// Initializes a new instance of the CustomBitArray class with the specified number of bits.
        /// All bits are initially set to false (0).
        /// </summary>
        /// <param name="lengthInBits">The total number of bits in the array.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if lengthInBits is negative.</exception>
        public CustomBitArray(int lengthInBits)
        {
            if (lengthInBits < 0)
                throw new ArgumentOutOfRangeException(nameof(lengthInBits), "Length in bits cannot be negative.");

            _lengthInBits = lengthInBits;
            _data = new byte[(lengthInBits + 7) / 8]; // Ceiling division for byte array size.
        }

        /// <summary>
        /// Initializes a new instance of the CustomBitArray class from a byte array.
        /// </summary>
        /// <param name="bytes">The byte array containing the bits.</param>
        /// <param name="lengthInBits">The total number of bits represented by the byte array.</param>
        /// <exception cref="ArgumentNullException">Thrown if bytes is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if lengthInBits is negative.</exception>
        /// <exception cref="ArgumentException">Thrown if the byte array is too short for the specified number of bits.</exception>
        public CustomBitArray(byte[] bytes, int lengthInBits)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));
            if (lengthInBits < 0)
                throw new ArgumentOutOfRangeException(nameof(lengthInBits), "Length in bits cannot be negative.");
            if (bytes.Length * 8 < lengthInBits)
                throw new ArgumentException("Byte array is too short for the specified number of bits.", nameof(bytes));

            _lengthInBits = lengthInBits;
            _data = new byte[bytes.Length];
            Buffer.BlockCopy(bytes, 0, _data, 0, bytes.Length);

            // Clear any unused bits in the last byte to ensure consistency.
            if (_lengthInBits % 8 != 0 && _data.Length > 0)
            {
                int lastByteIndex = _data.Length - 1;
                int bitsInLastByte = _lengthInBits % 8;
                byte mask = (byte)(0xFF << (8 - bitsInLastByte));
                _data[lastByteIndex] &= mask;
            }
        }

        /// <summary>
        /// Gets the total number of bits in the array.
        /// </summary>
        public int Length => _lengthInBits;

        /// <summary>
        /// Gets or sets the bit at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the bit to get or set.</param>
        /// <returns>The bit at the specified index (true for 1, false for 0).</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown if index is less than zero or greater than or equal to Length.</exception>
        public bool this[int index]
        {
            get
            {
                if (index < 0 || index >= _lengthInBits)
                    throw new IndexOutOfRangeException($"Index {index} is out of bounds for CustomBitArray of length {_lengthInBits} bits.");

                int byteIndex = index / 8;
                int bitOffset = index % 8; // Bit 0 is the MSB, bit 7 is the LSB.
                return (_data[byteIndex] & (1 << (7 - bitOffset))) != 0;
            }
            set
            {
                if (index < 0 || index >= _lengthInBits)
                    throw new IndexOutOfRangeException($"Index {index} is out of bounds for CustomBitArray of length {_lengthInBits} bits.");

                int byteIndex = index / 8;
                int bitOffset = index % 8;
                int mask = 1 << (7 - bitOffset);

                if (value)
                    _data[byteIndex] |= (byte)mask;
                else
                    _data[byteIndex] &= (byte)~mask;
            }
        }

        /// <summary>
        /// Creates a CustomBitArray from a hex string.
        /// </summary>
        /// <param name="hexString">The hex string representing the bits.</param>
        /// <param name="expectedLengthInBits">The expected total number of bits.</param>
        /// <returns>A new CustomBitArray instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown if hexString is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the hex string length does not match the expected bit length, or if it contains invalid hex characters.</exception>
        public static CustomBitArray FromHex(string hexString, int expectedLengthInBits)
        {
            if (hexString == null)
                throw new ArgumentNullException(nameof(hexString));
            if (hexString.Length * 4 != expectedLengthInBits)
                throw new ArgumentException($"Hex string length {hexString.Length} (representing {hexString.Length * 4} bits) does not match expected length {expectedLengthInBits} bits.", nameof(hexString));
            if (hexString.Length % 2 != 0)
                throw new ArgumentException("Hex string must have an even number of characters.", nameof(hexString));

            byte[] bytes = new byte[hexString.Length / 2];
            try
            {
                for (int i = 0; i < hexString.Length; i += 2)
                {
                    bytes[i / 2] = byte.Parse(hexString.Substring(i, 2), NumberStyles.HexNumber);
                }
            }
            catch (FormatException ex)
            {
                throw new ArgumentException("Invalid hex string format.", nameof(hexString), ex);
            }
            return new CustomBitArray(bytes, expectedLengthInBits);
        }

        /// <summary>
        /// Converts the CustomBitArray to a byte array.
        /// </summary>
        /// <returns>A new byte array representing the bits.</returns>
        public byte[] ToBytes()
        {
            byte[] result = new byte[_data.Length];
            Buffer.BlockCopy(_data, 0, result, 0, _data.Length);

            // Ensure unused bits in the last byte are cleared. This should already be handled by the constructor/setters
            // but adding it here defensively for the ToBytes representation.
            if (_lengthInBits % 8 != 0 && result.Length > 0)
            {
                int lastByteIndex = result.Length - 1;
                int bitsInLastByte = _lengthInBits % 8;
                byte mask = (byte)(0xFF << (8 - bitsInLastByte));
                result[lastByteIndex] &= mask;
            }

            return result;
        }

        /// <summary>
        /// Converts the CustomBitArray to a hex string.
        /// </summary>
        /// <returns>A hex string representation of the bits.</returns>
        public string ToHex()
        {
            StringBuilder hex = new StringBuilder(_data.Length * 2);
            for (int i = 0; i < _data.Length; i++)
            {
                hex.Append(_data[i].ToString("x2"));
            }
            return hex.ToString();
        }

        /// <summary>
        /// Creates a new CustomBitArray that is a slice of the current instance.
        /// </summary>
        /// <param name="startBit">The zero-based starting bit index of the slice.</param>
        /// <param name="length">The length of the slice in bits.</param>
        /// <returns>A new CustomBitArray representing the specified slice.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if startBit or length are out of bounds.</exception>
        public CustomBitArray Slice(int startBit, int length)
        {
            if (startBit < 0 || startBit >= _lengthInBits)
                throw new ArgumentOutOfRangeException(nameof(startBit), "Start bit is out of bounds.");
            if (length < 0 || startBit + length > _lengthInBits)
                throw new ArgumentOutOfRangeException(nameof(length), "Slice length is invalid or goes out of bounds.");

            CustomBitArray slicedArray = new CustomBitArray(length);
            for (int i = 0; i < length; i++)
            {
                slicedArray[i] = this[startBit + i];
            }
            return slicedArray;
        }

        /// <summary>
        /// Determines whether the specified CustomBitArray is equal to the current CustomBitArray.
        /// </summary>
        /// <param name="other">The CustomBitArray to compare with the current CustomBitArray.</param>
        /// <returns>true if the specified CustomBitArray is equal to the current CustomBitArray; otherwise, false.</returns>
        public bool Equals(CustomBitArray other)
        {
            if (ReferenceEquals(other, null)) return false;
            if (ReferenceEquals(this, other)) return true;

            if (_lengthInBits != other._lengthInBits) return false;
            if (_data.Length != other._data.Length) return false;

            // Compare byte by byte. Unused bits in the last byte should be zeroed out by construction.
            for (int i = 0; i < _data.Length; i++)
            {
                if (_data[i] != other._data[i])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current CustomBitArray.
        /// </summary>
        /// <param name="obj">The object to compare with the current CustomBitArray.</param>
        /// <returns>true if the specified object is equal to the current CustomBitArray; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as CustomBitArray);
        }

        /// <summary>
        /// Returns the hash code for this CustomBitArray instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            // A simple hash code, combining length and first few bytes.
            // For scenarios where CustomBitArray itself is used as a dictionary key frequently,
            // a more robust hash (like FNV-1a over all bytes) might be beneficial.
            // For this application, hex strings are dictionary keys, reducing direct CustomBitArray hashing.
            HashCode hash = new HashCode();
            hash.Add(_lengthInBits);
            for (int i = 0; i < _data.Length && i < 4; i++) // Combine first few bytes for a quicker hash
            {
                hash.Add(_data[i]);
            }
            return hash.ToHashCode();
        }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new CustomBitArray that is a copy of this instance.</returns>
        public object Clone()
        {
            return new CustomBitArray(ToBytes(), _lengthInBits);
        }
    }


    /// <summary>
    /// Implementation of MIH algorithm (see https://www.cs.toronto.edu/~norouzi/research/papers/multi_index_hashing.pdf
    /// (refer brief doc at https://github.com/facebook/ThreatExchange/blob/master/hashing/hashing.pdf)
    /// </summary>
    public class MIHIndex2
    {
        private readonly int _hashLength; // Hash size in bits
        private Dictionary<string, List<int>> _items; // Stores raw hex strings and their categories before training
        private List<(CustomBitArray hash, List<int> categories)> _index; // Stores processed bit arrays and categories after training
        private List<string> _categories; // Maps category index (int) to category name (string)
        private bool _trained;
        private Regex _pattern; // Regex to validate hex hash strings
        private int _floor; // floor(threshold / wordLength)
        private List<Dictionary<string, HashSet<int>>> _words; // List of dictionaries: word (hex string) -> set of indices of _index
        private int _wordLength; // Word length in bits
        private int _threshold; // Hamming distance threshold
        private object _cache; // Unused in Python, kept for parity.

        /// <summary>
        /// Initializes a new instance of the MIHIndex class.
        /// </summary>
        /// <param name="hashsize">Hash size in bits (not: NOT string length!). Defaults to 256.</param>
        /// <exception cref="ArgumentException">Thrown if hashsize is not a multiple of 8.</exception>
        public MIHIndex2(int hashsize = 256)
        {
            if (hashsize % 8 != 0)
            {
                throw new ArgumentException($"Hashsize {hashsize} is not a multiple of 8", nameof(hashsize));
            }
            _hashLength = hashsize;
            _items = null; // Will be initialized in Update
            _index = null; // Will be initialized in Train
            _categories = new List<string>();
            _trained = false;
            _pattern = new Regex("^[a-f0-9]{" + (hashsize / 4) + "}$", RegexOptions.IgnoreCase);
            _floor = 0; // Initialized in Train
            _words = null; // Initialized in Train
            _wordLength = 0; // Initialized in Train
            _threshold = 0; // Initialized in Train
            _cache = null; // Unused
        }

        /// <summary>
        /// Calculate and return a set of all hex combinations within the provided hamming distance.
        /// This is a recursive helper method.
        /// </summary>
        /// <param name="word">Candidate word as CustomBitArray. This instance will be modified during recursion.</param>
        /// <param name="distance">Maximum hamming distance (inclusive). Defaults to 2.</param>
        /// <param name="position">Position to commence comparison. For internal (recursive) use only.</param>
        /// <param name="entries">Set of possible entries. For internal (recursive) use only.</param>
        /// <returns>Set of hex strings within provided hamming distance of candidate word.</returns>
        private HashSet<string> GetWindow(CustomBitArray word, int distance = 2, int position = 0, HashSet<string> entries = null)
        {
            if (entries == null)
            {
                entries = new HashSet<string>();
            }

            if (position == word.Length)
            {
                entries.Add(word.ToHex());
                return entries;
            }

            if (distance > 0)
            {
                bool tempBit = word[position]; // Store original bit
                foreach (bool bitValue in new[] { true, false })
                {
                    word[position] = bitValue; // Flip bit
                    int distOffset = 0;
                    if (tempBit != word[position])
                    {
                        distOffset = -1; // Decrement remaining distance if bit was flipped
                    }
                    GetWindow(word, distance + distOffset, position + 1, entries);
                }
                word[position] = tempBit; // Restore original bit for other branches/recursion levels
            }
            else
            {
                GetWindow(word, distance, position + 1, entries);
            }
            return entries;
        }

        /// <summary>
        /// Add more entries for indexing, together with category/class. Accepts an iterable of hex strings.
        /// </summary>
        /// <param name="newItems">Iterable of hex strings to add.</param>
        /// <param name="category">Category/class name for the added strings (e.g., 'ignorable').</param>
        /// <exception cref="ArgumentException">Thrown if any string in newItems is not a valid hex string for the hash length.</exception>
        public void Update(IEnumerable<string> newItems, string category)
        {
            int offset = -1;
            // Find or add category
            for (int i = 0; i < _categories.Count; i++)
            {
                if (_categories[i] == category)
                {
                    offset = i;
                    break;
                }
            }
            if (offset == -1)
            {
                _categories.Add(category);
                offset = _categories.Count - 1;
            }

            if (_items == null)
            {
                _items = new Dictionary<string, List<int>>();
            }

            foreach (string x in newItems)
            {
                if (_pattern.IsMatch(x))
                {
                    if (!_items.ContainsKey(x))
                    {
                        _items[x] = new List<int> { offset };
                    }
                    else
                    {
                        if (!_items[x].Contains(offset))
                        {
                            _items[x].Add(offset);
                        }
                    }
                }
                else
                {
                    throw new ArgumentException($"Not a valid hex string: {x}", nameof(newItems));
                }
                _trained = false; // Mark index as untrained if new items are added
            }
        }

        /// <summary>
        /// Add a single entry for indexing, together with category/class.
        /// </summary>
        /// <param name="newItem">A single hex string to add.</param>
        /// <param name="category">Category/class name for the added string.</param>
        /// <exception cref="ArgumentException">Thrown if the newItem string is not a valid hex string for the hash length.</exception>
        public void Update(string newItem, string category)
        {
            Update(new[] { newItem }, category);
        }

        /// <summary>
        /// Train and initialise the index. (i.e., make it queryable)
        /// </summary>
        /// <param name="wordLength">Word length (in bits). Defaults to 16.</param>
        /// <param name="threshold">Threshold for matching - i.e., hamming distance needs to be &lt;= to guarantee entry to be returned. Defaults to 32.</param>
        /// <exception cref="InvalidOperationException">Thrown if the index is already trained.</exception>
        /// <exception cref="ArgumentException">Thrown if an invalid hash length is encountered during processing, or if wordLength is invalid.</exception>
        public void Train(int wordLength = 16, int threshold = 32)
        {
            if (_trained)
            {
                throw new InvalidOperationException("Index already trained. Rebuild object to train again.");
            }
            if (wordLength <= 0 || _hashLength % wordLength != 0)
            {
                throw new ArgumentException($"Word length {wordLength} must be positive and divide hash length {_hashLength} evenly.", nameof(wordLength));
            }

            // Handle case where no items were ever added
            if (_items == null)
            {
                _index = new List<(CustomBitArray hash, List<int> categories)>();
                _trained = true;
                _wordLength = wordLength;
                _threshold = threshold;
                _floor = (int)Math.Floor((double)threshold / wordLength);
                _words = new List<Dictionary<string, HashSet<int>>>();
                for (int i = 0; i < _hashLength; i += wordLength)
                {
                    _words.Add(new Dictionary<string, HashSet<int>>());
                }
                return;
            }

            _index = new List<(CustomBitArray hash, List<int> categories)>();
            foreach (var entry in _items)
            {
                string hexHash = entry.Key;
                List<int> categories = entry.Value;

                CustomBitArray b = CustomBitArray.FromHex(hexHash, _hashLength);
                // Redundant check due to FromHex, but original Python code had it.
                if (b.Length != _hashLength)
                {
                    throw new ArgumentException($"Invalid hash length encountered for '{hexHash}'. Expected {_hashLength} bits, got {b.Length} bits.", nameof(_items));
                }
                _index.Add((b, categories));
            }
            _items = null; // Free up memory after training

            _floor = (int)Math.Floor((double)threshold / wordLength);
            _wordLength = wordLength;
            _threshold = threshold;

            _words = new List<Dictionary<string, HashSet<int>>>();
            for (int i = 0; i < _hashLength; i += wordLength)
            {
                _words.Add(new Dictionary<string, HashSet<int>>());
            }

            for (int c = 0; c < _index.Count; c++)
            {
                CustomBitArray hashBits = _index[c].hash;
                int slot = 0;
                for (int i = 0; i < _hashLength; i += _wordLength)
                {
                    CustomBitArray wordSlice = hashBits.Slice(i, _wordLength);
                    string hexWord = wordSlice.ToHex();

                    if (!_words[slot].ContainsKey(hexWord))
                    {
                        _words[slot][hexWord] = new HashSet<int>();
                    }
                    _words[slot][hexWord].Add(c);
                    slot++;
                }
            }

            _trained = true;
        }

        /// <summary>
        /// Calculate hamming distance between two CustomBitArray hashes.
        /// </summary>
        /// <param name="hash1">Candidate hash.</param>
        /// <param name="hash2">Comparison hash.</param>
        /// <param name="maxHd">Maximum hamming distance. If null, defaults to length of hash1.
        /// If the distance exceeds this value, null is returned.</param>
        /// <returns>The Hamming distance if it's less than or equal to maxHd, otherwise null.</returns>
        /// <exception cref="ArgumentException">Thrown if hashes are of different lengths.</exception>
        public static int? GetHamming(CustomBitArray hash1, CustomBitArray hash2, int? maxHd = null)
        {
            if (hash1.Length != hash2.Length)
            {
                throw new ArgumentException("Hashes must be of the same length.");
            }

            int actualMaxHd = maxHd ?? hash1.Length;
            int hd = 0;
            for (int i = 0; i < hash1.Length; i++)
            {
                if (hash1[i] != hash2[i])
                {
                    hd++;
                    if (hd > actualMaxHd)
                    {
                        return null;
                    }
                }
            }
            return hd;
        }

        /// <summary>
        /// Query index for candidates within pre-set hamming distance threshold (set at train()).
        /// This method returns matches as a generator (IEnumerable).
        /// </summary>
        /// <param name="h">Candidate hash (hex string).</param>
        /// <returns>An enumerable of tuples containing the hex string of the matching hash,
        /// a list of its categories, and its hamming distance to the query hash.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the index is not trained yet.</exception>
        /// <exception cref="ArgumentException">Thrown if the candidate hash 'h' has an invalid length.</exception>
        public IEnumerable<(string hexMatch, List<string> categories, int hammingDistance)> Query(string h)
        {
            if (!_trained)
            {
                throw new InvalidOperationException("Index not trained yet.");
            }

            CustomBitArray queryBitArray = CustomBitArray.FromHex(h, _hashLength);
            if (queryBitArray.Length != _hashLength)
            {
                throw new ArgumentException($"Invalid hash length encountered: {h}. Expected {_hashLength} bits, got {queryBitArray.Length} bits.", nameof(h));
            }

            HashSet<int> candidates = new HashSet<int>();
            int slot = 0;
            for (int i = 0; i < _hashLength; i += _wordLength)
            {
                // Clone the slice for recursion in GetWindow to avoid modifying the original queryBitArray
                CustomBitArray wordSlice = (CustomBitArray)queryBitArray.Slice(i, _wordLength).Clone();
                HashSet<string> window = GetWindow(wordSlice); // GetWindow modifies the 'wordSlice' instance

                foreach (string w in window)
                {
                    if (_words[slot].ContainsKey(w))
                    {
                        foreach (int x in _words[slot][w]) // x is an index into _index
                        {
                            if (!candidates.Contains(x))
                            {
                                // Retrieve the full hash from the index for hamming distance calculation
                                CustomBitArray indexedHash = _index[x].hash;
                                int? r = GetHamming(queryBitArray, indexedHash, _threshold);
                                if (r.HasValue)
                                {
                                    List<string> cats = new List<string>();
                                    foreach (int d in _index[x].categories)
                                    {
                                        cats.Add(_categories[d]);
                                    }
                                    yield return (indexedHash.ToHex(), cats, r.Value);
                                }
                                candidates.Add(x);
                            }
                        }
                    }
                }
                slot++;
            }
        }
    }
}