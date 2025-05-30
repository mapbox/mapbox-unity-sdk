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
        public readonly int CacheSize;
        private Dictionary<CanonicalTileId, T> _fallbackDatas;
        private Dictionary<CanonicalTileId, LinkedListNode<T>> _cacheHash;
        private LinkedList<T> _cache;
        private Thread mainThread;
        private HashSet<CanonicalTileId> _previousFrameTiles;
        
        public TypeMemoryCache(int cacheSize = 100)
        {
            mainThread = System.Threading.Thread.CurrentThread;
            CacheSize = cacheSize;
            _cacheHash = new Dictionary<CanonicalTileId, LinkedListNode<T>>();
            _cache = new LinkedList<T>();
            // _datas = new Dictionary<CanonicalTileId, T>();
            // _trackedDatas = new Queue<CanonicalTileId>();
            _fallbackDatas = new Dictionary<CanonicalTileId, T>();
        }
			
        public void Add(T data)
        {
            if (_cacheHash == null)
            {
                Debug.LogWarning("Function called after TypeMemoryCache destroyed");
                return;
            }
            if (_cacheHash.TryGetValue(data.TileId, out var node))
            {
                _cache.Remove(node);
                _cache.AddFirst(node);
            }
            else
            {
                Prune();
                
                var llNode = _cache.AddFirst(data);
                _cacheHash.Add(data.TileId, llNode);
            }
        }

        private void Prune()
        {
            if (_cache.Count > CacheSize)
            {
                for (int i = 0; i < 20; i++)
                {
                    var lastItem = _cache.Last;
                    if (!_previousFrameTiles.Contains(lastItem.Value.TileId))
                    {
                        DropItem(_cache.Last);
                        break;
                    }
                    else
                    {
                        _cache.RemoveLast();
                        _cache.AddFirst(lastItem);
                    }
                }
            }
        }

        private void DropItem(LinkedListNode<T> node)
        {
            _cache.Remove(node);
            var disposedTileId = node.Value.TileId;
            _cacheHash.Remove(disposedTileId);
            node.Value.Dispose();
            CacheItemDisposed(disposedTileId);
        }

        public bool Exists(CanonicalTileId tileId)
        {
            if (_cacheHash == null)
            {
                Debug.LogWarning("Function called after TypeMemoryCache destroyed");
                return false;
            }
            return _cacheHash.ContainsKey(tileId) || _fallbackDatas.ContainsKey(tileId);
            //return _datas.ContainsKey(tileId) || _fallbackDatas.ContainsKey(tileId);
        }

        public bool Get(CanonicalTileId tileId, out T outData)
        {
            outData = null;
            if (_cacheHash == null)
            {
                Debug.LogWarning("Function called after TypeMemoryCache destroyed");
                return false;
            }

            if (_cacheHash.TryGetValue(tileId, out var linkedNode))
            {
                outData = linkedNode.Value;
                if (mainThread.Equals(System.Threading.Thread.CurrentThread))
                {
                    _cache.Remove(linkedNode);
                    _cache.AddFirst(linkedNode);
                }

                return true;
            }

            if (_fallbackDatas.TryGetValue(tileId, out var data))
            {
                outData = data;
                return true;
            }
            
            return false;
        }

        public IEnumerable<T> GetAllDatas()
        {
            if (_cacheHash == null)
            {
                Debug.LogWarning("Function called after TypeMemoryCache destroyed");
                return new List<T>();
            }
            return _cacheHash.Values.Select(x => x.Value);
        }

        public void Remove(CanonicalTileId tileId)
        {
            if (_cacheHash == null)
            {
                Debug.LogWarning("Function called after TypeMemoryCache destroyed");
                return;
            }
            if (_cacheHash.TryGetValue(tileId, out var linkedListNode))
            {
                DropItem(linkedListNode);
            }
        }

        
        public void RetainTiles(HashSet<CanonicalTileId> retainedTiles)
        {
            _previousFrameTiles = retainedTiles;
        }

        public void MarkFallback(CanonicalTileId dataTileId)
        {
            if (_cacheHash == null)
            {
                Debug.LogWarning("Function called after TypeMemoryCache destroyed");
                return;
            }
            if (_cacheHash.TryGetValue(dataTileId, out var data))
            {
                _cacheHash.Remove(dataTileId);
                _cache.Remove(data);
                _fallbackDatas.Add(dataTileId, data.Value);
            }
        }

        public Action<CanonicalTileId> CacheItemDisposed = (t) => { };

        public void OnDestroy()
        {
            foreach (var tileData in _cacheHash.Values)
            {
                tileData.Value.Dispose();
            }
            _cacheHash.Clear();
            _cacheHash = null;
            foreach (var fallbackData in _fallbackDatas.Values)
            {
                fallbackData.Dispose();
            }
            _fallbackDatas.Clear();
            _fallbackDatas = null;
            _cache.Clear();
            _cache = null;
        }
    }
    
    public interface ITypeCache
    {
        void OnDestroy();
    }
}
