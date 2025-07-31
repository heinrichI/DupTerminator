using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DupTerminator.WPF.Service
{
    /// <summary>
    /// Simple LRU cache (no dependencies needed).  It silently discards
    /// the image if we already hold more than a preset capacity.
    /// </summary>
    public sealed class LruCache<TKey, TValue>
    {
        private readonly int _capacity;
        private readonly LinkedList<(TKey Key, TValue Value)> _items = new();
        private readonly Dictionary<TKey, LinkedListNode<(TKey Key, TValue Value)>> _dict = new();

        public LruCache(int capacity) => _capacity = capacity;

        public bool TryGet(TKey key, out TValue? value)
        {
            if (_dict.TryGetValue(key, out var node))
            {
                _items.Remove(node);
                _items.AddFirst(node);
                value = node.Value.Value;
                return true;
            }
            value = default;
            return false;
        }

        public void Add(TKey key, TValue value)
        {
            if (_dict.TryGetValue(key, out var node))
            {
                _items.Remove(node);
                _items.AddFirst(node);
                node.Value = (key, value);
            }
            else
            {
                var newNode = _items.AddFirst((key, value));
                _dict[key] = newNode;
                if (_items.Count > _capacity)
                {
                    var last = _items.Last!;
                    _items.RemoveLast();
                    _dict.Remove(last.Value.Key);
                }
            }
        }
    }
}
