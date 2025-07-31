using System.Collections.Generic;
using UnityEngine;

namespace RedAlert.Core
{
    /// <summary>
    /// Advanced object pooling system for Red Alert RTS.
    /// Provides efficient object reuse for projectiles, effects, UI elements, and units.
    /// Includes automatic cleanup and memory management for WebGL optimization.
    /// </summary>
    public class PoolService : MonoBehaviour, UpdateDriver.ISlowTick
    {
        [Header("Pool Settings")]
        [SerializeField] private int _defaultPoolSize = 50;
        [SerializeField] private int _maxPoolSize = 200;
        [SerializeField] private float _cleanupInterval = 30f;
        [SerializeField] private bool _autoExpand = true;
        
        private readonly Dictionary<string, ObjectPool> _pools = new Dictionary<string, ObjectPool>();
        private readonly Dictionary<GameObject, string> _activeObjects = new Dictionary<GameObject, string>();
        private float _lastCleanupTime;
        
        private static PoolService _instance;
        public static PoolService Instance => _instance;
        
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void OnEnable()
        {
            UpdateDriver.Register(this);
        }
        
        private void OnDisable()
        {
            UpdateDriver.Unregister(this);
        }
        
        public void SlowTick()
        {
            if (Time.time - _lastCleanupTime >= _cleanupInterval)
            {
                CleanupPools();
                _lastCleanupTime = Time.time;
            }
        }
        
        // Pool management API
        public void RegisterPool(string poolId, GameObject prefab, int initialSize = -1)
        {
            if (_pools.ContainsKey(poolId))
            {
                Debug.LogWarning($"[PoolService] Pool '{poolId}' already exists.");
                return;
            }
            
            int size = initialSize > 0 ? initialSize : _defaultPoolSize;
            var pool = new ObjectPool(poolId, prefab, size, _maxPoolSize, _autoExpand);
            _pools[poolId] = pool;
            
            // Pre-populate pool
            pool.PrePopulate(transform);
            
            Debug.Log($"[PoolService] Registered pool '{poolId}' with {size} objects.");
        }
        
        public GameObject Get(string poolId, Vector3 position = default, Quaternion rotation = default)
        {
            if (!_pools.TryGetValue(poolId, out var pool))
            {
                Debug.LogError($"[PoolService] Pool '{poolId}' not found.");
                return null;
            }
            
            var obj = pool.Get(position, rotation);
            if (obj != null)
            {
                _activeObjects[obj] = poolId;
            }
            
            return obj;
        }
        
        public void Release(GameObject obj)
        {
            if (obj == null) return;
            
            if (_activeObjects.TryGetValue(obj, out var poolId))
            {
                if (_pools.TryGetValue(poolId, out var pool))
                {
                    pool.Release(obj);
                    _activeObjects.Remove(obj);
                }
            }
            else
            {
                Debug.LogWarning($"[PoolService] Object '{obj.name}' not found in active objects. Destroying.");
                Destroy(obj);
            }
        }
        
        public void ReleaseAll(string poolId)
        {
            if (!_pools.TryGetValue(poolId, out var pool)) return;
            
            var objectsToRelease = new List<GameObject>();
            foreach (var kvp in _activeObjects)
            {
                if (kvp.Value == poolId)
                {
                    objectsToRelease.Add(kvp.Key);
                }
            }
            
            foreach (var obj in objectsToRelease)
            {
                Release(obj);
            }
        }
        
        public int GetActiveCount(string poolId)
        {
            int count = 0;
            foreach (var kvp in _activeObjects)
            {
                if (kvp.Value == poolId) count++;
            }
            return count;
        }
        
        public int GetPoolSize(string poolId)
        {
            return _pools.TryGetValue(poolId, out var pool) ? pool.TotalCount : 0;
        }
        
        public void PrewarmPool(string poolId, int count)
        {
            if (_pools.TryGetValue(poolId, out var pool))
            {
                pool.Prewarm(count, transform);
            }
        }
        
        private void CleanupPools()
        {
            foreach (var pool in _pools.Values)
            {
                pool.Cleanup();
            }
            
            // Clean up destroyed active objects
            var keysToRemove = new List<GameObject>();
            foreach (var kvp in _activeObjects)
            {
                if (kvp.Key == null)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            
            foreach (var key in keysToRemove)
            {
                _activeObjects.Remove(key);
            }
        }
        
        // Convenience methods for common object types
        public GameObject GetProjectile(string projectileType, Vector3 position, Quaternion rotation)
        {
            return Get($"Projectile_{projectileType}", position, rotation);
        }
        
        public GameObject GetEffect(string effectType, Vector3 position, Quaternion rotation = default)
        {
            return Get($"Effect_{effectType}", position, rotation);
        }
        
        public GameObject GetUIElement(string uiType)
        {
            return Get($"UI_{uiType}");
        }
        
        public void ReleaseProjectile(GameObject projectile)
        {
            Release(projectile);
        }
        
        public void ReleaseEffect(GameObject effect)
        {
            Release(effect);
        }
        
        public void ReleaseUIElement(GameObject uiElement)
        {
            Release(uiElement);
        }
        
        // Statistics
        public PoolStats GetPoolStats()
        {
            int totalActive = _activeObjects.Count;
            int totalPooled = 0;
            int totalPools = _pools.Count;
            
            foreach (var pool in _pools.Values)
            {
                totalPooled += pool.TotalCount;
            }
            
            return new PoolStats
            {
                totalPools = totalPools,
                totalActiveObjects = totalActive,
                totalPooledObjects = totalPooled,
                memoryUsageEstimateMB = (totalActive + totalPooled) * 0.001f // Rough estimate
            };
        }
    }
    
    [System.Serializable]
    public struct PoolStats
    {
        public int totalPools;
        public int totalActiveObjects;
        public int totalPooledObjects;
        public float memoryUsageEstimateMB;
    }
    
    internal class ObjectPool
    {
        private readonly string _id;
        private readonly GameObject _prefab;
        private readonly int _maxSize;
        private readonly bool _autoExpand;
        private readonly Queue<GameObject> _available = new Queue<GameObject>();
        private readonly HashSet<GameObject> _active = new HashSet<GameObject>();
        
        public int TotalCount => _available.Count + _active.Count;
        public int ActiveCount => _active.Count;
        public int AvailableCount => _available.Count;
        
        public ObjectPool(string id, GameObject prefab, int initialSize, int maxSize, bool autoExpand)
        {
            _id = id;
            _prefab = prefab;
            _maxSize = maxSize;
            _autoExpand = autoExpand;
        }
        
        public void PrePopulate(Transform parent)
        {
            for (int i = 0; i < _maxSize / 2; i++) // Pre-populate half the max size
            {
                CreateObject(parent);
            }
        }
        
        public void Prewarm(int count, Transform parent)
        {
            int needed = count - _available.Count;
            for (int i = 0; i < needed && TotalCount < _maxSize; i++)
            {
                CreateObject(parent);
            }
        }
        
        private GameObject CreateObject(Transform parent)
        {
            var obj = Object.Instantiate(_prefab, parent);
            obj.name = $"{_prefab.name}_Pooled";
            obj.SetActive(false);
            _available.Enqueue(obj);
            return obj;
        }
        
        public GameObject Get(Vector3 position, Quaternion rotation)
        {
            GameObject obj = null;
            
            if (_available.Count > 0)
            {
                obj = _available.Dequeue();
            }
            else if (_autoExpand && TotalCount < _maxSize)
            {
                obj = CreateObject(PoolService.Instance.transform);
            }
            
            if (obj != null)
            {
                obj.transform.position = position;
                obj.transform.rotation = rotation;
                obj.SetActive(true);
                _active.Add(obj);
                
                // Reset object state
                var pooledObject = obj.GetComponent<IPooledObject>();
                pooledObject?.OnSpawnFromPool();
            }
            
            return obj;
        }
        
        public void Release(GameObject obj)
        {
            if (obj == null || !_active.Contains(obj)) return;
            
            _active.Remove(obj);
            
            // Reset object
            var pooledObject = obj.GetComponent<IPooledObject>();
            pooledObject?.OnReturnToPool();
            
            obj.SetActive(false);
            _available.Enqueue(obj);
        }
        
        public void Cleanup()
        {
            // Remove destroyed objects from active set
            _active.RemoveWhere(obj => obj == null);
            
            // Limit pool size if it's grown too large
            while (_available.Count > _maxSize / 2)
            {
                var obj = _available.Dequeue();
                if (obj != null)
                {
                    Object.Destroy(obj);
                }
            }
        }
    }
    
    /// <summary>
    /// Interface for objects that need custom behavior when spawned from or returned to pool.
    /// </summary>
    public interface IPooledObject
    {
        void OnSpawnFromPool();
        void OnReturnToPool();
    }
}