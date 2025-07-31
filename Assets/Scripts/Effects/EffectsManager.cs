using System.Collections.Generic;
using UnityEngine;
using RedAlert.Core;

namespace RedAlert.Effects
{
    /// <summary>
    /// Centralized effects manager for Red Alert RTS.
    /// Handles particle effects, explosions, muzzle flashes, and environmental effects.
    /// Uses object pooling for optimal performance.
    /// </summary>
    public class EffectsManager : MonoBehaviour, UpdateDriver.ISlowTick
    {
        [Header("Effect Prefabs")]
        [SerializeField] private GameObject _muzzleFlashPrefab;
        [SerializeField] private GameObject _explosionPrefab;
        [SerializeField] private GameObject _smokeTrailPrefab;
        [SerializeField] private GameObject _bloodEffectPrefab;
        [SerializeField] private GameObject _sparksPrefab;
        [SerializeField] private GameObject _dustCloudPrefab;
        [SerializeField] private GameObject _buildingDestructionPrefab;
        
        [Header("Settings")]
        [SerializeField] private int _poolSizePerEffect = 20;
        [SerializeField] private float _effectCleanupTime = 30f;
        [SerializeField] private bool _enableParticles = true;
        [SerializeField] private float _particleQualityScale = 1f;
        
        private readonly Dictionary<string, List<GameObject>> _activeEffects = new Dictionary<string, List<GameObject>>();
        private PoolService _poolService;
        private static EffectsManager _instance;
        
        public static EffectsManager Instance => _instance;
        
        public bool ParticlesEnabled
        {
            get => _enableParticles;
            set => _enableParticles = value;
        }
        
        public float ParticleQualityScale
        {
            get => _particleQualityScale;
            set => _particleQualityScale = Mathf.Clamp01(value);
        }
        
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeEffectsPools();
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
        
        private void InitializeEffectsPools()
        {
            _poolService = PoolService.Instance;
            if (_poolService == null)
            {
                Debug.LogError("[EffectsManager] PoolService not found!");
                return;
            }
            
            // Register effect pools
            RegisterEffectPool("MuzzleFlash", _muzzleFlashPrefab);
            RegisterEffectPool("Explosion", _explosionPrefab);
            RegisterEffectPool("SmokeTrail", _smokeTrailPrefab);
            RegisterEffectPool("BloodEffect", _bloodEffectPrefab);
            RegisterEffectPool("Sparks", _sparksPrefab);
            RegisterEffectPool("DustCloud", _dustCloudPrefab);
            RegisterEffectPool("BuildingDestruction", _buildingDestructionPrefab);
        }
        
        private void RegisterEffectPool(string effectName, GameObject prefab)
        {
            if (prefab != null)
            {
                _poolService.RegisterPool($"Effect_{effectName}", prefab, _poolSizePerEffect);
                _activeEffects[effectName] = new List<GameObject>();
            }
        }
        
        public void SlowTick()
        {
            CleanupFinishedEffects();
        }
        
        private void CleanupFinishedEffects()
        {
            var effectsToCleanup = new List<GameObject>();
            
            foreach (var effectList in _activeEffects.Values)
            {
                for (int i = effectList.Count - 1; i >= 0; i--)
                {
                    var effect = effectList[i];
                    if (effect == null || !effect.activeInHierarchy)
                    {
                        effectList.RemoveAt(i);
                        if (effect != null)
                        {
                            effectsToCleanup.Add(effect);
                        }
                    }
                    else
                    {
                        // Check if particle system has finished
                        var particleSystem = effect.GetComponent<ParticleSystem>();
                        if (particleSystem != null && !particleSystem.IsAlive())
                        {
                            effectList.RemoveAt(i);
                            effectsToCleanup.Add(effect);
                        }
                    }
                }
            }
            
            // Return finished effects to pool
            foreach (var effect in effectsToCleanup)
            {
                _poolService.ReleaseEffect(effect);
            }
        }
        
        // Public API for playing effects
        public GameObject PlayMuzzleFlash(Vector3 position, Vector3 direction, float scale = 1f)
        {
            if (!_enableParticles) return null;
            
            var effect = PlayEffect("MuzzleFlash", position, Quaternion.LookRotation(direction));
            if (effect != null)
            {
                ScaleEffect(effect, scale * _particleQualityScale);
                AutoDestroyEffect(effect, 0.5f);
            }
            return effect;
        }
        
        public GameObject PlayExplosion(Vector3 position, float scale = 1f, ExplosionType type = ExplosionType.Small)
        {
            if (!_enableParticles) return null;
            
            var effect = PlayEffect("Explosion", position);
            if (effect != null)
            {
                ScaleEffect(effect, scale * _particleQualityScale);
                
                // Adjust explosion based on type
                var particleSystem = effect.GetComponent<ParticleSystem>();
                if (particleSystem != null)
                {
                    var main = particleSystem.main;
                    switch (type)
                    {
                        case ExplosionType.Small:
                            main.maxParticles = Mathf.RoundToInt(50 * _particleQualityScale);
                            break;
                        case ExplosionType.Medium:
                            main.maxParticles = Mathf.RoundToInt(100 * _particleQualityScale);
                            break;
                        case ExplosionType.Large:
                            main.maxParticles = Mathf.RoundToInt(200 * _particleQualityScale);
                            break;
                    }
                }
                
                AutoDestroyEffect(effect, 3f);
            }
            return effect;
        }
        
        public GameObject PlaySmokeTrail(Vector3 startPosition, Vector3 endPosition, float duration = 2f)
        {
            if (!_enableParticles) return null;
            
            var effect = PlayEffect("SmokeTrail", startPosition);
            if (effect != null)
            {
                ScaleEffect(effect, _particleQualityScale);
                
                // Animate smoke trail from start to end
                var trailComponent = effect.GetComponent<TrailRenderer>();
                if (trailComponent != null)
                {
                    StartCoroutine(AnimateTrail(effect.transform, startPosition, endPosition, duration));
                }
                
                AutoDestroyEffect(effect, duration + 1f);
            }
            return effect;
        }
        
        public GameObject PlayBloodEffect(Vector3 position, Vector3 direction, float intensity = 1f)
        {
            if (!_enableParticles) return null;
            
            var effect = PlayEffect("BloodEffect", position, Quaternion.LookRotation(direction));
            if (effect != null)
            {
                ScaleEffect(effect, intensity * _particleQualityScale);
                
                var particleSystem = effect.GetComponent<ParticleSystem>();
                if (particleSystem != null)
                {
                    var velocityOverLifetime = particleSystem.velocityOverLifetime;
                    velocityOverLifetime.enabled = true;
                    velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
                    velocityOverLifetime.radial = new ParticleSystem.MinMaxCurve(intensity * 2f);
                }
                
                AutoDestroyEffect(effect, 2f);
            }
            return effect;
        }
        
        public GameObject PlaySparks(Vector3 position, Vector3 direction, int sparkCount = 10)
        {
            if (!_enableParticles) return null;
            
            var effect = PlayEffect("Sparks", position, Quaternion.LookRotation(direction));
            if (effect != null)
            {
                var particleSystem = effect.GetComponent<ParticleSystem>();
                if (particleSystem != null)
                {
                    var emission = particleSystem.emission;
                    emission.SetBursts(new ParticleSystem.Burst[]
                    {
                        new ParticleSystem.Burst(0f, Mathf.RoundToInt(sparkCount * _particleQualityScale))
                    });
                }
                
                AutoDestroyEffect(effect, 1.5f);
            }
            return effect;
        }
        
        public GameObject PlayDustCloud(Vector3 position, float radius = 2f)
        {
            if (!_enableParticles) return null;
            
            var effect = PlayEffect("DustCloud", position);
            if (effect != null)
            {
                var particleSystem = effect.GetComponent<ParticleSystem>();
                if (particleSystem != null)
                {
                    var shape = particleSystem.shape;
                    shape.radius = radius;
                    
                    var main = particleSystem.main;
                    main.maxParticles = Mathf.RoundToInt(30 * radius * _particleQualityScale);
                }
                
                AutoDestroyEffect(effect, 4f);
            }
            return effect;
        }
        
        public GameObject PlayBuildingDestruction(Vector3 position, Vector3 buildingSize)
        {
            if (!_enableParticles) return null;
            
            var effect = PlayEffect("BuildingDestruction", position);
            if (effect != null)
            {
                float sizeScale = (buildingSize.x + buildingSize.z) * 0.5f;
                ScaleEffect(effect, sizeScale * _particleQualityScale);
                
                var particleSystems = effect.GetComponentsInChildren<ParticleSystem>();
                foreach (var ps in particleSystems)
                {
                    var shape = ps.shape;
                    shape.scale = buildingSize;
                    
                    var main = ps.main;
                    main.maxParticles = Mathf.RoundToInt(main.maxParticles * sizeScale * _particleQualityScale);
                }
                
                AutoDestroyEffect(effect, 6f);
            }
            return effect;
        }
        
        private GameObject PlayEffect(string effectName, Vector3 position, Quaternion rotation = default)
        {
            if (!_activeEffects.ContainsKey(effectName)) return null;
            
            var effect = _poolService.GetEffect(effectName, position, rotation);
            if (effect != null)
            {
                _activeEffects[effectName].Add(effect);
                
                // Start particle systems
                var particleSystems = effect.GetComponentsInChildren<ParticleSystem>();
                foreach (var ps in particleSystems)
                {
                    ps.Play();
                }
            }
            
            return effect;
        }
        
        private void ScaleEffect(GameObject effect, float scale)
        {
            if (effect == null) return;
            
            effect.transform.localScale = Vector3.one * scale;
            
            var particleSystems = effect.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particleSystems)
            {
                var main = ps.main;
                main.startSpeed = main.startSpeed.constant * scale;
                main.startSize = main.startSize.constant * scale;
            }
        }
        
        private void AutoDestroyEffect(GameObject effect, float delay)
        {
            if (effect != null)
            {
                StartCoroutine(DestroyEffectAfterDelay(effect, delay));
            }
        }
        
        private System.Collections.IEnumerator DestroyEffectAfterDelay(GameObject effect, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (effect != null)
            {
                _poolService.ReleaseEffect(effect);
            }
        }
        
        private System.Collections.IEnumerator AnimateTrail(Transform trailTransform, Vector3 start, Vector3 end, float duration)
        {
            float elapsed = 0f;
            
            while (elapsed < duration && trailTransform != null)
            {
                float t = elapsed / duration;
                trailTransform.position = Vector3.Lerp(start, end, t);
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
        
        // Performance management
        public void SetEffectQuality(float quality)
        {
            _particleQualityScale = Mathf.Clamp01(quality);
            
            // Update active particle systems
            foreach (var effectList in _activeEffects.Values)
            {
                foreach (var effect in effectList)
                {
                    if (effect != null)
                    {
                        var particleSystems = effect.GetComponentsInChildren<ParticleSystem>();
                        foreach (var ps in particleSystems)
                        {
                            var main = ps.main;
                            main.maxParticles = Mathf.RoundToInt(main.maxParticles * quality);
                        }
                    }
                }
            }
        }
        
        public void StopAllEffects()
        {
            foreach (var effectList in _activeEffects.Values)
            {
                foreach (var effect in effectList)
                {
                    if (effect != null)
                    {
                        _poolService.ReleaseEffect(effect);
                    }
                }
                effectList.Clear();
            }
        }
        
        public int GetActiveEffectCount()
        {
            int total = 0;
            foreach (var effectList in _activeEffects.Values)
            {
                total += effectList.Count;
            }
            return total;
        }
    }
    
    public enum ExplosionType
    {
        Small,
        Medium,
        Large
    }
}