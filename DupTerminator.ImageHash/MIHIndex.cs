using System;
using System.Collections;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DupTerminator.ImageHash
{
    public class MIHIndex
    {
        private int _hashLength;
        private Dictionary<string, List<int>> _items;
        private List<(BitArray hash, List<int> categories)> _index;
        private List<string> _categories;
        private bool _trained;
        private Regex _pattern;
        private int _floor;
        private int _wordLength;
        private int _threshold;
        private List<Dictionary<string, HashSet<int>>> _words;

        public MIHIndex(int hashSize = 256)
        {
            if (hashSize % 8 != 0)
                throw new ArgumentException($"hashsize {hashSize} is not a multiple of 8");

            _hashLength = hashSize;
            _items = null;
            _index = null;
            _categories = new List<string>();
            _trained = false;
            _pattern = new Regex($"^[a-f0-9]{{{hashSize / 4}}}$", RegexOptions.IgnoreCase);
            _floor = 0;
            _wordLength = 0;
            _threshold = 0;
            _words = null;
        }

        private HashSet<string> GetWindow(BitArray word, int distance = 2, int position = 0)
        {
            var entries = new HashSet<string>();
            GetWindowRecursive(word, distance, position, entries);
            return entries;
        }

        private void GetWindowRecursive(BitArray word, int distance, int position, HashSet<string> entries)
        {
            if (position == word.Length)
            {
                entries.Add(BitArrayToHex(word));
                return;
            }

            if (distance > 0)
            {
                bool originalBit = word[position];
                // Flip to true (1)
                word[position] = true;
                int distOffset = originalBit ? 0 : -1;
                GetWindowRecursive(word, distance + distOffset, position + 1, entries);

                // Flip to false (0)
                word[position] = false;
                distOffset = !originalBit ? 0 : -1;
                GetWindowRecursive(word, distance + distOffset, position + 1, entries);

                // Restore original
                word[position] = originalBit;
            }
            else
            {
                GetWindowRecursive(word, distance, position + 1, entries);
            }
        }

        public void Update(IEnumerable<string> newItems, string category)
        {
            int offset = _categories.IndexOf(category);
            if (offset == -1)
            {
                _categories.Add(category);
                offset = _categories.Count - 1;
            }

            if (_items == null)
                _items = new Dictionary<string, List<int>>();

            foreach (var x in newItems)
            {
                if (!_pattern.IsMatch(x))
                    throw new ArgumentException($"Not a valid hex string: {x}");

                if (!_items.ContainsKey(x))
                {
                    _items[x] = new List<int> { offset };
                }
                else
                {
                    if (!_items[x].Contains(offset))
                        _items[x].Add(offset);
                }
            }

            _trained = false;
        }

        public void Update(string newItem, string category)
        {
            Update(new[] { newItem }, category);
        }

        public void Train(int wordLength = 16, int threshold = 32)
        {
            if (_trained)
                throw new InvalidOperationException("Index already trained. Rebuild object to train again");

            if (_items == null || _items.Count == 0)
                throw new InvalidOperationException("No items to train on");

            _index = new List<(BitArray, List<int>)>();

            foreach (var kvp in _items)
            {
                var bits = HexToBitArray(kvp.Key);
                if (bits.Length != _hashLength)
                    throw new ArgumentException($"Invalid hash length encountered: {kvp.Key}");

                _index.Add((bits, kvp.Value));
            }

            _items = null; // Free memory

            _floor = threshold / wordLength; // integer floor
            _wordLength = wordLength;
            _threshold = threshold;

            _words = new List<Dictionary<string, HashSet<int>>>();

            for (int i = 0; i < _hashLength; i += wordLength)
            {
                var wordDict = new Dictionary<string, HashSet<int>>();

                for (int c = 0; c < _index.Count; c++)
                {
                    var subBits = GetSubBitArray(_index[c].Item1, i, i + wordLength);
                    string hex = BitArrayToHex(subBits);

                    if (!wordDict.TryGetValue(hex, out var set))
                    {
                        set = new HashSet<int>();
                        wordDict[hex] = set;
                    }
                    set.Add(c);
                }

                // Freeze: convert to immutable set (simulate frozenset)
                foreach (var key in wordDict.Keys.ToList())
                {
                    wordDict[key] = new HashSet<int>(wordDict[key]); // already immutable via copy
                }

                _words.Add(wordDict);
            }

            _trained = true;
        }

        public IEnumerable<(string hash, List<string> categories, int hammingDistance)> Query(string hexHash)
        {
            if (!_trained)
                throw new InvalidOperationException("Index not trained yet");

            var queryBits = HexToBitArray(hexHash);
            if (queryBits.Length != _hashLength)
                throw new ArgumentException($"Invalid hash length encountered: {hexHash}");

            var candidates = new HashSet<int>();
            int slot = 0;

            for (int i = 0; i < _hashLength; i += _wordLength)
            {
                var word = GetSubBitArray(queryBits, i, i + _wordLength);
                var window = GetWindow(word, _floor);

                if (_words[slot] != null)
                {
                    foreach (var w in window)
                    {
                        if (_words[slot].TryGetValue(w, out var indices))
                        {
                            foreach (var idx in indices)
                            {
                                if (candidates.Contains(idx)) continue;

                                var (targetHash, cats) = _index[idx];
                                var hd = GetHammingDistance(queryBits, targetHash, _threshold);
                                if (hd.HasValue)
                                {
                                    var categoryNames = cats.Select(c => _categories[c]).ToList();
                                    yield return (BitArrayToHex(targetHash), categoryNames, hd.Value);
                                    candidates.Add(idx);
                                }
                            }
                        }
                    }
                }
                slot++;
            }
        }

        public static int? GetHammingDistance(BitArray a, BitArray b, int? maxHd = null)
        {
            if (a.Length != b.Length)
                throw new ArgumentException("BitArrays must be of equal length");

            int max = maxHd ?? a.Length;
            int hd = 0;

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    hd++;
                    if (hd > max)
                        return null;
                }
            }

            return hd;
        }

        // Helper: Convert hex string to BitArray
        private BitArray HexToBitArray(string hex)
        {
            int numBytes = hex.Length / 2;
            var bytes = new byte[numBytes];
            for (int i = 0; i < numBytes; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return new BitArray(bytes);
        }

        // Helper: Convert BitArray to hex string
        private string BitArrayToHex(BitArray bits)
        {
            byte[] bytes = BitArrayToByteArray(bits);
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }

        // Helper: Extract sub-bitarray [start, end)
        private BitArray GetSubBitArray(BitArray bits, int start, int length)
        {
            int len = length - start;
            var result = new bool[len];
            for (int i = 0; i < len; i++)
            {
                result[i] = bits[start + i];
            }
            return new BitArray(result);
        }

        // Helper: Convert BitArray to byte array (MSB first in each byte)
        private byte[] BitArrayToByteArray(BitArray bits)
        {
            int byteCount = (bits.Length + 7) / 8;
            var result = new byte[byteCount];

            for (int i = 0; i < bits.Length; i++)
            {
                if (bits[i])
                {
                    result[i / 8] |= (byte)(1 << (7 - (i % 8))); // Big-endian bit order (like Python)
                }
            }

            return result;
        }
    }
}


//---

//### 🔧 Usage Example (C#)

//```csharp
//var mih = new MIHIndex(hashSize: 256);

//    mih.Update("a1b2c3d4e5f6" + "00000000000000000000000000000000000000000000000000000000", "ignorable");
//mih.Update("deadbeefcafe" + "00000000000000000000000000000000000000000000000000000000", "malicious");

//mih.Train(wordLength: 16, threshold: 32);

//var results = mih.Query("a1b2c3d4e5f60000000000000000000000000000000000000000000000000000");

//foreach (var (hash, cats, dist) in results)
//{
//    Console.WriteLine($"Match: {hash}, Categories: [{string.Join(", ", cats)}], HD: {dist}");
//}
//```

//---

//### 📝 Notes

//-**Bit Order * *: Python’s `bitarray` uses big-endian bit order within bytes. The `BitArrayToByteArray` method mimics this.
//- **Performance**: For large-scale use, consider replacing `BitArray` with `ulong[]` or `Span<byte>` and using SIMD/ intrinsics for Hamming distance.
//-**Immutability * *: The `_words` structure uses `HashSet<int>` to simulate `frozenset`.
//-**Memory * *: After training, `_items` is cleared to save memory.

