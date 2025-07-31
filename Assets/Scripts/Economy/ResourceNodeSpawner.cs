using UnityEngine;
using RedAlert.Data;
using System.Collections.Generic;

namespace RedAlert.Economy
{
    /// <summary>
    /// Spawns resource nodes on the map at specified locations.
    /// Manages resource node distribution and initial setup for RTS gameplay.
    /// </summary>
    public class ResourceNodeSpawner : MonoBehaviour
    {
        [Header("Resource Node Setup")]
        [SerializeField] private CrystaliteNodeDef _defaultNodeDef;
        [SerializeField] private GameObject _nodeVisualPrefab;
        [SerializeField] private int _nodesPerCluster = 4;
        [SerializeField] private float _clusterRadius = 5f;
        
        [Header("Map Distribution")]
        [SerializeField] private int _numberOfClusters = 6;
        [SerializeField] private Vector2 _mapBounds = new Vector2(32f, 32f);
        [SerializeField] private float _minDistanceFromCenter = 8f;
        [SerializeField] private bool _spawnOnStart = true;
        
        [Header("Debug")]
        [SerializeField] private bool _showGizmos = true;
        
        private List<CrystaliteNode> _spawnedNodes = new List<CrystaliteNode>();
        
        private void Start()
        {
            if (_spawnOnStart)
            {
                SpawnResourceNodes();
            }
        }
        
        /// <summary>
        /// Spawn resource nodes across the map in clusters
        /// </summary>
        public void SpawnResourceNodes()
        {
            ClearExistingNodes();
            
            if (_defaultNodeDef == null)
            {
                Debug.LogError("ResourceNodeSpawner: No default node definition assigned!");
                return;
            }
            
            for (int cluster = 0; cluster < _numberOfClusters; cluster++)
            {
                Vector3 clusterCenter = GenerateClusterPosition();
                SpawnResourceCluster(clusterCenter);
            }
            
            Debug.Log($"ResourceNodeSpawner: Spawned {_spawnedNodes.Count} resource nodes in {_numberOfClusters} clusters");
        }
        
        private Vector3 GenerateClusterPosition()
        {
            Vector3 position;
            int attempts = 0;
            const int maxAttempts = 50;
            
            do
            {
                // Generate random position within map bounds
                float x = Random.Range(-_mapBounds.x / 2f, _mapBounds.x / 2f);
                float z = Random.Range(-_mapBounds.y / 2f, _mapBounds.y / 2f);
                position = new Vector3(x, 0, z);
                attempts++;
                
                // Ensure minimum distance from center
                if (Vector3.Distance(Vector3.zero, position) >= _minDistanceFromCenter)
                {
                    break;
                }
                
            } while (attempts < maxAttempts);
            
            return position;
        }
        
        private void SpawnResourceCluster(Vector3 centerPosition)
        {
            for (int i = 0; i < _nodesPerCluster; i++)
            {
                // Generate position within cluster radius
                Vector2 randomCircle = Random.insideUnitCircle * _clusterRadius;
                Vector3 nodePosition = centerPosition + new Vector3(randomCircle.x, 0, randomCircle.y);
                
                // Clamp to map bounds
                nodePosition.x = Mathf.Clamp(nodePosition.x, -_mapBounds.x / 2f, _mapBounds.x / 2f);
                nodePosition.z = Mathf.Clamp(nodePosition.z, -_mapBounds.y / 2f, _mapBounds.y / 2f);
                
                SpawnResourceNode(nodePosition);
            }
        }
        
        private void SpawnResourceNode(Vector3 position)
        {
            // Create node game object
            GameObject nodeObject = new GameObject($"CrystaliteNode_{_spawnedNodes.Count + 1}");
            nodeObject.transform.position = position;
            nodeObject.transform.parent = transform;
            
            // Add CrystaliteNode component
            CrystaliteNode node = nodeObject.AddComponent<CrystaliteNode>();
            
            // Configure node with definition data
            if (_defaultNodeDef != null)
            {
                // Use reflection or direct assignment (since CrystaliteNode has serialized fields)
                // We'll set the capacity directly in the node
                nodeObject.name = $"CrystaliteNode_{_defaultNodeDef.Id}_{_spawnedNodes.Count + 1}";
            }
            
            // Add visual representation
            if (_nodeVisualPrefab != null)
            {
                GameObject visual = Instantiate(_nodeVisualPrefab, position, Quaternion.identity, nodeObject.transform);
            }
            else
            {
                // Create simple placeholder visual
                CreatePlaceholderVisual(nodeObject, position);
            }
            
            // Add to our list
            _spawnedNodes.Add(node);
        }
        
        private void CreatePlaceholderVisual(GameObject parent, Vector3 position)
        {
            // Create a simple crystal-like visual using primitives
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.transform.parent = parent.transform;
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(1.5f, 0.5f, 1.5f);
            visual.name = "CrystaliteVisual";
            
            // Set a distinctive color
            Renderer renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(0.2f, 0.9f, 1f, 1f); // Cyan crystalite color
                mat.SetFloat("_Metallic", 0.7f);
                mat.SetFloat("_Smoothness", 0.8f);
                renderer.material = mat;
            }
            
            // Add a slight glow effect with additional primitive
            GameObject glow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            glow.transform.parent = visual.transform;
            glow.transform.localPosition = Vector3.up * 0.5f;
            glow.transform.localScale = Vector3.one * 0.3f;
            glow.name = "CrystaliteGlow";
            
            Renderer glowRenderer = glow.GetComponent<Renderer>();
            if (glowRenderer != null)
            {
                Material glowMat = new Material(Shader.Find("Standard"));
                glowMat.color = new Color(0.5f, 1f, 1f, 0.7f);
                glowMat.SetFloat("_Mode", 3); // Transparent
                glowMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                glowMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                glowMat.SetInt("_ZWrite", 0);
                glowMat.DisableKeyword("_ALPHATEST_ON");
                glowMat.EnableKeyword("_ALPHABLEND_ON");
                glowMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                glowMat.renderQueue = 3000;
                glowRenderer.material = glowMat;
            }
        }
        
        /// <summary>
        /// Clear all existing spawned nodes
        /// </summary>
        public void ClearExistingNodes()
        {
            foreach (var node in _spawnedNodes)
            {
                if (node != null)
                {
                    DestroyImmediate(node.gameObject);
                }
            }
            _spawnedNodes.Clear();
        }
        
        /// <summary>
        /// Get all spawned resource nodes
        /// </summary>
        public List<CrystaliteNode> GetSpawnedNodes()
        {
            return new List<CrystaliteNode>(_spawnedNodes);
        }
        
        /// <summary>
        /// Get the nearest resource node to a position
        /// </summary>
        public CrystaliteNode GetNearestNode(Vector3 position)
        {
            CrystaliteNode nearest = null;
            float nearestDistance = float.MaxValue;
            
            foreach (var node in _spawnedNodes)
            {
                if (node == null || node.IsDepleted) continue;
                
                float distance = Vector3.Distance(position, node.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = node;
                }
            }
            
            return nearest;
        }
        
        private void OnDrawGizmos()
        {
            if (!_showGizmos) return;
            
            // Draw map bounds
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(_mapBounds.x, 1f, _mapBounds.y));
            
            // Draw minimum distance from center
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(Vector3.zero, _minDistanceFromCenter);
            
            // Draw cluster radius for each spawned node cluster
            if (_spawnedNodes.Count > 0)
            {
                Gizmos.color = Color.green;
                var processedClusters = new List<Vector3>();
                
                foreach (var node in _spawnedNodes)
                {
                    if (node == null) continue;
                    
                    Vector3 nodePos = node.transform.position;
                    bool isNewCluster = true;
                    
                    foreach (var cluster in processedClusters)
                    {
                        if (Vector3.Distance(nodePos, cluster) <= _clusterRadius)
                        {
                            isNewCluster = false;
                            break;
                        }
                    }
                    
                    if (isNewCluster)
                    {
                        processedClusters.Add(nodePos);
                        Gizmos.DrawWireSphere(nodePos, _clusterRadius);
                    }
                }
            }
        }
    }
}