using UnityEngine;
using System.Collections;

/// <summary>
/// 🏏 Target Dragger for Cricket Pitch
/// Allows moving target with touch (mobile) and mouse drag (PC)
/// Movement: Up=Towards batsmen, Down=Towards umpire, Left/Right=Sideways
/// </summary>
public class TargetDragger : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 15f; // How fast the target moves (increased for smoothness)
    [SerializeField] private float dragSensitivity = 3f; // Mouse/touch sensitivity (increased for faster response)
    
    [Header("Boundary Settings")]
    [SerializeField] private float maxUpDistance = 10f; // Max distance towards batsmen (fallback)
    [SerializeField] private float maxDownDistance = 5f; // Max distance towards umpire (fallback)
    [SerializeField] private float maxSideDistance = 3f; // Max sideways movement (fallback)
    [SerializeField] private bool usePitchingAreaBounds = true; // Use Pitching Area collider for boundaries
    
    [Header("Input Settings")]
    [SerializeField] private bool enableMouseInput = true; // Enable mouse dragging
    [SerializeField] private bool enableTouchInput = true; // Enable touch dragging
    [SerializeField] private float touchDeadZone = 0.1f; // Minimum touch movement to register
    
    [Header("Raycast Settings")]
    [SerializeField] private LayerMask targetLayerMask = -1; // Layer mask for target detection
    [SerializeField] private bool useLayerMask = false; // Use layer mask instead of collider filtering
    [SerializeField] private float raycastMaxDistance = 100f; // Maximum raycast distance
    
    // Private variables
    private Vector3 originalPosition; // Starting position of target
    private Vector3 targetPosition; // Where target should move to
    private bool isDragging = false;
    private Vector3 lastInputPosition;
    private Camera mainCamera;
    
    // Cricket pitch references
    private Transform umpireWicket; // Wicket closer to umpire
    private Transform batsmenWicket; // Wicket closer to batsmen
    
    // Pitching Area boundary reference
    private BoxCollider pitchingAreaBounds; // Reference to Pitching Area collider
    private Vector3 pitchingAreaCenter; // Center of pitching area
    private Vector3 pitchingAreaSize; // Size of pitching area
    
    void Start()
    {
        // Store original position
        originalPosition = transform.position;
        targetPosition = originalPosition;
        
        // Get main camera
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }
        
        // 🎯 CRITICAL: Ensure target has a collider for raycast detection
        EnsureTargetHasCollider();
        
        // 🎯 OPTIONAL: Set up target layer for better raycast filtering
        SetupTargetLayer();
        
        // 🎯 CRITICAL: Find and set up Pitching Area boundaries
        FindPitchingAreaBounds();
        
        // Find wickets (you can assign these in inspector or find them automatically)
        FindWickets();
        
       // Debug.Log("🏏 Target Dragger initialized!");
        //Debug.Log($"🏏 Original position: {originalPosition}");
        //Debug.Log($"🏏 Movement boundaries: Up={maxUpDistance}m, Down={maxDownDistance}m, Side={maxSideDistance}m");
    }
    
    void Update()
    {
        // Handle mouse input
        if (enableMouseInput)
        {
            HandleMouseInput();
        }
        
        // Handle touch input
        if (enableTouchInput)
        {
            HandleTouchInput();
        }
        
        // Smooth movement towards target position (much faster and smoother)
        if (Vector3.Distance(transform.position, targetPosition) > 0.001f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        }
    }
    
    void FixedUpdate()
    {
        // 🎯 ENHANCED: More frequent input handling for smoother dragging
        if (isDragging)
        {
            // Handle continuous mouse input for smoother dragging
            if (enableMouseInput && Input.GetMouseButton(0))
            {
                ContinueDragging(Input.mousePosition);
            }
        }
    }
    
    /// <summary>
    /// Handle mouse input for dragging
    /// </summary>
    void HandleMouseInput()
    {
        // Mouse button down - start dragging
        if (Input.GetMouseButtonDown(0))
        {
            StartDragging(Input.mousePosition);
        }
        
        // Mouse button held - continue dragging
        if (Input.GetMouseButton(0) && isDragging)
        {
            ContinueDragging(Input.mousePosition);
        }
        
        // Mouse button up - stop dragging
        if (Input.GetMouseButtonUp(0))
        {
            StopDragging();
        }
    }
    
    /// <summary>
    /// Handle touch input for mobile
    /// </summary>
    void HandleTouchInput()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    StartDragging(touch.position);
                    break;
                    
                case TouchPhase.Moved:
                    if (isDragging)
                    {
                        ContinueDragging(touch.position);
                    }
                    break;
                    
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    StopDragging();
                    break;
            }
        }
    }
    
    /// <summary>
    /// Start dragging operation with proper raycast detection
    /// </summary>
    void StartDragging(Vector3 inputPosition)
    {
        // 🎯 ENHANCED: Use proper raycast to detect if we clicked on the target
        if (IsClickOnTarget(inputPosition))
        {
            isDragging = true;
            lastInputPosition = inputPosition;
          //  Debug.Log("🏏 Started dragging target - raycast hit confirmed");
        }
        else
        {
           // Debug.Log("🏏 Click missed target - raycast didn't hit");
        }
    }
    
    /// <summary>
    /// Continue dragging operation
    /// </summary>
    void ContinueDragging(Vector3 inputPosition)
    {
        if (!isDragging) return;
        
        // Calculate input delta
        Vector3 inputDelta = inputPosition - lastInputPosition;
        
        // Convert to world movement
        Vector3 worldDelta = GetWorldDeltaFromInput(inputDelta);
        
        // Apply movement with constraints
        Vector3 newPosition = targetPosition + worldDelta;
        targetPosition = ConstrainPosition(newPosition);
        
        // Update last input position
        lastInputPosition = inputPosition;
        
     //   Debug.Log($"🏏 Dragging target to: {targetPosition}");
    }
    
    /// <summary>
    /// Stop dragging operation
    /// </summary>
    void StopDragging()
    {
        if (isDragging)
        {
            isDragging = false;
          //  Debug.Log("🏏 Stopped dragging target");
        }
    }
    
    /// <summary>
    /// 🎯 ENHANCED: Check if click/touch is actually on the target using filtered raycast
    /// </summary>
    bool IsClickOnTarget(Vector3 inputPosition)
    {
        Ray ray = mainCamera.ScreenPointToRay(inputPosition);
        RaycastHit hit;
        
        // 🎯 PRECISE: Cast ray with filtering to only hit the target
        if (useLayerMask)
        {
            // Use layer mask filtering
            if (Physics.Raycast(ray, out hit, raycastMaxDistance, targetLayerMask))
            {
                // Check if the hit object is this target
                if (hit.collider.gameObject == gameObject)
                {
                   // Debug.Log($"🏏 Raycast hit target at: {hit.point} (Layer filtered)");
                    return true;
                }
                else
                {
                  //  Debug.Log($"🏏 Raycast hit other object in target layer: {hit.collider.gameObject.name}");
                    return false;
                }
            }
        }
        else
        {
            // Use collider filtering - cast ray to all objects but filter by target
            if (Physics.Raycast(ray, out hit, raycastMaxDistance))
            {
                // Check if the hit object is this target
                if (hit.collider.gameObject == gameObject)
                {
                   // Debug.Log($"🏏 Raycast hit target at: {hit.point} (Collider filtered)");
                    return true;
                }
                else
                {
                  //  Debug.Log($"🏏 Raycast hit other object: {hit.collider.gameObject.name} - ignoring");
                    // Continue raycasting to find target behind other objects
                    return ContinueRaycastToTarget(ray, hit);
                }
            }
        }
        
      //  Debug.Log("🏏 Raycast didn't hit anything");
        return false;
    }
    
    /// <summary>
    /// Convert screen input position to world position (kept for compatibility)
    /// </summary>
    Vector3 GetWorldPositionFromInput(Vector3 inputPosition)
    {
        Ray ray = mainCamera.ScreenPointToRay(inputPosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        
        if (groundPlane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }
        
        return Vector3.zero;
    }
    
    /// <summary>
    /// 🎯 ENHANCED: Continue raycasting through other objects to find target
    /// </summary>
    bool ContinueRaycastToTarget(Ray ray, RaycastHit previousHit)
    {
        // Start from the previous hit point and continue raycasting
        Vector3 startPoint = previousHit.point + ray.direction * 0.01f; // Slightly past the previous hit
        Ray newRay = new Ray(startPoint, ray.direction);
        
        // Continue raycasting with remaining distance
        float remainingDistance = raycastMaxDistance - Vector3.Distance(ray.origin, startPoint);
        
        if (remainingDistance > 0)
        {
            RaycastHit newHit;
            if (Physics.Raycast(newRay, out newHit, remainingDistance))
            {
                // Check if this hit is our target
                if (newHit.collider.gameObject == gameObject)
                {
                 //   Debug.Log($"🏏 Continued raycast hit target at: {newHit.point} (through other objects)");
                    return true;
                }
                else
                {
                 //   Debug.Log($"🏏 Continued raycast hit: {newHit.collider.gameObject.name} - continuing...");
                    // Recursively continue raycasting
                    return ContinueRaycastToTarget(ray, newHit);
                }
            }
        }
        
     //   Debug.Log("🏏 Raycast didn't find target after going through other objects");
        return false;
    }
    
    /// <summary>
    /// Convert screen input delta to world movement delta
    /// </summary>
    Vector3 GetWorldDeltaFromInput(Vector3 inputDelta)
    {
        // Convert screen delta to world movement
        Vector3 worldDelta = Vector3.zero;
        
        // 🎯 FIXED: Y movement (up/down on screen) = forward/backward on pitch
        // Up on screen = towards batsmen (Z+), Down on screen = towards umpire (Z-)
        worldDelta.z = inputDelta.y * dragSensitivity * 0.02f; // Removed negative sign, increased sensitivity
        
        // X movement (left/right on screen) = left/right on pitch (working fine, don't change)
        worldDelta.x = inputDelta.x * dragSensitivity * 0.02f; // Increased sensitivity for faster response
        
        return worldDelta;
    }
    
    /// <summary>
    /// Constrain target position within boundaries
    /// </summary>
    Vector3 ConstrainPosition(Vector3 newPosition)
    {
        Vector3 constrainedPosition = newPosition;
        
        if (usePitchingAreaBounds && pitchingAreaBounds != null)
        {
            // 🎯 ENHANCED: Use Pitching Area boundaries for precise constraints
            constrainedPosition = ConstrainToPitchingArea(newPosition);
        }
        else
        {
            // Fallback to manual boundary constraints
            // Constrain forward/backward movement (Z-axis)
            float forwardDistance = newPosition.z - originalPosition.z;
            if (forwardDistance > maxUpDistance) // Towards batsmen
            {
                constrainedPosition.z = originalPosition.z + maxUpDistance;
            }
            else if (forwardDistance < -maxDownDistance) // Towards umpire
            {
                constrainedPosition.z = originalPosition.z - maxDownDistance;
            }
            
            // Constrain sideways movement (X-axis)
            float sideDistance = Mathf.Abs(newPosition.x - originalPosition.x);
            if (sideDistance > maxSideDistance)
            {
                float sign = (newPosition.x > originalPosition.x) ? 1f : -1f;
                constrainedPosition.x = originalPosition.x + (maxSideDistance * sign);
            }
        }
        
        // Keep Y position constant (on ground)
        constrainedPosition.y = originalPosition.y;
        
        return constrainedPosition;
    }
    
    /// <summary>
    /// 🎯 ENHANCED: Constrain target position to Pitching Area boundaries
    /// </summary>
    Vector3 ConstrainToPitchingArea(Vector3 newPosition)
    {
        Vector3 constrainedPosition = newPosition;
        
        // Calculate Pitching Area boundaries in world space
        Vector3 minBound = pitchingAreaCenter - (pitchingAreaSize * 0.5f);
        Vector3 maxBound = pitchingAreaCenter + (pitchingAreaSize * 0.5f);
        
        // Constrain X (sideways movement)
        constrainedPosition.x = Mathf.Clamp(newPosition.x, minBound.x, maxBound.x);
        
        // Constrain Z (forward/backward movement)
        constrainedPosition.z = Mathf.Clamp(newPosition.z, minBound.z, maxBound.z);
        
        // Keep Y position constant (on ground)
        constrainedPosition.y = originalPosition.y;
        
        // Debug boundary constraints
        if (newPosition != constrainedPosition)
        {
         //   Debug.Log($"🏏 Target constrained to Pitching Area:");
           // Debug.Log($"🏏 Requested: {newPosition}");
            //Debug.Log($"🏏 Constrained: {constrainedPosition}");
            //Debug.Log($"🏏 Pitching Area bounds: {minBound} to {maxBound}");
        }
        
        return constrainedPosition;
    }
    
    /// <summary>
    /// 🎯 CRITICAL: Ensure target has a collider for raycast detection
    /// </summary>
    void EnsureTargetHasCollider()
    {
        Collider targetCollider = GetComponent<Collider>();
        if (targetCollider == null)
        {
            // Add a box collider if none exists
            BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
            boxCollider.size = new Vector3(1f, 0.1f, 1f); // Flat box for ground target
            boxCollider.center = Vector3.zero;
         //   Debug.Log("🏏 Added BoxCollider to target for raycast detection");
        }
        else
        {
         //   Debug.Log($"🏏 Target already has collider: {targetCollider.GetType().Name}");
        }
    }
    
    /// <summary>
    /// 🎯 OPTIONAL: Set up target layer for better raycast filtering
    /// </summary>
    void SetupTargetLayer()
    {
        if (useLayerMask)
        {
            // Check if target is on the correct layer
            if (gameObject.layer != targetLayerMask.value)
            {
                // Try to set target to a specific layer for filtering
                // You can create a custom layer called "Target" in Unity
              //  Debug.Log($"🏏 Target layer: {LayerMask.LayerToName(gameObject.layer)}");
              //  Debug.Log($"🏏 Target layer mask: {targetLayerMask.value}");
                
                // Optional: Auto-set to a specific layer if available
                int targetLayer = LayerMask.NameToLayer("Target");
                if (targetLayer != -1)
                {
                    gameObject.layer = targetLayer;
                    targetLayerMask = 1 << targetLayer; // Set layer mask to only this layer
                //    Debug.Log("🏏 Auto-set target to 'Target' layer for better filtering");
                }
                else
                {
                 //   Debug.LogWarning("🏏 'Target' layer not found. Create a layer called 'Target' for better filtering.");
                }
            }
        }
    }
    
    /// <summary>
    /// 🎯 CRITICAL: Find and set up Pitching Area boundaries for target constraints
    /// </summary>
    void FindPitchingAreaBounds()
    {
        if (usePitchingAreaBounds)
        {
            // Try to find Pitching Area by name
            GameObject pitchingArea = GameObject.Find("Pitching Area");
            if (pitchingArea != null)
            {
                pitchingAreaBounds = pitchingArea.GetComponent<BoxCollider>();
                if (pitchingAreaBounds != null)
                {
                    // Calculate world-space boundaries
                    pitchingAreaCenter = pitchingArea.transform.position;
                    pitchingAreaSize = Vector3.Scale(pitchingAreaBounds.size, pitchingArea.transform.localScale);
                    
                //    Debug.Log($"🏏 Found Pitching Area bounds:");
               //     Debug.Log($"🏏 Center: {pitchingAreaCenter}");
                 //   Debug.Log($"🏏 Size: {pitchingAreaSize}");
                   // Debug.Log($"🏏 Using Pitching Area boundaries for target constraints");
                    
                    // Update movement limits based on pitching area
                    UpdateMovementLimitsFromPitchingArea();
                }
                else
                {
                    //Debug.LogWarning("🏏 Pitching Area found but no BoxCollider component!");
                }
            }
            else
            {
                //Debug.LogWarning("🏏 Pitching Area not found! Using fallback boundaries.");
            }
        }
        else
        {
            //Debug.Log("🏏 Using manual boundary settings (Pitching Area bounds disabled)");
        }
    }
    
    /// <summary>
    /// Update movement limits based on Pitching Area boundaries
    /// </summary>
    void UpdateMovementLimitsFromPitchingArea()
    {
        if (pitchingAreaBounds != null)
        {
            // Calculate boundaries relative to original target position
            float halfWidth = pitchingAreaSize.x * 0.5f;
            float halfLength = pitchingAreaSize.z * 0.5f;
            
            // Set movement limits based on pitching area size
            maxSideDistance = halfWidth;
            maxUpDistance = halfLength;
            maxDownDistance = halfLength;
            
            // Movement limits updated
        }
    }
    
    /// <summary>
    /// Find wicket references automatically
    /// </summary>
    void FindWickets()
    {
        // Try to find wickets by name
        GameObject[] wickets = GameObject.FindGameObjectsWithTag("Wicket");
        if (wickets.Length >= 2)
        {
            // Assume first wicket is umpire side, second is batsmen side
            umpireWicket = wickets[0].transform;
            batsmenWicket = wickets[1].transform;
            
            // Wickets found
        }
        else
        {
            // Fallback: search by name
            umpireWicket = GameObject.Find("Wicket")?.transform;
            batsmenWicket = GameObject.Find("Wicket (1)")?.transform;
            
            if (umpireWicket != null && batsmenWicket != null)
            {
                // Wickets found by name
            }
            else
            {
                Debug.LogWarning("🏏 Could not find wicket references - using default boundaries");
            }
        }
    }
    
    /// <summary>
    /// Reset target to original position
    /// </summary>
    public void ResetToOriginalPosition()
    {
        targetPosition = originalPosition;
        // Target reset to original position
    }
    
    /// <summary>
    /// Move target to specific position (for testing)
    /// </summary>
    public void MoveToPosition(Vector3 newPosition)
    {
        targetPosition = ConstrainPosition(newPosition);
        // Target moved to new position
    }
    
    /// <summary>
    /// Get current target position
    /// </summary>
    public Vector3 GetCurrentPosition()
    {
        return transform.position;
    }
    
    /// <summary>
    /// Get target position (where target is moving to)
    /// </summary>
    public Vector3 GetTargetPosition()
    {
        return targetPosition;
    }
    
    /// <summary>
    /// Check if target is currently being dragged
    /// </summary>
    public bool IsDragging()
    {
        return isDragging;
    }
    
    // Context menu for testing
    [ContextMenu("Reset Position")]
    void ResetPositionContext()
    {
        ResetToOriginalPosition();
    }
    
    [ContextMenu("Move Towards Batsmen")]
    void MoveTowardsBatsmenContext()
    {
        Vector3 newPos = originalPosition + Vector3.forward * maxUpDistance;
        MoveToPosition(newPos);
    }
    
    [ContextMenu("Move Towards Umpire")]
    void MoveTowardsUmpireContext()
    {
        Vector3 newPos = originalPosition + Vector3.back * maxDownDistance;
        MoveToPosition(newPos);
    }
    
    /// <summary>
    /// 🎯 DEBUG: Draw Pitching Area boundaries in Scene view
    /// </summary>
    void OnDrawGizmos()
    {
        if (usePitchingAreaBounds && pitchingAreaBounds != null)
        {
            // Draw Pitching Area boundaries
            Gizmos.color = Color.green;
            Gizmos.matrix = pitchingAreaBounds.transform.localToWorldMatrix;
            Gizmos.DrawWireCube(pitchingAreaBounds.center, pitchingAreaBounds.size);
            
            // Draw target movement area
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
            
            // Draw movement limits
            if (Application.isPlaying)
            {
                Gizmos.color = Color.red;
                Vector3 minBound = pitchingAreaCenter - (pitchingAreaSize * 0.5f);
                Vector3 maxBound = pitchingAreaCenter + (pitchingAreaSize * 0.5f);
                Gizmos.DrawLine(minBound, maxBound);
            }
        }
    }
    
    /// <summary>
    /// Set the input camera for raycasting (used by state machine)
    /// </summary>
    public void SetInputCamera(Camera cam)
    {
        if (cam != null)
        {
            mainCamera = cam;
        }
    }
}
