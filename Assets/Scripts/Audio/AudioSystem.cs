using System.Collections.Generic;
using UnityEngine;
using RedAlert.Core;

namespace RedAlert.Audio
{
    /// <summary>
    /// Centralized audio system for Red Alert RTS.
    /// Handles SFX, UI sounds, command acknowledgments, and combat audio.
    /// Uses object pooling for performance and supports 3D spatial audio.
    /// </summary>
    public class AudioSystem : MonoBehaviour, UpdateDriver.ISlowTick
    {
        [Header("Audio Sources")]
        [SerializeField] private AudioSource _musicSource;
        [SerializeField] private AudioSource _uiSource;
        [SerializeField] private GameObject _audioSourcePrefab;
        [SerializeField] private Transform _audioSourceParent;
        
        [Header("Audio Settings")]
        [SerializeField] private float _masterVolume = 1f;
        [SerializeField] private float _sfxVolume = 0.8f;
        [SerializeField] private float _uiVolume = 0.9f;
        [SerializeField] private float _musicVolume = 0.6f;
        [SerializeField] private int _poolSize = 20;
        
        [Header("Audio Clips")]
        [SerializeField] private AudioClip _unitSelectSound;
        [SerializeField] private AudioClip _unitMoveCommand;
        [SerializeField] private AudioClip _unitAttackCommand;
        [SerializeField] private AudioClip _buildingPlaceSound;
        [SerializeField] private AudioClip _buildingCompleteSound;
        [SerializeField] private AudioClip _weaponFireSound;
        [SerializeField] private AudioClip _explosionSound;
        [SerializeField] private AudioClip _insufficientResourcesSound;
        [SerializeField] private AudioClip _buttonClickSound;
        
        private readonly Queue<AudioSource> _audioSourcePool = new Queue<AudioSource>();
        private readonly List<AudioSource> _activeAudioSources = new List<AudioSource>();
        
        private static AudioSystem _instance;
        public static AudioSystem Instance => _instance;
        
        public float MasterVolume
        {
            get => _masterVolume;
            set
            {
                _masterVolume = Mathf.Clamp01(value);
                UpdateVolumeSettings();
            }
        }
        
        public float SFXVolume
        {
            get => _sfxVolume;
            set
            {
                _sfxVolume = Mathf.Clamp01(value);
                UpdateVolumeSettings();
            }
        }
        
        public float UIVolume
        {
            get => _uiVolume;
            set
            {
                _uiVolume = Mathf.Clamp01(value);
                UpdateVolumeSettings();
            }
        }
        
        public float MusicVolume
        {
            get => _musicVolume;
            set
            {
                _musicVolume = Mathf.Clamp01(value);
                UpdateVolumeSettings();
            }
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAudioSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnEnable()
        {
            UpdateDriver.Register(this);
            
            // Subscribe to game events
            EventBus.OnInsufficientResources += OnInsufficientResources;
        }

        private void OnDisable()
        {
            UpdateDriver.Unregister(this);
            
            // Unsubscribe from game events
            EventBus.OnInsufficientResources -= OnInsufficientResources;
        }

        private void InitializeAudioSystem()
        {
            // Create audio source pool
            if (_audioSourceParent == null)
            {
                var poolContainer = new GameObject("AudioSourcePool");
                poolContainer.transform.SetParent(transform);
                _audioSourceParent = poolContainer.transform;
            }
            
            for (int i = 0; i < _poolSize; i++)
            {
                CreatePooledAudioSource();
            }
            
            // Initialize UI audio source if not assigned
            if (_uiSource == null)
            {
                var uiAudioGO = new GameObject("UI_AudioSource");
                uiAudioGO.transform.SetParent(transform);
                _uiSource = uiAudioGO.AddComponent<AudioSource>();
                _uiSource.playOnAwake = false;
                _uiSource.spatialBlend = 0f; // 2D audio
            }
            
            // Initialize music source if not assigned
            if (_musicSource == null)
            {
                var musicAudioGO = new GameObject("Music_AudioSource");
                musicAudioGO.transform.SetParent(transform);
                _musicSource = musicAudioGO.AddComponent<AudioSource>();
                _musicSource.playOnAwake = false;
                _musicSource.spatialBlend = 0f; // 2D audio
                _musicSource.loop = true;
            }
            
            UpdateVolumeSettings();
        }

        private void CreatePooledAudioSource()
        {
            GameObject audioGO;
            if (_audioSourcePrefab != null)
            {
                audioGO = Instantiate(_audioSourcePrefab, _audioSourceParent);
            }
            else
            {
                audioGO = new GameObject("PooledAudioSource");
                audioGO.transform.SetParent(_audioSourceParent);
                audioGO.AddComponent<AudioSource>();
            }
            
            var audioSource = audioGO.GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D audio by default
            audioGO.SetActive(false);
            
            _audioSourcePool.Enqueue(audioSource);
        }

        private void UpdateVolumeSettings()
        {
            if (_uiSource != null)
                _uiSource.volume = _masterVolume * _uiVolume;
            
            if (_musicSource != null)
                _musicSource.volume = _masterVolume * _musicVolume;
        }

        public void SlowTick()
        {
            // Clean up finished audio sources and return to pool
            for (int i = _activeAudioSources.Count - 1; i >= 0; i--)
            {
                var audioSource = _activeAudioSources[i];
                if (audioSource != null && !audioSource.isPlaying)
                {
                    audioSource.gameObject.SetActive(false);
                    _activeAudioSources.RemoveAt(i);
                    _audioSourcePool.Enqueue(audioSource);
                }
            }
        }

        // Public API for playing sounds
        public void PlayUISound(AudioClip clip)
        {
            if (clip != null && _uiSource != null)
            {
                _uiSource.PlayOneShot(clip, _masterVolume * _uiVolume);
            }
        }

        public void PlaySpatialSound(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (clip == null) return;
            
            var audioSource = GetPooledAudioSource();
            if (audioSource != null)
            {
                audioSource.transform.position = position;
                audioSource.clip = clip;
                audioSource.volume = _masterVolume * _sfxVolume * volume;
                audioSource.spatialBlend = 1f; // 3D
                audioSource.gameObject.SetActive(true);
                audioSource.Play();
                
                _activeAudioSources.Add(audioSource);
            }
        }

        public void PlaySound2D(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;
            
            var audioSource = GetPooledAudioSource();
            if (audioSource != null)
            {
                audioSource.clip = clip;
                audioSource.volume = _masterVolume * _sfxVolume * volume;
                audioSource.spatialBlend = 0f; // 2D
                audioSource.gameObject.SetActive(true);
                audioSource.Play();
                
                _activeAudioSources.Add(audioSource);
            }
        }

        public void PlayMusic(AudioClip musicClip, bool loop = true)
        {
            if (_musicSource != null && musicClip != null)
            {
                _musicSource.clip = musicClip;
                _musicSource.loop = loop;
                _musicSource.Play();
            }
        }

        public void StopMusic()
        {
            if (_musicSource != null)
            {
                _musicSource.Stop();
            }
        }

        private AudioSource GetPooledAudioSource()
        {
            if (_audioSourcePool.Count > 0)
            {
                return _audioSourcePool.Dequeue();
            }
            
            // Pool exhausted, create new one
            CreatePooledAudioSource();
            return _audioSourcePool.Count > 0 ? _audioSourcePool.Dequeue() : null;
        }

        // Game event handlers
        private void OnInsufficientResources(int requiredAmount)
        {
            PlayUISound(_insufficientResourcesSound);
        }

        // Command and unit feedback sounds
        public void PlayUnitSelectSound()
        {
            PlayUISound(_unitSelectSound);
        }

        public void PlayUnitMoveCommand()
        {
            PlayUISound(_unitMoveCommand);
        }

        public void PlayUnitAttackCommand()
        {
            PlayUISound(_unitAttackCommand);
        }

        public void PlayBuildingPlaceSound(Vector3 position)
        {
            PlaySpatialSound(_buildingPlaceSound, position);
        }

        public void PlayBuildingCompleteSound(Vector3 position)
        {
            PlaySpatialSound(_buildingCompleteSound, position);
        }

        public void PlayWeaponFireSound(Vector3 position)
        {
            PlaySpatialSound(_weaponFireSound, position, 0.8f);
        }

        public void PlayExplosionSound(Vector3 position)
        {
            PlaySpatialSound(_explosionSound, position);
        }

        public void PlayButtonClickSound()
        {
            PlayUISound(_buttonClickSound);
        }
    }
}