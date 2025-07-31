using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using DupTerminator.BusinessLogic.Abstraction;

namespace DupTerminator.ImageHash
{
    public sealed class MIHIndexSeek : IDisposable
    {
        private readonly int _hashLength;
        private readonly Regex _pattern;
        private bool _trained = false;
        private List<string> _categories = new List<string>();
        private Dictionary<string, List<int>>? _items;
        private List<(byte[] Hash, List<int> CategoryIndices)>? _index;
        private List<Dictionary<string, HashSet<int>>>? _words;
        private int _floor;
        private int _wordLength;
        private int _threshold;

        // For SIMD operations
        private readonly int _vectorSize;
        private bool _disposed = false;

        public MIHIndexSeek(int hashSize = 256)
        {
            if (hashSize % 8 != 0)
                throw new ArgumentException($"hashsize {hashSize} is not a multiple of 8", nameof(hashSize));

            _hashLength = hashSize;
            _pattern = new Regex($"^[a-f0-9]{{{hashSize / 4}}}$", RegexOptions.IgnoreCase);
            _vectorSize = Vector<byte>.Count;
        }

        public void Update(IEnumerable<string> newHashes, string category)
        {
            ThrowIfDisposed();

            if (_trained)
                throw new InvalidOperationException("Cannot update after training");

            int categoryIndex = _categories.IndexOf(category);
            if (categoryIndex == -1)
            {
                _categories.Add(category);
                categoryIndex = _categories.Count - 1;
            }

            _items ??= new Dictionary<string, List<int>>();

            foreach (string hash in newHashes)
            {
                if (!_pattern.IsMatch(hash))
                    throw new ArgumentException($"Not a valid hex string: {hash}", nameof(newHashes));

                if (!_items.TryGetValue(hash, out var indices))
                {
                    _items[hash] = new List<int> { categoryIndex };
                }
                else if (!indices.Contains(categoryIndex))
                {
                    indices.Add(categoryIndex);
                }
            }
        }

        public void Train(int wordLength = 16, int threshold = 32)
        {
            ThrowIfDisposed();

            if (_trained)
                throw new InvalidOperationException("Index already trained");

            if (_items == null || _items.Count == 0)
                throw new InvalidOperationException("No items to train");

            _index = new List<(byte[], List<int>)>();
            foreach (var item in _items)
            {
                byte[] hashBytes = HexStringToByteArray(item.Key);
                if (hashBytes.Length * 8 != _hashLength)
                    throw new InvalidOperationException($"Invalid hash length encountered: {item.Key}");

                _index.Add((hashBytes, item.Value));
            }

            _items = null; // Free memory

            _floor = threshold / wordLength;
            _wordLength = wordLength;
            _threshold = threshold;

            int numWords = _hashLength / wordLength;
            _words = new List<Dictionary<string, HashSet<int>>>(numWords);

            for (int i = 0; i < numWords; i++)
            {
                var wordDict = new Dictionary<string, HashSet<int>>();
                int startBit = i * wordLength;

                for (int j = 0; j < _index.Count; j++)
                {
                    ReadOnlySpan<byte> hashSpan = _index[j].Hash;
                    string wordHex = ExtractWordHex(hashSpan, startBit, wordLength);

                    if (!wordDict.TryGetValue(wordHex, out var indices))
                    {
                        wordDict[wordHex] = new HashSet<int> { j };
                    }
                    else
                    {
                        indices.Add(j);
                    }
                }

                _words.Add(wordDict);
            }

            _trained = true;
        }

        public IEnumerable<(string Hash, List<string> Categories, int HammingDistance)> Query(string hash)
        {
            ThrowIfDisposed();

            if (!_trained)
                throw new InvalidOperationException("Index not trained yet");

            byte[] queryBytes = HexStringToByteArray(hash);
            if (queryBytes.Length * 8 != _hashLength)
                throw new ArgumentException($"Invalid hash length encountered: {hash}", nameof(hash));

            HashSet<int> candidates = new HashSet<int>();
            int slot = 0;

            for (int i = 0; i < _hashLength; i += _wordLength)
            {
                ReadOnlySpan<byte> querySpan = queryBytes;
                HashSet<string> window = GetWindow(querySpan.Slice(i / 8, (_wordLength + 7) / 8), i % 8, _wordLength);

                foreach (string w in window)
                {
                    if (_words![slot].TryGetValue(w, out var indices))
                    {
                        foreach (int index in indices)
                        {
                            if (candidates.Add(index))
                            {
                                int hamming = GetHammingDistanceSIMD(queryBytes, _index![index].Hash, _threshold);
                                if (hamming <= _threshold)
                                {
                                    List<string> cats = _index[index].CategoryIndices
                                        .Select(catIndex => _categories[catIndex])
                                        .ToList();

                                    yield return (ByteArrayToHexString(_index[index].Hash), cats, hamming);
                                }
                            }
                        }
                    }
                }

                slot++;
            }
        }

        private HashSet<string> GetWindow(ReadOnlySpan<byte> bytes, int startBit, int length, int distance = 2, int position = 0, HashSet<string>? entries = null)
        {
            entries ??= new HashSet<string>();

            if (position == length)
            {
                entries.Add(ExtractWordHex(bytes, startBit, length));
                return entries;
            }

            if (distance > 0)
            {
                bool originalBit = GetBit(bytes, startBit + position);

                for (int i = 0; i <= 1; i++)
                {
                    bool newBit = i == 1;
                    SetBit(ref MemoryMarshal.GetReference(bytes), startBit + position, newBit);

                    int distOffset = originalBit != newBit ? -1 : 0;
                    GetWindow(bytes, startBit, length, distance + distOffset, position + 1, entries);
                }

                SetBit(ref MemoryMarshal.GetReference(bytes), startBit + position, originalBit);
            }
            else
            {
                GetWindow(bytes, startBit, length, distance, position + 1, entries);
            }

            return entries;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool GetBit(ReadOnlySpan<byte> bytes, int bitIndex)
        {
            int byteIndex = bitIndex / 8;
            int bitOffset = bitIndex % 8;
            return (bytes[byteIndex] & (1 << (7 - bitOffset))) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetBit(ref byte byteRef, int bitIndex, bool value)
        {
            int byteIndex = bitIndex / 8;
            int bitOffset = bitIndex % 8;
            byte mask = (byte)(1 << (7 - bitOffset));

            if (value)
                Unsafe.Add(ref byteRef, byteIndex) |= mask;
            else
                Unsafe.Add(ref byteRef, byteIndex) &= (byte)~mask;
        }

        private string ExtractWordHex(ReadOnlySpan<byte> hash, int startBit, int length)
        {
            int byteLength = (length + 7) / 8;
            byte[] wordBytes = new byte[byteLength];
            Span<byte> wordSpan = wordBytes;

            for (int i = 0; i < length; i++)
            {
                if (GetBit(hash, startBit + i))
                {
                    int targetByte = i / 8;
                    int targetBit = i % 8;
                    wordSpan[targetByte] |= (byte)(1 << (7 - targetBit));
                }
            }

            return Convert.ToHexString(wordSpan).ToLowerInvariant();
        }

        private int GetHammingDistanceSIMD(byte[] hash1, byte[] hash2, int maxDistance)
        {
            ReadOnlySpan<byte> span1 = hash1;
            ReadOnlySpan<byte> span2 = hash2;

            int distance = 0;
            int i = 0;

            // Process with SIMD vectors
            if (Vector.IsHardwareAccelerated)
            {
                for (; i <= hash1.Length - _vectorSize; i += _vectorSize)
                {
                    var vec1 = new Vector<byte>(span1.Slice(i));
                    var vec2 = new Vector<byte>(span2.Slice(i));

                    Vector<byte> xorResult = Vector.Xor(vec1, vec2);

                    // Count bits using SWAR method
                    for (int j = 0; j < _vectorSize; j++)
                    {
                        byte diff = xorResult[j];
                        distance += BitOperations.PopCount(diff);

                        if (distance > maxDistance)
                            return int.MaxValue;
                    }
                }
            }

            // Process remaining bytes
            for (; i < hash1.Length; i++)
            {
                byte diff = (byte)(span1[i] ^ span2[i]);
                distance += BitOperations.PopCount(diff);

                if (distance > maxDistance)
                    return int.MaxValue;
            }

            return distance;
        }

        private static byte[] HexStringToByteArray(string hex)
        {
            int length = hex.Length;
            byte[] bytes = new byte[length / 2];

            for (int i = 0; i < length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }

            return bytes;
        }

        private static string ByteArrayToHexString(byte[] bytes)
        {
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _items?.Clear();
                _index?.Clear();
                _words?.Clear();
                _categories.Clear();

                _disposed = true;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(MIHIndexSeek));
        }
    }
}
