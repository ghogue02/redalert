using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using RedAlert.Units;
using RedAlert.Economy;

namespace RedAlert.Core
{
    /// <summary>
    /// Manages overall game state including victory/defeat conditions.
    /// Tracks teams, units, buildings, and resources to determine win/loss conditions.
    /// </summary>
    public class GameStateManager : MonoBehaviour, UpdateDriver.ISlowTick
    {
        [Header("Game Configuration")]
        [SerializeField] private float _gameStartDelay = 2f;
        [SerializeField] private bool _enableVictoryConditions = true;
        
        [Header("Victory Conditions")]
        [SerializeField] private bool _eliminationVictory = true;
        [SerializeField] private bool _economicVictory = false;
        [SerializeField] private int _economicVictoryThreshold = 10000;
        
        [Header("Team Configuration")]
        [SerializeField] private int _playerTeam = 1;
        [SerializeField] private int _enemyTeam = 2;
        
        [Header("Debug")]
        [SerializeField] private bool _showDebugInfo = true;
        
        public enum GameState
        {
            Loading,
            Playing,
            Victory,
            Defeat,
            Paused
        }
        
        private GameState _currentState = GameState.Loading;
        private float _gameStartTime;
        private float _gameTime;
        
        // Team tracking
        private Dictionary<int, List<GameObject>> _teamUnits = new Dictionary<int, List<GameObject>>();
        private Dictionary<int, List<GameObject>> _teamBuildings = new Dictionary<int, List<GameObject>>();
        private Dictionary<int, PlayerEconomy> _teamEconomies = new Dictionary<int, PlayerEconomy>();
        
        // Events
        public System.Action<GameState> OnGameStateChanged;
        public System.Action<int> OnTeamDefeated;
        public System.Action<int> OnTeamVictorious;
        
        public GameState CurrentState => _currentState;
        public float GameTime => _gameTime;
        public bool IsGameActive => _currentState == GameState.Playing;
        
        private void Start()
        {
            _gameStartTime = Time.time + _gameStartDelay;
            SetGameState(GameState.Loading);
            
            // Subscribe to events
            EventBus.OnUnitDied += HandleUnitDeath;
            EventBus.OnBuildingDestroyed += HandleBuildingDestroyed;
            
            // Find initial teams
            RefreshTeamData();
        }
        
        private void OnEnable()
        {
            UpdateDriver.Register(this);
        }
        
        private void OnDisable()
        {
            UpdateDriver.Unregister(this);
            
            // Unsubscribe from events
            EventBus.OnUnitDied -= HandleUnitDeath;
            EventBus.OnBuildingDestroyed -= HandleBuildingDestroyed;
        }
        
        public void SlowTick()
        {
            UpdateGameTime();
            
            switch (_currentState)
            {
                case GameState.Loading:
                    if (Time.time >= _gameStartTime)
                    {
                        StartGame();
                    }
                    break;
                    
                case GameState.Playing:
                    if (_enableVictoryConditions)
                    {
                        CheckVictoryConditions();
                    }
                    break;
            }
            
            // Refresh team data periodically
            RefreshTeamData();
        }
        
        private void UpdateGameTime()
        {
            if (_currentState == GameState.Playing)
            {
                _gameTime += 0.25f; // 4Hz updates
            }
        }
        
        private void StartGame()
        {
            SetGameState(GameState.Playing);
            Debug.Log("GameStateManager: Game Started!");
        }
        
        private void SetGameState(GameState newState)
        {
            if (_currentState == newState) return;
            
            GameState previousState = _currentState;
            _currentState = newState;
            
            Debug.Log($"GameStateManager: State changed from {previousState} to {newState}");
            OnGameStateChanged?.Invoke(newState);
            
            // Handle state-specific logic
            switch (newState)
            {
                case GameState.Victory:
                    HandleVictory();
                    break;
                case GameState.Defeat:
                    HandleDefeat();
                    break;
                case GameState.Paused:
                    Time.timeScale = 0f;
                    break;
                default:
                    if (previousState == GameState.Paused)
                    {
                        Time.timeScale = 1f;
                    }
                    break;
            }
        }
        
        private void CheckVictoryConditions()
        {
            if (_eliminationVictory)
            {
                CheckEliminationVictory();
            }
            
            if (_economicVictory)
            {
                CheckEconomicVictory();
            }
        }
        
        private void CheckEliminationVictory()
        {
            bool playerHasUnits = HasAliveUnits(_playerTeam);
            bool enemyHasUnits = HasAliveUnits(_enemyTeam);
            
            if (!playerHasUnits && !enemyHasUnits)
            {
                // Draw - for now, treat as defeat
                SetGameState(GameState.Defeat);
            }
            else if (!playerHasUnits)
            {
                SetGameState(GameState.Defeat);
                OnTeamDefeated?.Invoke(_playerTeam);
                OnTeamVictorious?.Invoke(_enemyTeam);
            }
            else if (!enemyHasUnits)
            {
                SetGameState(GameState.Victory);
                OnTeamDefeated?.Invoke(_enemyTeam);
                OnTeamVictorious?.Invoke(_playerTeam);
            }
        }
        
        private void CheckEconomicVictory()
        {
            if (_teamEconomies.ContainsKey(_playerTeam))
            {
                PlayerEconomy playerEconomy = _teamEconomies[_playerTeam];
                if (playerEconomy != null && playerEconomy.Crystalite >= _economicVictoryThreshold)
                {
                    SetGameState(GameState.Victory);
                    OnTeamVictorious?.Invoke(_playerTeam);
                }
            }
        }
        
        private bool HasAliveUnits(int teamId)
        {
            if (!_teamUnits.ContainsKey(teamId))
                return false;
                
            // Check units
            foreach (var unit in _teamUnits[teamId])
            {
                if (unit != null)
                {
                    Damageable damageable = unit.GetComponent<Damageable>();
                    if (damageable == null || !damageable.IsDead)
                    {
                        return true;
                    }
                }
            }
            
            // Check buildings (also count as "units" for elimination)
            if (_teamBuildings.ContainsKey(teamId))
            {
                foreach (var building in _teamBuildings[teamId])
                {
                    if (building != null)
                    {
                        Damageable damageable = building.GetComponent<Damageable>();
                        if (damageable == null || !damageable.IsDead)  
                        {
                            return true;
                        }
                    }
                }
            }
            
            return false;
        }
        
        private void RefreshTeamData()
        {
            // Clear existing data
            _teamUnits.Clear();
            _teamBuildings.Clear();
            _teamEconomies.Clear();
            
            // Find all team components
            Team[] allTeams = FindObjectsOfType<Team>();
            
            foreach (Team team in allTeams)
            {
                int teamId = team.TeamId;
                GameObject obj = team.gameObject;
                
                // Initialize team lists if needed
                if (!_teamUnits.ContainsKey(teamId))
                {
                    _teamUnits[teamId] = new List<GameObject>();
                }
                if (!_teamBuildings.ContainsKey(teamId))
                {
                    _teamBuildings[teamId] = new List<GameObject>();
                }
                
                // Categorize by layer or component type
                if (obj.layer == LayerMask.NameToLayer("Buildings") || obj.GetComponent<BuildQueue>() != null)
                {
                    _teamBuildings[teamId].Add(obj);
                }
                else
                {
                    _teamUnits[teamId].Add(obj);
                }
                
                // Check for economy components
                PlayerEconomy economy = obj.GetComponent<PlayerEconomy>();
                if (economy != null)
                {
                    _teamEconomies[teamId] = economy;
                }
            }
            
            // Also find standalone PlayerEconomy objects
            PlayerEconomy[] allEconomies = FindObjectsOfType<PlayerEconomy>();
            foreach (var economy in allEconomies)
            {
                // Assume standalone economies belong to player team
                if (!_teamEconomies.ContainsKey(_playerTeam))
                {
                    _teamEconomies[_playerTeam] = economy;
                }
            }
        }
        
        private void HandleUnitDeath(GameObject unit)
        {
            // Unit death is handled by periodic refresh and victory condition checks
            if (_showDebugInfo)
            {
                Team team = unit.GetComponent<Team>();
                string teamName = team != null ? $"Team {team.TeamId}" : "Unknown Team";
                Debug.Log($"GameStateManager: Unit {unit.name} from {teamName} has died");
            }
        }
        
        private void HandleBuildingDestroyed(GameObject building)
        {
            // Building destruction is handled by periodic refresh and victory condition checks
            if (_showDebugInfo)
            {
                Team team = building.GetComponent<Team>();
                string teamName = team != null ? $"Team {team.TeamId}" : "Unknown Team";
                Debug.Log($"GameStateManager: Building {building.name} from {teamName} has been destroyed");
            }
        }
        
        private void HandleVictory()
        {
            Debug.Log("VICTORY! Player has won the game!");
            // Could trigger victory UI, effects, etc.
        }
        
        private void HandleDefeat()
        {
            Debug.Log("DEFEAT! Player has lost the game!");
            // Could trigger defeat UI, restart options, etc.
        }
        
        /// <summary>
        /// Manually end the game with victory
        /// </summary>
        public void TriggerVictory()
        {
            SetGameState(GameState.Victory);
        }
        
        /// <summary>
        /// Manually end the game with defeat
        /// </summary>
        public void TriggerDefeat()
        {
            SetGameState(GameState.Defeat);
        }
        
        /// <summary>
        /// Pause/unpause the game
        /// </summary>
        public void TogglePause()
        {
            if (_currentState == GameState.Paused)
            {
                SetGameState(GameState.Playing);
            }
            else if (_currentState == GameState.Playing)
            {
                SetGameState(GameState.Paused);
            }
        }
        
        /// <summary>
        /// Get team unit count (alive units only)
        /// </summary>
        public int GetTeamUnitCount(int teamId)
        {
            if (!_teamUnits.ContainsKey(teamId))
                return 0;
                
            return _teamUnits[teamId].Count(unit => 
            {
                if (unit == null) return false;
                Damageable damageable = unit.GetComponent<Damageable>();
                return damageable == null || !damageable.IsDead;
            });
        }
        
        /// <summary>
        /// Get team building count (alive buildings only)
        /// </summary>
        public int GetTeamBuildingCount(int teamId)
        {
            if (!_teamBuildings.ContainsKey(teamId))
                return 0;
                
            return _teamBuildings[teamId].Count(building => 
            {
                if (building == null) return false;
                Damageable damageable = building.GetComponent<Damageable>();
                return damageable == null || !damageable.IsDead;
            });
        }
        
        /// <summary>
        /// Get team resource count
        /// </summary>
        public int GetTeamResources(int teamId)
        {
            if (_teamEconomies.ContainsKey(teamId) && _teamEconomies[teamId] != null)
            {
                return _teamEconomies[teamId].Crystalite;
            }
            return 0;
        }
        
        private void OnGUI()
        {
            if (!_showDebugInfo) return;
            
            // Debug display
            GUILayout.BeginArea(new Rect(10, Screen.height - 200, 300, 190));
            GUILayout.BeginVertical("box");
            
            GUILayout.Label($"Game State: {_currentState}");
            GUILayout.Label($"Game Time: {_gameTime:F1}s");
            
            GUILayout.Space(10);
            
            GUILayout.Label($"Player Team ({_playerTeam}):");
            GUILayout.Label($"  Units: {GetTeamUnitCount(_playerTeam)}");
            GUILayout.Label($"  Buildings: {GetTeamBuildingCount(_playerTeam)}");
            GUILayout.Label($"  Resources: {GetTeamResources(_playerTeam)}");
            
            GUILayout.Label($"Enemy Team ({_enemyTeam}):");
            GUILayout.Label($"  Units: {GetTeamUnitCount(_enemyTeam)}");
            GUILayout.Label($"  Buildings: {GetTeamBuildingCount(_enemyTeam)}");
            GUILayout.Label($"  Resources: {GetTeamResources(_enemyTeam)}");
            
            if (GUILayout.Button("Toggle Pause"))
            {
                TogglePause();
            }
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}