using UnityEngine;
using UnityEngine.InputSystem;

namespace RedAlert.Core
{
    /// <summary>
    /// RTS-style camera controller with WASD movement, mouse pan, and zoom.
    /// Supports camera bounds to constrain movement within map limits.
    /// Optimized for WebGL with smooth movement and configurable speeds.
    /// </summary>
    public class RTSCameraController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 10f;
        [SerializeField] private float _panSpeed = 2f;
        [SerializeField] private float _rotateSpeed = 50f;
        
        [Header("Zoom")]
        [SerializeField] private float _zoomSpeed = 5f;
        [SerializeField] private float _minZoom = 5f;
        [SerializeField] private float _maxZoom = 50f;
        
        [Header("Bounds")]
        [SerializeField] private Bounds _cameraBounds = new Bounds(Vector3.zero, new Vector3(64f, 0f, 64f));
        
        [Header("Input")]
        [SerializeField] private bool _invertMouseY = false;
        [SerializeField] private float _edgePanThreshold = 10f;
        [SerializeField] private bool _enableEdgePan = true;
        
        private Camera _camera;
        private Vector2 _moveInput;
        private Vector2 _mousePosition;
        private Vector2 _lastMousePosition;
        private bool _isMousePanning;
        private bool _isRotating;
        
        // Performance optimization: cache transform
        private Transform _cameraTransform;
        
        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _cameraTransform = transform;
            
            if (_camera == null)
            {
                Debug.LogError("RTSCameraController requires a Camera component!");
                enabled = false;
            }
        }
        
        private void Start()
        {
            // Ensure camera starts within bounds
            ClampCameraPosition();
        }
        
        private void Update()
        {
            HandleKeyboardMovement();
            HandleMouseInput();
            HandleZoom();
            HandleEdgePan();
            
            // Apply bounds clamping after all movement
            ClampCameraPosition();
        }
        
        private void HandleKeyboardMovement()
        {
            Vector3 movement = Vector3.zero;
            
            // WASD movement
            if (Input.GetKey(KeyCode.W)) movement += Vector3.forward;
            if (Input.GetKey(KeyCode.S)) movement += Vector3.back;
            if (Input.GetKey(KeyCode.A)) movement += Vector3.left;
            if (Input.GetKey(KeyCode.D)) movement += Vector3.right;
            
            // Transform movement to world space based on camera rotation
            movement = _cameraTransform.TransformDirection(movement);
            movement.y = 0; // Keep movement horizontal
            movement = movement.normalized;
            
            // Apply movement
            if (movement != Vector3.zero)
            {
                _cameraTransform.position += movement * _moveSpeed * Time.deltaTime;
            }
        }
        
        private void HandleMouseInput()
        {
            _mousePosition = Input.mousePosition;
            
            // Middle mouse button panning
            if (Input.GetMouseButtonDown(2))
            {
                _isMousePanning = true;
                _lastMousePosition = _mousePosition;
            }
            else if (Input.GetMouseButtonUp(2))
            {
                _isMousePanning = false;
            }
            
            // Right mouse button rotation
            if (Input.GetMouseButtonDown(1))
            {
                _isRotating = true;
                _lastMousePosition = _mousePosition;
                Cursor.lockState = CursorLockMode.Locked;
            }
            else if (Input.GetMouseButtonUp(1))
            {
                _isRotating = false;
                Cursor.lockState = CursorLockMode.None;
            }
            
            // Handle mouse panning
            if (_isMousePanning)
            {
                Vector2 mouseDelta = _mousePosition - _lastMousePosition;
                
                // Convert screen movement to world movement
                Vector3 worldDelta = _camera.ScreenToWorldPoint(new Vector3(mouseDelta.x, mouseDelta.y, _camera.nearClipPlane));
                worldDelta = _cameraTransform.InverseTransformDirection(worldDelta);
                
                _cameraTransform.position -= new Vector3(worldDelta.x, 0, worldDelta.z) * _panSpeed;
                _lastMousePosition = _mousePosition;
            }
            
            // Handle mouse rotation
            if (_isRotating)
            {
                Vector2 mouseDelta = _mousePosition - _lastMousePosition;
                
                float rotationY = mouseDelta.x * _rotateSpeed * Time.deltaTime;
                float rotationX = -mouseDelta.y * _rotateSpeed * Time.deltaTime;
                
                if (_invertMouseY) rotationX = -rotationX;
                
                _cameraTransform.Rotate(rotationX, rotationY, 0);
                _lastMousePosition = _mousePosition;
            }
        }
        
        private void HandleZoom()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                // Move camera forward/backward for perspective zoom
                Vector3 forward = _cameraTransform.forward;
                forward.y = 0; // Keep zoom horizontal
                forward = forward.normalized;
                
                float zoomAmount = scroll * _zoomSpeed;
                Vector3 newPosition = _cameraTransform.position + forward * zoomAmount;
                
                // Clamp zoom by distance from ground
                float currentHeight = _cameraTransform.position.y;
                if (currentHeight >= _minZoom && currentHeight <= _maxZoom)
                {
                    _cameraTransform.position = newPosition;
                }
                else if (currentHeight < _minZoom && zoomAmount < 0)
                {
                    _cameraTransform.position = newPosition;
                }
                else if (currentHeight > _maxZoom && zoomAmount > 0)
                {
                    _cameraTransform.position = newPosition;
                }
            }
        }
        
        private void HandleEdgePan()
        {
            if (!_enableEdgePan || _isMousePanning || _isRotating) return;
            
            Vector2 screenMousePos = Input.mousePosition;
            Vector3 panDirection = Vector3.zero;
            
            // Check screen edges
            if (screenMousePos.x <= _edgePanThreshold)
                panDirection += Vector3.left;
            else if (screenMousePos.x >= Screen.width - _edgePanThreshold)
                panDirection += Vector3.right;
                
            if (screenMousePos.y <= _edgePanThreshold)
                panDirection += Vector3.back;
            else if (screenMousePos.y >= Screen.height - _edgePanThreshold)
                panDirection += Vector3.forward;
            
            // Apply edge panning
            if (panDirection != Vector3.zero)
            {
                panDirection = _cameraTransform.TransformDirection(panDirection);
                panDirection.y = 0;
                panDirection = panDirection.normalized;
                
                _cameraTransform.position += panDirection * _moveSpeed * 0.5f * Time.deltaTime;
            }
        }
        
        private void ClampCameraPosition()
        {
            Vector3 pos = _cameraTransform.position;
            
            // Clamp X and Z to bounds
            pos.x = Mathf.Clamp(pos.x, _cameraBounds.min.x, _cameraBounds.max.x);
            pos.z = Mathf.Clamp(pos.z, _cameraBounds.min.z, _cameraBounds.max.z);
            
            // Clamp Y for zoom limits
            pos.y = Mathf.Clamp(pos.y, _minZoom, _maxZoom);
            
            _cameraTransform.position = pos;
        }
        
        /// <summary>
        /// Set the camera bounds for map constraints
        /// </summary>
        public void SetCameraBounds(Bounds bounds)
        {
            _cameraBounds = bounds;
        }
        
        /// <summary>
        /// Focus camera on a specific world position
        /// </summary>
        public void FocusOn(Vector3 worldPosition)
        {
            Vector3 targetPos = worldPosition;
            targetPos.y = _cameraTransform.position.y; // Maintain current height
            
            _cameraTransform.position = targetPos;
            ClampCameraPosition();
        }
        
        /// <summary>
        /// Set zoom level (height above ground)
        /// </summary>
        public void SetZoom(float zoom)
        {
            Vector3 pos = _cameraTransform.position;
            pos.y = Mathf.Clamp(zoom, _minZoom, _maxZoom);
            _cameraTransform.position = pos;
        }
        
        private void OnDrawGizmos()
        {
            // Draw camera bounds in editor
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(_cameraBounds.center, _cameraBounds.size);
        }
    }
}