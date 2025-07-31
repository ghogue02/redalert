using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Collections;
using RedAlert.Core;

namespace RedAlert.Pathing
{
    /// <summary>
    /// Provides batched path requests and results to locomotion agents.
    /// Implements NavMesh pathfinding with WebGL-optimized batching to avoid frame spikes.
    /// Processes path requests at 4Hz to maintain performance.
    /// </summary>
    public class PathService : MonoBehaviour, UpdateDriver.ISlowTick
    {
        [Header("Performance")]
        [SerializeField] private int _maxPathsPerFrame = 5;
        [SerializeField] private float _pathRequestTimeout = 2f;
        
        private Queue<PathRequest> _pathRequestQueue = new Queue<PathRequest>();
        private Dictionary<NavMeshAgent, PathRequest> _activeRequests = new Dictionary<NavMeshAgent, PathRequest>();
        
        private struct PathRequest
        {
            public NavMeshAgent agent;
            public Vector3 destination;
            public float requestTime;
            
            public PathRequest(NavMeshAgent agent, Vector3 destination)
            {
                this.agent = agent;
                this.destination = destination;
                this.requestTime = Time.time;
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
        
        /// <summary>
        /// Attempt to enqueue a path request. Returns true if enqueued, false if should use direct pathfinding.
        /// </summary>
        public bool TryEnqueueSetDestination(NavMeshAgent agent, Vector3 destination)
        {
            if (agent == null || !agent.isActiveAndEnabled)
                return false;
                
            // If agent already has an active request, update it
            if (_activeRequests.ContainsKey(agent))
            {
                var existing = _activeRequests[agent];
                existing.destination = destination;
                existing.requestTime = Time.time;
                _activeRequests[agent] = existing;
                return true;
            }
            
            // Add new request to queue
            var request = new PathRequest(agent, destination);
            _pathRequestQueue.Enqueue(request);
            _activeRequests[agent] = request;
            
            return true;
        }
        
        /// <summary>
        /// Process path requests at 4Hz to maintain performance
        /// </summary>
        public void SlowTick()
        {
            ProcessPathRequests();
            CleanupTimedOutRequests();
        }
        
        private void ProcessPathRequests()
        {
            int processedThisFrame = 0;
            
            while (_pathRequestQueue.Count > 0 && processedThisFrame < _maxPathsPerFrame)
            {
                var request = _pathRequestQueue.Dequeue();
                
                // Validate agent is still valid
                if (request.agent == null || !request.agent.isActiveAndEnabled)
                {
                    _activeRequests.Remove(request.agent);
                    processedThisFrame++;
                    continue;
                }
                
                // Set destination
                try
                {
                    request.agent.SetDestination(request.destination);
                    _activeRequests.Remove(request.agent);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"PathService: Failed to set destination for {request.agent.name}: {e.Message}");
                    _activeRequests.Remove(request.agent);
                }
                
                processedThisFrame++;
            }
        }
        
        private void CleanupTimedOutRequests()
        {
            var currentTime = Time.time;
            var keysToRemove = new List<NavMeshAgent>();
            
            foreach (var kvp in _activeRequests)
            {
                if (currentTime - kvp.Value.requestTime > _pathRequestTimeout)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            
            foreach (var key in keysToRemove)
            {
                _activeRequests.Remove(key);
            }
        }
        
        /// <summary>
        /// Get the number of pending path requests
        /// </summary>
        public int GetPendingRequestCount()
        {
            return _pathRequestQueue.Count;
        }
        
        /// <summary>
        /// Check if an agent has a pending path request
        /// </summary>
        public bool HasPendingRequest(NavMeshAgent agent)
        {
            return _activeRequests.ContainsKey(agent);
        }
        
        /// <summary>
        /// Cancel a pending path request for an agent
        /// </summary>
        public void CancelRequest(NavMeshAgent agent)
        {
            _activeRequests.Remove(agent);
        }
        
        /// <summary>
        /// Clear all pending path requests
        /// </summary>
        public void ClearAllRequests()
        {
            _pathRequestQueue.Clear();
            _activeRequests.Clear();
        }
    }
}