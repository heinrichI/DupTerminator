using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DupTerminator.BusinessLogic.Abstraction;
using DupTerminator.BusinessLogic.Model;

namespace DupTerminator.ImageHash
{
    public sealed class MIHMy : IMIH
    {
        private readonly int _hashLength;
        private bool _trained = false;
        private List<PHashFileInfo> _fileInfos = new List<PHashFileInfo>();
        private Dictionary<ulong, List<int>>? _items;
        private List<(ulong Hash, List<int> FileInfoIndices)>? _index;
        private List<Dictionary<ulong, HashSet<int>>>? _words;
        private int _floor;
        private int _wordLength;
        private int _threshold;

        // For SIMD operations
        private readonly int _vectorSize;
        private bool _disposed = false;

        public MIHMy(int hashSize = 256)
        {
            if (hashSize % 8 != 0)
                throw new ArgumentException($"hashsize {hashSize} is not a multiple of 8", nameof(hashSize));

            _hashLength = hashSize;
            _vectorSize = Vector<byte>.Count;
        }

        public void Update(IDictionary<ulong, IList<PHashFileInfo>> newHashes)
        {
            ThrowIfDisposed();

            if (_trained)
                throw new InvalidOperationException("Cannot update after training");

            _items ??= new Dictionary<ulong, List<int>>();

            foreach (var kvp in newHashes)
            {
                ulong hash = kvp.Key;
                var fileInfos = kvp.Value;

                foreach (var fileInfo in fileInfos)
                {
                    int fileInfoIndex = _fileInfos.IndexOf(fileInfo);
                    if (fileInfoIndex == -1)
                    {
                        _fileInfos.Add(fileInfo);
                        fileInfoIndex = _fileInfos.Count - 1;
                    }

                    if (!_items.TryGetValue(hash, out var indices))
                    {
                        _items[hash] = new List<int> { fileInfoIndex };
                    }
                    else if (!indices.Contains(fileInfoIndex))
                    {
                        indices.Add(fileInfoIndex);
                    }
                }
            }
        }

        public void Train(int wordLength = 16, int threshold = 7)
        {
            ThrowIfDisposed();

            if (_trained)
                throw new InvalidOperationException("Index already trained");

            if (_items == null || _items.Count == 0)
                throw new InvalidOperationException("No items to train");

            _index = new List<(ulong, List<int>)>();
            foreach (var item in _items)
            {
                _index.Add((item.Key, item.Value));
            }

            _items = null; // Free memory

            _floor = threshold / wordLength;
            _wordLength = wordLength;
            _threshold = threshold;

            int numWords = _hashLength / wordLength;
            _words = new List<Dictionary<ulong, HashSet<int>>>(numWords);

            for (int i = 0; i < numWords; i++)
            {
                var wordDict = new Dictionary<ulong, HashSet<int>>();
                int startBit = i * wordLength;

                for (int j = 0; j < _index.Count; j++)
                {
                    ulong hash = _index[j].Hash;
                    ulong word = ExtractWord(hash, startBit, wordLength);

                    if (!wordDict.TryGetValue(word, out var indices))
                    {
                        wordDict[word] = new HashSet<int> { j };
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

        public IEnumerable<(ulong Hash, List<PHashFileInfo> FileInfos, int HammingDistance)> Query(ulong hash)
        {
            ThrowIfDisposed();

            if (!_trained)
                throw new InvalidOperationException("Index not trained yet");

            HashSet<int> candidates = new HashSet<int>();
            int slot = 0;

            for (int i = 0; i < _hashLength; i += _wordLength)
            {
                HashSet<ulong> window = GetWindow(hash, i, _wordLength);

                foreach (ulong w in window)
                {
                    if (_words![slot].TryGetValue(w, out var indices))
                    {
                        foreach (int index in indices)
                        {
                            if (candidates.Add(index))
                            {
                                int hamming = GetHammingDistance(hash, _index![index].Hash, _threshold);
                                if (hamming <= _threshold)
                                {
                                    List<PHashFileInfo> fileInfos = _index[index].FileInfoIndices
                                        .Select(fileInfoIndex => _fileInfos[fileInfoIndex])
                                        .ToList();

                                    yield return (_index[index].Hash, fileInfos, hamming);
                                }
                            }
                        }
                    }
                }

                slot++;
            }
        }

        private HashSet<ulong> GetWindow(ulong hash, int startBit, int length, int distance = 2, int position = 0, HashSet<ulong>? entries = null)
        {
            entries ??= new HashSet<ulong>();

            if (position == length)
            {
                entries.Add(ExtractWord(hash, startBit, length));
                return entries;
            }

            if (distance > 0)
            {
                bool originalBit = GetBit(hash, startBit + position);

                for (int i = 0; i <= 1; i++)
                {
                    bool newBit = i == 1;
                    ulong modifiedHash = SetBit(hash, startBit + position, newBit);

                    int distOffset = originalBit != newBit ? -1 : 0;
                    GetWindow(modifiedHash, startBit, length, distance + distOffset, position + 1, entries);
                }
            }
            else
            {
                GetWindow(hash, startBit, length, distance, position + 1, entries);
            }

            return entries;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool GetBit(ulong hash, int bitIndex)
        {
            return (hash & (1UL << (63 - bitIndex))) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong SetBit(ulong hash, int bitIndex, bool value)
        {
            ulong mask = 1UL << (63 - bitIndex);
            return value ? (hash | mask) : (hash & ~mask);
        }

        private ulong ExtractWord(ulong hash, int startBit, int length)
        {
            ulong word = 0;
            for (int i = 0; i < length; i++)
            {
                if (GetBit(hash, startBit + i))
                {
                    word |= 1UL << (63 - i);
                }
            }
            return word;
        }

        private int GetHammingDistance(ulong hash1, ulong hash2, int maxDistance)
        {
            ulong xorResult = hash1 ^ hash2;
            int distance = BitOperations.PopCount(xorResult);

            return distance <= maxDistance ? distance : int.MaxValue;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _items?.Clear();
                _index?.Clear();
                _words?.Clear();
                _fileInfos.Clear();

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
