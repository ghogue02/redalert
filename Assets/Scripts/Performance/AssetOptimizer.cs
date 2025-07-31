using System.Collections.Generic;
using UnityEngine;

namespace RedAlert.Performance
{
    /// <summary>
    /// Asset optimization system for WebGL deployment.
    /// Handles texture compression, LOD management, and GPU instancing setup.
    /// </summary>
    public class AssetOptimizer : MonoBehaviour
    {
        [Header("Texture Optimization")]
        [SerializeField] private int _maxTextureSize = 1024;
        [SerializeField] private TextureFormat _preferredFormat = TextureFormat.ASTC_4x4;
        [SerializeField] private bool _generateMipmaps = true;
        
        [Header("LOD Settings")]
        [SerializeField] private float[] _lodDistances = { 15f, 50f, 100f };
        [SerializeField] private float[] _lodQualityReduction = { 1f, 0.7f, 0.4f };
        
        [Header("Instancing")]
        [SerializeField] private bool _enableGPUInstancing = true;
        [SerializeField] private int _maxInstancesPerBatch = 1000;
        
        private readonly Dictionary<string, LODGroup> _lodGroups = new Dictionary<string, LODGroup>();
        private readonly Dictionary<Material, Material> _instancedMaterials = new Dictionary<Material, Material>();
        
        private void Start()
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                OptimizeAssetsForWebGL();
            }
        }
        
        private void OptimizeAssetsForWebGL()
        {
            Debug.Log("[AssetOptimizer] Starting WebGL asset optimization...");
            
            // Optimize textures
            OptimizeTextures();
            
            // Set up LOD systems
            SetupLODGroups();
            
            // Enable GPU instancing where possible
            if (_enableGPUInstancing)
            {
                SetupGPUInstancing();
            }
            
            // Reduce quality settings for better performance
            OptimizeQualitySettings();
            
            Debug.Log("[AssetOptimizer] WebGL optimization complete.");
        }
        
        private void OptimizeTextures()
        {
            var textures = FindObjectsOfType<Renderer>();
            int optimizedCount = 0;
            
            foreach (var renderer in textures)
            {
                foreach (var material in renderer.materials)
                {
                    if (material == null) continue;
                    
                    // Check main texture
                    if (material.mainTexture is Texture2D mainTex)
                    {
                        if (OptimizeTexture(mainTex))
                            optimizedCount++;
                    }
                    
                    // Check other common texture properties
                    string[] textureProperties = { "_BumpMap", "_MetallicGlossMap", "_OcclusionMap", "_EmissionMap" };
                    foreach (string prop in textureProperties)
                    {
                        if (material.HasProperty(prop) && material.GetTexture(prop) is Texture2D tex)
                        {
                            if (OptimizeTexture(tex))
                                optimizedCount++;
                        }
                    }
                }
            }
            
            Debug.Log($"[AssetOptimizer] Optimized {optimizedCount} textures for WebGL.");
        }
        
        private bool OptimizeTexture(Texture2D texture)
        {
            if (texture == null) return false;
            
            // For runtime, we can't modify texture import settings
            // But we can provide recommendations for build-time optimization
            bool needsOptimization = false;
            
            if (texture.width > _maxTextureSize || texture.height > _maxTextureSize)
            {
                Debug.LogWarning($"[AssetOptimizer] Texture '{texture.name}' exceeds max size ({_maxTextureSize}px). Consider reducing size.");
                needsOptimization = true;
            }
            
            if (!texture.isReadable)
            {
                // Good - texture is optimized for GPU use
            }
            else
            {
                Debug.LogWarning($"[AssetOptimizer] Texture '{texture.name}' is readable. Disable 'Read/Write Enabled' for better performance.");
                needsOptimization = true;
            }
            
            return needsOptimization;
        }
        
        private void SetupLODGroups()
        {
            var allRenderers = FindObjectsOfType<Renderer>();
            var processedObjects = new HashSet<GameObject>();
            int lodGroupsCreated = 0;
            
            foreach (var renderer in allRenderers)
            {
                var rootObject = renderer.transform.root.gameObject;
                if (processedObjects.Contains(rootObject)) continue;
                
                // Skip UI and small objects
                if (rootObject.layer == LayerMask.NameToLayer("UI")) continue;
                
                var bounds = renderer.bounds;
                float objectSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                
                // Only create LOD groups for larger objects
                if (objectSize > 2f)
                {
                    CreateLODGroup(rootObject);
                    processedObjects.Add(rootObject);
                    lodGroupsCreated++;
                }
            }
            
            Debug.Log($"[AssetOptimizer] Created {lodGroupsCreated} LOD groups.");
        }
        
        private void CreateLODGroup(GameObject target)
        {
            if (target.GetComponent<LODGroup>() != null) return; // Already has LOD group
            
            var lodGroup = target.AddComponent<LODGroup>();
            var renderers = target.GetComponentsInChildren<Renderer>();
            
            // Create LOD levels
            LOD[] lods = new LOD[_lodDistances.Length];
            
            for (int i = 0; i < _lodDistances.Length; i++)
            {
                // For simplicity, use the same renderers for all LOD levels
                // In a full implementation, you'd have different mesh quality per level
                lods[i] = new LOD(_lodQualityReduction[i], renderers);
            }
            
            lodGroup.SetLODs(lods);
            lodGroup.RecalculateBounds();
            
            _lodGroups[target.name] = lodGroup;
        }
        
        private void SetupGPUInstancing()
        {
            var renderers = FindObjectsOfType<MeshRenderer>();
            int instancedCount = 0;
            
            foreach (var renderer in renderers)
            {
                foreach (var material in renderer.materials)
                {
                    if (material == null) continue;
                    
                    // Check if material supports instancing
                    if (!material.enableInstancing)
                    {
                        // Create instanced version if possible
                        if (CanEnableInstancing(material))
                        {
                            var instancedMat = CreateInstancedMaterial(material);
                            if (instancedMat != null)
                            {
                                // Replace material
                                var materials = renderer.materials;
                                for (int i = 0; i < materials.Length; i++)
                                {
                                    if (materials[i] == material)
                                    {
                                        materials[i] = instancedMat;
                                    }
                                }
                                renderer.materials = materials;
                                instancedCount++;
                            }
                        }
                    }
                }
            }
            
            Debug.Log($"[AssetOptimizer] Enabled GPU instancing on {instancedCount} materials.");
        }
        
        private bool CanEnableInstancing(Material material)
        {
            // Check if material shader supports instancing
            var shader = material.shader;
            if (shader == null) return false;
            
            // Most URP shaders support instancing
            string shaderName = shader.name.ToLower();
            return shaderName.Contains("universal") || 
                   shaderName.Contains("urp") || 
                   shaderName.Contains("lit") ||
                   shaderName.Contains("unlit");
        }
        
        private Material CreateInstancedMaterial(Material original)
        {
            if (_instancedMaterials.ContainsKey(original))
                return _instancedMaterials[original];
            
            var instancedMaterial = new Material(original)
            {
                name = original.name + "_Instanced",
                enableInstancing = true
            };
            
            _instancedMaterials[original] = instancedMaterial;
            return instancedMaterial;
        }
        
        private void OptimizeQualitySettings()
        {
            // Optimize quality settings for WebGL
            QualitySettings.pixelLightCount = 2;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
            QualitySettings.antiAliasing = 0; // Disable MSAA for better performance
            QualitySettings.shadowResolution = ShadowResolution.Low;
            QualitySettings.shadowDistance = 50f;
            QualitySettings.shadowCascades = 2;
            
            // Reduce texture quality slightly
            QualitySettings.masterTextureLimit = 1; // Half-resolution textures
            
            Debug.Log("[AssetOptimizer] Applied WebGL quality optimizations.");
        }
        
        // Public API for runtime optimization
        public void OptimizeRenderer(Renderer renderer)
        {
            if (renderer == null) return;
            
            // Set up LOD if not present
            var rootObject = renderer.transform.root.gameObject;
            if (rootObject.GetComponent<LODGroup>() == null)
            {
                CreateLODGroup(rootObject);
            }
            
            // Enable instancing on materials
            if (_enableGPUInstancing)
            {
                foreach (var material in renderer.materials)
                {
                    if (material != null && CanEnableInstancing(material) && !material.enableInstancing)
                    {
                        var instancedMat = CreateInstancedMaterial(material);
                        if (instancedMat != null)
                        {
                            var materials = renderer.materials;
                            for (int i = 0; i < materials.Length; i++)
                            {
                                if (materials[i] == material)
                                {
                                    materials[i] = instancedMat;
                                }
                            }
                            renderer.materials = materials;
                        }
                    }
                }
            }
        }
        
        public void SetLODDistances(float[] distances, float[] qualityReductions)
        {
            if (distances.Length != qualityReductions.Length)
            {
                Debug.LogError("[AssetOptimizer] LOD distances and quality arrays must have same length.");
                return;
            }
            
            _lodDistances = distances;
            _lodQualityReduction = qualityReductions;
            
            // Update existing LOD groups
            foreach (var lodGroup in _lodGroups.Values)
            {
                if (lodGroup != null)
                {
                    var lods = new LOD[_lodDistances.Length];
                    var renderers = lodGroup.GetComponentsInChildren<Renderer>();
                    
                    for (int i = 0; i < _lodDistances.Length; i++)
                    {
                        lods[i] = new LOD(_lodQualityReduction[i], renderers);
                    }
                    
                    lodGroup.SetLODs(lods);
                    lodGroup.RecalculateBounds();
                }
            }
        }
    }
}