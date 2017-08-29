namespace NServiceBus.Persistence.AzureStorage.SecondaryIndices
{
    using System.Collections.Generic;

    /// <summary>
    /// This is a simple implementation of LRU cache.
    /// </summary>
    /// <remarks>
    /// The LRUCache uses two fields in every operation:
    /// <see cref="lru" /> - to provide last-recently-used behavior
    /// <see cref="items" /> - to provide key-value mapping with O(1)
    /// Locking everywhere ensures that these two parts can be updated atomically.
    /// ConcurrentDictionary does not ensure lru behavior as it cannot remove the last item that was used,
    /// hence a custom implementation for a thread safe LRU has been introduced.
    /// </remarks>
    class LRUCache<TKey, TValue>
    {
        public LRUCache(int capacity)
        {
            this.capacity = capacity;
        }

        public bool TryGet(TKey key, out TValue value)
        {
            lock (@lock)
            {
                if (items.TryGetValue(key, out var node))
                {
                    lru.Remove(node);
                    lru.AddLast(node);
                    value = node.Value.Value;
                    return true;
                }

                value = default(TValue);
                return false;
            }
        }

        public void Put(TKey key, TValue value)
        {
            lock (@lock)
            {
                if (items.TryGetValue(key, out var node) == false)
                {
                    node = new LinkedListNode<Item>(
                        new Item
                        {
                            Key = key,
                            Value = value
                        });
                    items.Add(key, node);

                    TrimOneIfNeeded();
                }
                else
                {
                    // just update the value
                    node.Value.Value = value;
                    lru.Remove(node);
                }

                lru.AddLast(node);
            }
        }

        public void Remove(TKey key)
        {
            lock (@lock)
            {
                if (items.TryGetValue(key, out var node))
                {
                    lru.Remove(node);
                    items.Remove(key);
                }
            }
        }

        void TrimOneIfNeeded()
        {
            if (items.Count > 0 && items.Count > capacity)
            {
                var node = lru.First;
                lru.Remove(node);
                items.Remove(node.Value.Key);
            }
        }

        LinkedList<Item> lru = new LinkedList<Item>();
        Dictionary<TKey, LinkedListNode<Item>> items = new Dictionary<TKey, LinkedListNode<Item>>();

        int capacity;
        object @lock = new object();

        class Item
        {
            public TKey Key;
            public TValue Value;
        }
    }
}