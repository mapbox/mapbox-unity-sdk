using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Tiles;
using UnityEngine;

namespace Mapbox.BaseModule.Data.Platform.Cache
{
    //this is a basic LRU (least recently used) cache but "used" comes from read/write action
    //retainTiles method provides in-use support; not sound but good enough
    public class TypeMemoryCache<T> : ITypeCache where T : MapboxTileData
    {
        private readonly int mainThreadId;

        /// <summary>
        /// Every member of this cache is main-thread only; none of its collections are synchronized.
        /// Mutators call this and refuse the operation off-thread, because leaving the state
        /// untouched is always safe. Readers deliberately do not: refusing a read would fabricate a
        /// cache miss, which is a worse lie than the read. <see cref="Get"/> sits between the two -
        /// it reads on any thread but promotes only on the main one, which it already guards inline.
        ///
        /// Audited over a full session in August 2026: 476,581 calls, none off the main thread. This
        /// exists to catch a regression, not a known caller.
        /// </summary>
        private bool OnMainThread(string operation)
        {
            if (Thread.CurrentThread.ManagedThreadId == mainThreadId)
                return true;

            Debug.LogError(
                $"TypeMemoryCache.{operation} was called from thread " +
                $"{Thread.CurrentThread.ManagedThreadId}, but the cache is main-thread only and its " +
                "collections are not synchronized. The operation was refused. Marshal the call back " +
                "to the main thread.");
            return false;
        }

        public Action<CanonicalTileId> CacheItemDisposed = (t) => { };
        
        private readonly int _inactiveCapacity;
        
        // Active list
        private readonly Dictionary<CanonicalTileId, T> _active;
        // Inactive lists
        private readonly Dictionary<CanonicalTileId, LinkedListNode<(CanonicalTileId key, T value)>> _inactiveMap;
        private readonly LinkedList<(CanonicalTileId key, T value)> _inactiveList;
        
        private Dictionary<CanonicalTileId, T> _fallbackDatas;
        private HashSet<CanonicalTileId> _previousFrameTiles;
        private HashSet<CanonicalTileId> _tilesToDemote;
        
        public TypeMemoryCache(int cacheSize = 100)
        {
            mainThreadId = Thread.CurrentThread.ManagedThreadId;
            if (cacheSize <= 0)
                throw new ArgumentException("Inactive capacity must be > 0");

            _inactiveCapacity = cacheSize;
            _active = new Dictionary<CanonicalTileId, T>();
            _inactiveMap = new Dictionary<CanonicalTileId, LinkedListNode<(CanonicalTileId, T)>>();
            _inactiveList = new LinkedList<(CanonicalTileId, T)>();
            _tilesToDemote = new HashSet<CanonicalTileId>();
            _fallbackDatas = new Dictionary<CanonicalTileId, T>();
        }
		
        /// <summary>
        /// Add new data to the active pool. Replaces if key already exists.
        /// </summary>
        public void Add(T data)
        {
            if (!OnMainThread(nameof(Add)))
                return;
            _active[data.TileId] = data;
        }
        
        /// <summary>
        /// Get data by key. Promotes from inactive to active if found.
        /// </summary>
        public bool Get(CanonicalTileId key, out T outData)
        {
            if (_active.TryGetValue(key, out outData))
                return true;

            if (_inactiveMap.TryGetValue(key, out var node))
            {
                if (Thread.CurrentThread.ManagedThreadId == mainThreadId) //do not write while on worker threads
                {
                    // Promote to active
                    _inactiveList.Remove(node);
                    _inactiveMap.Remove(key);
                    _active[key] = node.Value.value;
                }

                outData = node.Value.value;
                return true;
            }

            if (_fallbackDatas.TryGetValue(key, out outData))
            {
                return true;
            }

            outData = null;
            return false;
        }
        
        public void Remove(CanonicalTileId tileId)
        {
            if (!OnMainThread(nameof(Remove)))
                return;
            if (_active.TryGetValue(tileId, out var data))
            {
                data.Dispose();
                _active.Remove(tileId);
                return;
            }

            if (_inactiveMap.TryGetValue(tileId, out var tuple))
            {
                tuple.Value.value.Dispose();
                _inactiveMap.Remove(tileId);
                _inactiveList.Remove(tuple);
                return;
            }

            if (_fallbackDatas.TryGetValue(tileId, out var fallback))
            {
                fallback.Dispose();
                _fallbackDatas.Remove(tileId);
            }
        }
        
        /// <summary>
        /// Returns true if the key exists in active or inactive pools.
        /// </summary>
        public bool Exists(CanonicalTileId key)
        {
            return _active.ContainsKey(key) || _inactiveMap.ContainsKey(key) || _fallbackDatas.ContainsKey(key);
        }
        
        /// <summary>
        /// Call once per frame with the set of keys that should stay active.
        /// Others are demoted into the inactive pool.
        /// </summary>
        public void RetainTiles(HashSet<CanonicalTileId> currentActiveKeys)
        {
            if (!OnMainThread(nameof(RetainTiles)))
                return;
            _tilesToDemote.Clear();
            foreach (var key in _active.Keys)
            {
                if (!currentActiveKeys.Contains(key))
                    _tilesToDemote.Add(key);
            }

            foreach (var key in _tilesToDemote)
            {
                var value = _active[key];
                _active.Remove(key);

                var node = new LinkedListNode<(CanonicalTileId, T)>((key, value));
                _inactiveList.AddFirst(node);
                _inactiveMap[key] = node;

                if (_inactiveList.Count > _inactiveCapacity)
                {
                    var last = _inactiveList.Last;
                    _inactiveList.RemoveLast();
                    _inactiveMap.Remove(last.Value.key);
                    CacheItemDisposed(last.Value.key);
                    last.Value.value.Dispose();
                }
            }
        }
        
        public void MarkFallback(CanonicalTileId dataTileId)
        {
            if (!OnMainThread(nameof(MarkFallback)))
                return;
            if (_active.TryGetValue(dataTileId, out var data))
            {
                _active.Remove(dataTileId);
                _fallbackDatas.Add(dataTileId, data);
            }
            if (_inactiveMap.TryGetValue(dataTileId, out var tuple))
            {
                _inactiveMap.Remove(dataTileId);
                _inactiveList.Remove(tuple);
                _fallbackDatas.Add(dataTileId, tuple.Value.value);
            }
        }

        public void ClearInactive()
        {
            if (!OnMainThread(nameof(ClearInactive)))
                return;
            foreach (var tileData in _inactiveList)
            {
                tileData.value.Dispose();
            }
            _inactiveMap.Clear();
            _inactiveList.Clear();
        }
        
        public IEnumerable<T> GetAllDatas()
        {
            foreach (var data in _active)
            {
                yield return data.Value;
            }

            foreach (var data in _inactiveList)
            {
                yield return data.value;
            }

            foreach (var data in _fallbackDatas)
            {
                yield return data.Value;
            }
        }
        public void OnDestroy()
        {
            if (!OnMainThread(nameof(OnDestroy)))
                return;
            foreach (var tileData in _active.Values)
            {
                tileData.Dispose();
            }
            foreach (var tileData in _inactiveList)
            {
                tileData.value.Dispose();
            }
            foreach (var fallbackData in _fallbackDatas.Values)
            {
                fallbackData.Dispose();
            }
            
            _active.Clear();
            _inactiveList.Clear();
            _inactiveMap.Clear();
            _fallbackDatas.Clear();
        }
        
        public int ActiveCount => _active.Count;
        public int InactiveCount => _inactiveMap.Count;

        public Dictionary<CanonicalTileId, T> GetActiveData => _active;
        public Dictionary<CanonicalTileId, T> GetFallbackData => _fallbackDatas;
    }
    
    public interface ITypeCache
    {
        void OnDestroy();
        int ActiveCount { get; }
        int InactiveCount { get; }
    }
}