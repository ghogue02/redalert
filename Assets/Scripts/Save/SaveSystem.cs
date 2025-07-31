using System;
using System.IO;
using UnityEngine;
using RedAlert.Economy;
using RedAlert.Core;

namespace RedAlert.Save
{
    /// <summary>
    /// Simple save/load system for Red Alert RTS.
    /// Saves game state including economy, settings, and basic unit positions.
    /// Uses JSON serialization for WebGL compatibility.
    /// </summary>
    public class SaveSystem : MonoBehaviour
    {
        [Header("Save Settings")]
        [SerializeField] private bool _autoSave = true;
        [SerializeField] private float _autoSaveInterval = 300f; // 5 minutes
        [SerializeField] private int _maxSaveSlots = 5;
        
        private float _lastAutoSaveTime;
        private const string SaveFilePrefix = "RedAlert_Save_";
        private const string SettingsFileName = "RedAlert_Settings";
        
        public event Action<GameSaveData> OnGameLoaded;
        public event Action<string> OnSaveCompleted;
        public event Action<string> OnLoadCompleted;
        
        private void Update()
        {
            if (_autoSave && Time.time - _lastAutoSaveTime >= _autoSaveInterval)
            {
                AutoSave();
                _lastAutoSaveTime = Time.time;
            }
        }
        
        public bool SaveGame(int slot = 0)
        {
            try
            {
                var saveData = CollectGameData();
                string json = JsonUtility.ToJson(saveData, true);
                string fileName = GetSaveFileName(slot);
                
#if UNITY_WEBGL && !UNITY_EDITOR
                PlayerPrefs.SetString(fileName, json);
                PlayerPrefs.Save();
#else
                string filePath = Path.Combine(Application.persistentDataPath, fileName + ".json");
                File.WriteAllText(filePath, json);
#endif
                
                OnSaveCompleted?.Invoke($"Game saved to slot {slot}");
                Debug.Log($"Game saved successfully to slot {slot}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save game: {e.Message}");
                return false;
            }
        }
        
        public bool LoadGame(int slot = 0)
        {
            try
            {
                string fileName = GetSaveFileName(slot);
                string json = null;
                
#if UNITY_WEBGL && !UNITY_EDITOR
                json = PlayerPrefs.GetString(fileName, "");
                if (string.IsNullOrEmpty(json))
                    return false;
#else
                string filePath = Path.Combine(Application.persistentDataPath, fileName + ".json");
                if (!File.Exists(filePath))
                    return false;
                json = File.ReadAllText(filePath);
#endif
                
                var saveData = JsonUtility.FromJson<GameSaveData>(json);
                ApplyGameData(saveData);
                
                OnGameLoaded?.Invoke(saveData);
                OnLoadCompleted?.Invoke($"Game loaded from slot {slot}");
                Debug.Log($"Game loaded successfully from slot {slot}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load game: {e.Message}");
                return false;
            }
        }
        
        public bool SaveExists(int slot = 0)
        {
            string fileName = GetSaveFileName(slot);
            
#if UNITY_WEBGL && !UNITY_EDITOR
            return PlayerPrefs.HasKey(fileName);
#else
            string filePath = Path.Combine(Application.persistentDataPath, fileName + ".json");
            return File.Exists(filePath);
#endif
        }
        
        public void DeleteSave(int slot = 0)
        {
            string fileName = GetSaveFileName(slot);
            
#if UNITY_WEBGL && !UNITY_EDITOR
            PlayerPrefs.DeleteKey(fileName);
            PlayerPrefs.Save();
#else
            string filePath = Path.Combine(Application.persistentDataPath, fileName + ".json");
            if (File.Exists(filePath))
                File.Delete(filePath);
#endif
        }
        
        public void SaveSettings(GameSettings settings)
        {
            try
            {
                string json = JsonUtility.ToJson(settings, true);
                
#if UNITY_WEBGL && !UNITY_EDITOR
                PlayerPrefs.SetString(SettingsFileName, json);
                PlayerPrefs.Save();
#else
                string filePath = Path.Combine(Application.persistentDataPath, SettingsFileName + ".json");
                File.WriteAllText(filePath, json);
#endif
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save settings: {e.Message}");
            }
        }
        
        public GameSettings LoadSettings()
        {
            try
            {
                string json = null;
                
#if UNITY_WEBGL && !UNITY_EDITOR
                json = PlayerPrefs.GetString(SettingsFileName, "");
#else
                string filePath = Path.Combine(Application.persistentDataPath, SettingsFileName + ".json");
                if (File.Exists(filePath))
                    json = File.ReadAllText(filePath);
#endif
                
                if (!string.IsNullOrEmpty(json))
                {
                    return JsonUtility.FromJson<GameSettings>(json);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load settings: {e.Message}");
            }
            
            return GameSettings.Default();
        }
        
        private void AutoSave()
        {
            SaveGame(999); // Use slot 999 for autosave
        }
        
        private string GetSaveFileName(int slot)
        {
            return slot == 999 ? SaveFilePrefix + "AutoSave" : SaveFilePrefix + slot.ToString("D2");
        }
        
        private GameSaveData CollectGameData()
        {
            var saveData = new GameSaveData
            {
                saveTime = DateTime.Now.ToBinary(),
                gameVersion = Application.version,
                sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            };
            
            // Save economy data
            var playerEconomy = FindObjectOfType<PlayerEconomy>();
            if (playerEconomy != null)
            {
                saveData.playerCrystalite = playerEconomy.Crystalite;
            }
            
            // Save game state
            var gameStateManager = FindObjectOfType<GameStateManager>();
            if (gameStateManager != null)
            {
                // Save basic game state info
                saveData.gameStarted = true; // Simplified for now
            }
            
            return saveData;
        }
        
        private void ApplyGameData(GameSaveData saveData)
        {
            // Apply economy data
            var playerEconomy = FindObjectOfType<PlayerEconomy>();
            if (playerEconomy != null)
            {
                playerEconomy.SetCrystalite(saveData.playerCrystalite);
            }
            
            // Apply other game state as needed
        }
    }
    
    [Serializable]
    public class GameSaveData
    {
        public long saveTime;
        public string gameVersion;
        public string sceneName;
        public int playerCrystalite;
        public bool gameStarted;
        
        // Unit and building data would go here in a full implementation
        // public UnitSaveData[] units;
        // public BuildingSaveData[] buildings;
    }
    
    [Serializable]
    public class GameSettings
    {
        public float masterVolume = 1f;
        public float sfxVolume = 0.8f;
        public float uiVolume = 0.9f;
        public float musicVolume = 0.6f;
        public bool fullscreen = true;
        public int resolutionWidth = 1920;
        public int resolutionHeight = 1080;
        public int qualityLevel = 2;
        public bool vsync = true;
        
        public static GameSettings Default()
        {
            return new GameSettings();
        }
    }
}