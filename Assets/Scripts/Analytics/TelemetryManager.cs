using System;
using System.Collections.Generic;
using UnityEngine;
using RedAlert.Core;
using RedAlert.Units;
using RedAlert.Economy;

namespace RedAlert.Analytics
{
    /// <summary>
    /// Telemetry manager for Red Alert RTS game balance and analytics.
    /// Tracks gameplay metrics for balance tuning and player behavior analysis.
    /// </summary>
    public class TelemetryManager : MonoBehaviour, UpdateDriver.ISlowTick
    {
        [Header("Telemetry Settings")]
        [SerializeField] private bool _enableTelemetry = true;
        [SerializeField] private float _reportInterval = 30f;
        [SerializeField] private bool _logToConsole = true;
        [SerializeField] private bool _exportToFile = false;
        
        [Header("Tracked Metrics")]
        [SerializeField] private bool _trackCombat = true;
        [SerializeField] private bool _trackEconomy = true;
        [SerializeField] private bool _trackBuilding = true;
        [SerializeField] private bool _trackUnit = true;
        [SerializeField] private bool _trackPerformance = true;
        
        private GameplayMetrics _currentSession;
        private readonly List<GameplayMetrics> _historicalSessions = new List<GameplayMetrics>();
        private float _lastReportTime;
        private float _sessionStartTime;
        
        // Event tracking
        private readonly Dictionary<string, int> _eventCounts = new Dictionary<string, int>();
        private readonly Dictionary<string, float> _eventTotalValues = new Dictionary<string, float>();
        
        public GameplayMetrics CurrentSessionMetrics => _currentSession;
        public bool TelemetryEnabled => _enableTelemetry;
        
        private void Awake()
        {
            InitializeTelemetry();
        }
        
        private void OnEnable()
        {
            UpdateDriver.Register(this);
            SubscribeToEvents();
        }
        
        private void OnDisable()
        {
            UpdateDriver.Unregister(this);
            UnsubscribeFromEvents();
        }
        
        private void InitializeTelemetry()
        {
            if (!_enableTelemetry) return;
            
            _sessionStartTime = Time.time;
            _currentSession = new GameplayMetrics
            {
                sessionId = Guid.NewGuid().ToString(),
                startTime = DateTime.Now,
                gameVersion = Application.version,
                platform = Application.platform.ToString()
            };
            
            Debug.Log($"[Telemetry] Session started: {_currentSession.sessionId}");
        }
        
        private void SubscribeToEvents()
        {
            if (!_enableTelemetry) return;
            
            // Combat events
            EventBus.OnUnitDeath += OnUnitDeath;
            EventBus.OnUnderAttack += OnUnderAttack;
            
            // Economy events
            // Note: These would need to be added to EventBus
            // EventBus.OnResourcesChanged += OnResourcesChanged;
            // EventBus.OnBuildingCompleted += OnBuildingCompleted;
        }
        
        private void UnsubscribeFromEvents()
        {
            if (!_enableTelemetry) return;
            
            EventBus.OnUnitDeath -= OnUnitDeath;
            EventBus.OnUnderAttack -= OnUnderAttack;
        }
        
        public void SlowTick()
        {
            if (!_enableTelemetry) return;
            
            UpdatePerformanceMetrics();
            
            if (Time.time - _lastReportTime >= _reportInterval)
            {
                GeneratePeriodicReport();
                _lastReportTime = Time.time;
            }
        }
        
        private void UpdatePerformanceMetrics()
        {
            if (!_trackPerformance) return;
            
            _currentSession.averageFPS = (_currentSession.averageFPS + (1f / Time.unscaledDeltaTime)) * 0.5f;
            _currentSession.memoryUsageMB = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemory(false) / (1024 * 1024);
        }
        
        // Event handlers
        private void OnUnitDeath(GameObject unit)
        {
            if (!_trackCombat) return;
            
            var basicUnit = unit.GetComponent<BasicUnit>();
            var team = unit.GetComponent<Team>();
            
            if (basicUnit != null && team != null)
            {
                _currentSession.totalDeaths++;
                
                if (team.TeamId == 1) // Player team
                {
                    _currentSession.playerUnitsLost++;
                    TrackEvent("PlayerUnitLost", basicUnit.name);
                }
                else
                {
                    _currentSession.enemyUnitsDestroyed++;
                    TrackEvent("EnemyUnitDestroyed", basicUnit.name);
                }
                
                // Track unit-specific deaths
                string unitType = basicUnit.name.Replace("(Clone)", "");
                TrackEvent($"UnitDeath_{unitType}");
            }
        }
        
        private void OnUnderAttack(GameObject unit)
        {
            if (!_trackCombat) return;
            
            _currentSession.combatEngagements++;
            TrackEvent("CombatEngagement");
        }
        
        // Resource tracking
        public void TrackResourceChange(string resourceType, int amount, int newTotal)
        {
            if (!_enableTelemetry || !_trackEconomy) return;
            
            if (resourceType == "Crystalite")
            {
                if (amount > 0)
                {
                    _currentSession.resourcesGathered += amount;
                    TrackEvent("ResourceGathered", amount);
                }
                else
                {
                    _currentSession.resourcesSpent += Mathf.Abs(amount);
                    TrackEvent("ResourceSpent", Mathf.Abs(amount));
                }
                
                _currentSession.currentResources = newTotal;
                _currentSession.peakResources = Mathf.Max(_currentSession.peakResources, newTotal);
            }
        }
        
        // Building tracking
        public void TrackBuildingPlaced(string buildingType, Vector3 position, int cost)
        {
            if (!_enableTelemetry || !_trackBuilding) return;
            
            _currentSession.buildingsConstructed++;
            TrackEvent($"BuildingPlaced_{buildingType}");
            TrackEvent("BuildingPlaced", cost);
            
            // Track building positions for map heat analysis
            _currentSession.buildingPositions.Add(new Vector2(position.x, position.z));
        }
        
        public void TrackBuildingDestroyed(string buildingType, bool playerOwned)
        {
            if (!_enableTelemetry || !_trackBuilding) return;
            
            if (playerOwned)
            {
                _currentSession.playerBuildingsLost++;
                TrackEvent($"PlayerBuildingLost_{buildingType}");
            }
            else
            {
                _currentSession.enemyBuildingsDestroyed++;
                TrackEvent($"EnemyBuildingDestroyed_{buildingType}");
            }
        }
        
        // Unit tracking
        public void TrackUnitProduced(string unitType, int cost, float buildTime)
        {
            if (!_enableTelemetry || !_trackUnit) return;
            
            _currentSession.unitsProduced++;
            TrackEvent($"UnitProduced_{unitType}");
            TrackEvent("UnitProduced", cost);
            
            // Track build times for balance analysis
            _currentSession.averageBuildTime = 
                (_currentSession.averageBuildTime * (_currentSession.unitsProduced - 1) + buildTime) / _currentSession.unitsProduced;
        }
        
        // Command tracking
        public void TrackCommand(string commandType, int unitCount)
        {
            if (!_enableTelemetry) return;
            
            _currentSession.commandsIssued++;
            TrackEvent($"Command_{commandType}");
            TrackEvent("CommandWithUnits", unitCount);
        }
        
        // Generic event tracking
        public void TrackEvent(string eventName, float value = 1f)
        {
            if (!_enableTelemetry) return;
            
            if (!_eventCounts.ContainsKey(eventName))
            {
                _eventCounts[eventName] = 0;
                _eventTotalValues[eventName] = 0f;
            }
            
            _eventCounts[eventName]++;
            _eventTotalValues[eventName] += value;
        }
        
        // Reporting
        private void GeneratePeriodicReport()
        {
            if (!_logToConsole) return;
            
            float sessionDuration = Time.time - _sessionStartTime;
            
            string report = $"\n=== Red Alert RTS - Telemetry Report ===\n";
            report += $"Session Duration: {sessionDuration:F1}s\n";
            report += $"Average FPS: {_currentSession.averageFPS:F1}\n";
            report += $"Memory Usage: {_currentSession.memoryUsageMB}MB\n\n";
            
            report += "Combat Metrics:\n";
            report += $"  Total Deaths: {_currentSession.totalDeaths}\n";
            report += $"  Player Units Lost: {_currentSession.playerUnitsLost}\n";
            report += $"  Enemy Units Destroyed: {_currentSession.enemyUnitsDestroyed}\n";
            report += $"  Combat Engagements: {_currentSession.combatEngagements}\n\n";
            
            report += "Economy Metrics:\n";
            report += $"  Resources Gathered: {_currentSession.resourcesGathered}\n";
            report += $"  Resources Spent: {_currentSession.resourcesSpent}\n";
            report += $"  Current Resources: {_currentSession.currentResources}\n";
            report += $"  Peak Resources: {_currentSession.peakResources}\n\n";
            
            report += "Production Metrics:\n";
            report += $"  Buildings Constructed: {_currentSession.buildingsConstructed}\n";
            report += $"  Units Produced: {_currentSession.unitsProduced}\n";
            report += $"  Commands Issued: {_currentSession.commandsIssued}\n";
            report += $"  Average Build Time: {_currentSession.averageBuildTime:F1}s\n\n";
            
            // Top events
            report += "Top Events:\n";
            var sortedEvents = new List<KeyValuePair<string, int>>(_eventCounts);
            sortedEvents.Sort((a, b) => b.Value.CompareTo(a.Value));
            
            for (int i = 0; i < Mathf.Min(5, sortedEvents.Count); i++)
            {
                var evt = sortedEvents[i];
                float avgValue = _eventTotalValues[evt.Key] / evt.Value;
                report += $"  {evt.Key}: {evt.Value} times (avg: {avgValue:F1})\n";
            }
            
            Debug.Log(report);
        }
        
        public GameplayReport GenerateSessionReport()
        {
            if (!_enableTelemetry) return null;
            
            _currentSession.endTime = DateTime.Now;
            _currentSession.sessionDuration = Time.time - _sessionStartTime;
            
            var report = new GameplayReport
            {
                metrics = _currentSession,
                eventCounts = new Dictionary<string, int>(_eventCounts),
                eventValues = new Dictionary<string, float>(_eventTotalValues),
                balanceAnalysis = GenerateBalanceAnalysis()
            };
            
            return report;
        }
        
        private BalanceAnalysis GenerateBalanceAnalysis()
        {
            var analysis = new BalanceAnalysis();
            
            // Calculate kill/death ratios
            if (_currentSession.playerUnitsLost > 0)
            {
                analysis.killDeathRatio = (float)_currentSession.enemyUnitsDestroyed / _currentSession.playerUnitsLost;
            }
            
            // Calculate resource efficiency
            if (_currentSession.resourcesSpent > 0)
            {
                analysis.resourceEfficiency = Mathf.Min(1f, (float)_currentSession.enemyUnitsDestroyed / (_currentSession.resourcesSpent / 100f));
            }
            
            // Economic growth rate
            if (_currentSession.sessionDuration > 0)
            {
                analysis.economicGrowthRate = _currentSession.resourcesGathered / _currentSession.sessionDuration;
            }
            
            // Action per minute
            analysis.actionsPerMinute = (_currentSession.commandsIssued * 60f) / _currentSession.sessionDuration;
            
            // Generate recommendations
            analysis.recommendations = GenerateBalanceRecommendations(analysis);
            
            return analysis;
        }
        
        private List<string> GenerateBalanceRecommendations(BalanceAnalysis analysis)
        {
            var recommendations = new List<string>();
            
            if (analysis.killDeathRatio < 0.5f)
            {
                recommendations.Add("Consider reducing enemy unit damage or increasing player unit health");
            }
            else if (analysis.killDeathRatio > 2f)
            {
                recommendations.Add("Consider increasing enemy unit health or player unit cost");
            }
            
            if (analysis.resourceEfficiency < 0.3f)
            {
                recommendations.Add("Units may be too expensive for their effectiveness");
            }
            else if (analysis.resourceEfficiency > 0.8f)
            {
                recommendations.Add("Units may be too cost-effective, consider increasing prices");
            }
            
            if (analysis.economicGrowthRate < 5f)
            {
                recommendations.Add("Resource generation may be too slow");
            }
            else if (analysis.economicGrowthRate > 20f)
            {
                recommendations.Add("Resource generation may be too fast");
            }
            
            if (analysis.actionsPerMinute < 30f)
            {
                recommendations.Add("Game pace may be too slow, consider faster build times");
            }
            else if (analysis.actionsPerMinute > 120f)
            {
                recommendations.Add("Game pace may be too fast, consider slower build times");
            }
            
            return recommendations;
        }
        
        // Save session data
        public void EndSession()
        {
            if (!_enableTelemetry) return;
            
            var report = GenerateSessionReport();
            _historicalSessions.Add(_currentSession);
            
            if (_exportToFile)
            {
                ExportSessionData(report);
            }
            
            Debug.Log($"[Telemetry] Session ended: {_currentSession.sessionId}");
        }
        
        private void ExportSessionData(GameplayReport report)
        {
            try
            {
                string json = JsonUtility.ToJson(report, true);
                string filename = $"telemetry_{_currentSession.sessionId}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                string path = System.IO.Path.Combine(Application.persistentDataPath, filename);
                System.IO.File.WriteAllText(path, json);
                
                Debug.Log($"[Telemetry] Session data exported to: {path}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Telemetry] Failed to export session data: {e.Message}");
            }
        }
    }
    
    [Serializable]
    public class GameplayMetrics
    {
        public string sessionId;
        public DateTime startTime;
        public DateTime endTime;
        public float sessionDuration;
        public string gameVersion;
        public string platform;
        
        // Performance metrics
        public float averageFPS;
        public long memoryUsageMB;
        
        // Combat metrics
        public int totalDeaths;
        public int playerUnitsLost;
        public int enemyUnitsDestroyed;
        public int combatEngagements;
        
        // Economy metrics
        public int resourcesGathered;
        public int resourcesSpent;
        public int currentResources;
        public int peakResources;
        
        // Building metrics
        public int buildingsConstructed;
        public int playerBuildingsLost;
        public int enemyBuildingsDestroyed;
        public List<Vector2> buildingPositions = new List<Vector2>();
        
        // Unit metrics
        public int unitsProduced;
        public float averageBuildTime;
        
        // Command metrics
        public int commandsIssued;
    }
    
    [Serializable]
    public class GameplayReport
    {
        public GameplayMetrics metrics;
        public Dictionary<string, int> eventCounts;
        public Dictionary<string, float> eventValues;
        public BalanceAnalysis balanceAnalysis;
    }
    
    [Serializable]
    public class BalanceAnalysis
    {
        public float killDeathRatio;
        public float resourceEfficiency;
        public float economicGrowthRate;
        public float actionsPerMinute;
        public List<string> recommendations = new List<string>();
    }
}