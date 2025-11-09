using UnityEngine;
using CricketGame;

public class PlayerAnimationController : MonoBehaviour
{
    [Header("Bowling Controller Reference")]
    public BowlingController bowlingController;
    
    [Header("Animation Settings")]
    [SerializeField] private bool enableDebugLogs = true;
    
    [Header("Spawn Point Settings")]
    [SerializeField] private Transform animationSpawnPoint;
    
    [Header("Trigger Detection")]
    [SerializeField, Tooltip("Tag name for trigger box (leave empty to detect any trigger)")] private string triggerBoxTag = "";
    [SerializeField, Tooltip("Stop camera when entering trigger box")] private bool enableTriggerStopCamera = true;
    
    [Header("Target Hide Settings")]
    [SerializeField, Tooltip("Tag for Target GameObject")] private string targetTag = "Target";
    [SerializeField, Tooltip("Child name to hide (e.g., 'Sides' or 'Slides')")] private string sidesChildName = "Slides";
    [SerializeField, Tooltip("Duration to shrink/hide target")] private float hideDuration = 0.3f;
    [SerializeField, Tooltip("Duration to show/restore target")] private float showDuration = 0.3f;
    
    [Header("Y Rotation Control")]
    [SerializeField, Tooltip("Enable to allow random Y rotation variation between min and max degrees. Disable to keep Y rotation at 0.")]
    private bool enableYawJitter = false;
    [SerializeField, Tooltip("Minimum Y rotation in degrees (for bowling state random range)")]
    [Range(-5f, 0f)] private float minYRotation = -0.4f;
    [SerializeField, Tooltip("Maximum Y rotation in degrees (for bowling state random range)")]
    [Range(0f, 5f)] private float maxYRotation = 0.4f;
    [SerializeField, Tooltip("Target Y rotation value (set automatically if jitter enabled, or 0 if disabled)")]
    private float targetYRotation = 0f;
    
    // CRITICAL FIX: Store the last animated position to ensure consistency
    private Vector3 lastAnimatedSpawnPosition;
    private bool hasValidAnimatedPosition = false;
    
    // Target hide state
    private Transform targetSidesTransform;
    private Vector3 originalSidesScale;
    private bool originalScaleStored = false;
    private Coroutine currentScaleCoroutine;
    
    private Transform cachedTransform;
    
    void Awake()
    {
        InitializeReferences();
        EnsureTriggerDetectionSetup();
        
        // Initialize target Y rotation to 0
        targetYRotation = 0f;
        cachedTransform = transform;
        
        // CRITICAL FIX: Always refresh spawn point reference to ensure we're using scene instance, not prefab
        if (animationSpawnPoint != null)
        {
            ForceRefreshSpawnPointReference();
        }
    }
    /// <summary>
    /// Ensure trigger detection will work at runtime:
    /// - The bowler should have a NON-trigger collider.
    /// - The Trigger Box should carry a kinematic Rigidbody (recommended) to raise trigger messages.
    /// We DO NOT add a Rigidbody to the bowler to avoid interfering with core movement.
    /// </summary>
    private void EnsureTriggerDetectionSetup()
    {
        // Ensure this object has a non-trigger collider (the trigger box is the trigger)
        Collider selfCollider = GetComponent<Collider>();
        if (selfCollider != null && selfCollider.isTrigger)
        {
            selfCollider.isTrigger = false;
        }
    }
    
    void OnEnable()
    {
        // Backup initialization in case Awake didn't work
        if (bowlingController == null || animationSpawnPoint == null)
        {
            InitializeReferences();
        }
        
        // Subscribe to next ball event
        CricketGame.BowlerEvents.OnNextBallReady += HandleNextBallReady;
    }
    
    void OnDisable()
    {
        // Unsubscribe from events
        CricketGame.BowlerEvents.OnNextBallReady -= HandleNextBallReady;
    }
    
    /// <summary>
    /// Handle next ball ready event - restore target visibility
    /// </summary>
    private void HandleNextBallReady()
    {
        ShowTargetSides();
    }
    
    void InitializeReferences()
    {
        // Auto-find BowlingController if not assigned
        if (bowlingController == null)
        {
            bowlingController = FindObjectOfType<BowlingController>();
        }
        
        // Auto-find Animation Spawn Point if not assigned
        if (animationSpawnPoint == null)
        {
            // Try to find RightHand in this GameObject's children
            Transform rightHand = transform.Find("RightHand");
            if (rightHand == null)
            {
                // Try to find any hand-related transform
                rightHand = transform.Find("Right Hand");
            }
            if (rightHand == null)
            {
                // Try to find any hand transform recursively
                rightHand = FindChildRecursive(transform, "RightHand");
            }
            if (rightHand == null)
            {
                rightHand = FindChildRecursive(transform, "Right Hand");
            }
            
            if (rightHand != null)
            {
                animationSpawnPoint = rightHand;
            }
        }
        
        // Validate setup
    }

    /// <summary>
    /// Called by animation event when ball is released from bowler's hand
    /// This function handles both ball creation and bowling in one call
    /// For looping animations, this will create a new ball each time
    /// </summary>
    /// <summary>
    /// Animation event called when ball is released from bowler's hand
    /// OPTIMIZED: Frame-distributed execution to prevent lag
    /// </summary>
    public void OnBallReleased()
    {
        if (bowlingController == null)
        {
            return;
        }
        
        // CRITICAL: Store position immediately (light operation)
        Vector3 currentHandPosition = GetCurrentAnimatedSpawnPosition();
        lastAnimatedSpawnPosition = currentHandPosition;
        hasValidAnimatedPosition = true;
        
        // OPTIMIZED: Start frame-distributed sequence to prevent lag
        StartCoroutine(OptimizedBallReleaseSequence());
    }
    
    /// <summary>
    /// Frame-distributed ball release sequence to prevent performance spikes
    /// </summary>
    private System.Collections.IEnumerator OptimizedBallReleaseSequence()
    {
        // Frame 1: Create ball and hold it in hand
        yield return null;
        CreateBallAndHoldInHand();
        
        // Frame 2: Start bowling (heavy operation)
        yield return null;
        StartBowlingOptimized();
    }
    
    /// <summary>
    /// Create ball and ensure it stays in bowler's hand (Frame 1)
    /// </summary>
    private void CreateBallAndHoldInHand()
    {
        bowlingController.InstantiateNewBall();
        
        // CRITICAL: Ensure ball stays in hand by disabling physics temporarily
        GameObject ballInstance = bowlingController.GetCurrentBallInstance();
        if (ballInstance != null)
        {
            Rigidbody ballRigidbody = ballInstance.GetComponent<Rigidbody>();
            if (ballRigidbody != null)
            {
                ballRigidbody.isKinematic = true; // Disable physics to keep ball in hand
            }
        }
    }
    
    /// <summary>
    /// Start bowling with optimized performance (Frame 2)
    /// </summary>
    private void StartBowlingOptimized()
    {
        bowlingController.BowlCurrentBall();
    }
    
    /// <summary>
    /// Context menu to test OnBallReleased functionality
    /// </summary>
    [ContextMenu("Test OnBallReleased")]
    public void TestOnBallReleased()
    {
        OnBallReleased();
    }

    /// <summary>
    /// Context menu to check PlayerAnimationController status
    /// </summary>
    [ContextMenu("Check PlayerAnimationController Status")]
    public void CheckPlayerAnimationControllerStatus()
    {
        
    }

    [Header("Root Motion Settings")]
    [SerializeField] private bool enableManualRootMotion = false;
    [SerializeField] private float rootMotionDistance = 2f; // Distance to move forward during bowling
    [SerializeField] private float rootMotionDuration = 1f; // Duration of the movement
    
    private Vector3 originalPosition;
    private bool isMovingRoot = false;
    
    /// <summary>
    /// Called at the start of bowling animation to store original position
    /// </summary>
    public void OnAnimationStart()
    {
        if (enableManualRootMotion)
        {
            originalPosition = transform.position;
        }
    }
    
    /// <summary>
    /// Called during bowling animation to move root forward
    /// </summary>
    public void OnAnimationMidpoint()
    {
        if (enableManualRootMotion && !isMovingRoot)
        {
            StartCoroutine(MoveRootForward());
        }
    }
    
    /// <summary>
    /// Called at the end of bowling animation
    /// </summary>
    public void OnAnimationEnd()
    {
    }
    
    /// <summary>
    /// Coroutine to smoothly move the root forward during bowling
    /// </summary>
    private System.Collections.IEnumerator MoveRootForward()
    {
        isMovingRoot = true;
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + transform.forward * rootMotionDistance;
        
        float elapsed = 0f;
        while (elapsed < rootMotionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / rootMotionDuration;
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }
        
        transform.position = targetPos;
        isMovingRoot = false;
        
    }

    /// <summary>
    /// Alternative method to just create a ball without bowling (for setup animations)
    /// </summary>
    public void OnBallCreated()
    {
        
        if (bowlingController == null)
        {
            return;
        }
        
        bowlingController.InstantiateNewBall();
    }
    
    /// <summary>
    /// Called from animation event to destroy old ball before creating new one
    /// </summary>
    public void OnDestroyOldBall()
    {
        
        if (bowlingController == null)
        {
            return;
        }
        
        // Get current ball instance and destroy it
        GameObject currentBall = bowlingController.GetCurrentBallInstance();
        if (currentBall != null)
        {
            Destroy(currentBall);
        }
    }
    
    /// <summary>
    /// Alternative method to just bowl existing ball (for release animations)
    /// </summary>
    public void OnBallBowled()
    {
        
        if (bowlingController == null)
        {
            return;
        }
        
        bowlingController.BowlCurrentBall();
    }
    
    /// <summary>
    /// Smart ball release for looping animations - only creates new ball if previous one is done
    /// Use this for continuous bowling animations
    /// </summary>
    public void OnBallReleasedSmart()
    {
        
        if (bowlingController == null)
        {
            return;
        }
        
        // Check if we need to create a new ball (if none exists or previous one is done)
        if (bowlingController.GetCurrentBallInstance() == null)
        {
            bowlingController.InstantiateNewBall();
        }
        
        // Bowl the current ball
        bowlingController.BowlCurrentBall();
    }
    
    /// <summary>
    /// Get the animation-driven spawn point
    /// Now uses BowlingController's spawn mapping for the current bowler
    /// </summary>
    public Transform GetAnimationSpawnPoint()
    {
        // First try to get the current spawn position from BowlingController
        if (bowlingController != null)
        {
            Transform currentSpawn = bowlingController.GetCurrentBowlerSpawnPosition();
            if (currentSpawn != null)
            {
                return currentSpawn;
            }
        }
        
        // Fallback to the original animationSpawnPoint
        return animationSpawnPoint;
    }
    
    /// <summary>
    /// Stop any ongoing root motion movement (called during spawn position switching)
    /// </summary>
    public void StopRootMotion()
    {
        if (isMovingRoot)
        {
            StopCoroutine(MoveRootForward());
            isMovingRoot = false;
        }
    }

    /// <summary>
    /// Stop all movement coroutines (called during spawn position switching)
    /// </summary>
    public void StopAllMovement()
    {
        StopAllCoroutines();
        isMovingRoot = false;
    }

    /// <summary>
    /// Get the current animated position of the spawn point
    /// </summary>
    public Vector3 GetCurrentAnimatedSpawnPosition()
    {
        if (animationSpawnPoint != null)
        {
            // CRITICAL FIX: Always refresh to ensure we're using the scene instance's bone, not the prefab's bone
            RefreshSpawnPointReference();
            
            // Get the current world position of the animated bone
            Vector3 currentPosition = animationSpawnPoint.position;
            return currentPosition;
        }
        return Vector3.zero;
    }
    
    /// <summary>
    /// Refresh the spawn point position (call this when bowler moves)
    /// </summary>
    public void RefreshSpawnPointPosition()
    {
        if (animationSpawnPoint != null)
        {
            // CRITICAL FIX: Ensure we're using the scene instance's bone, not the prefab's bone
            RefreshSpawnPointReference();
            
            // The transform position should automatically update when the bone moves
            // Just log the current position for debugging
        }
    }
    
    /// <summary>
    /// Refresh the spawn point reference to ensure it's pointing to the scene instance, not the prefab
    /// </summary>
    private void RefreshSpawnPointReference()
    {
        if (animationSpawnPoint != null)
        {
            // ALWAYS refresh to ensure we're using the scene instance, not the prefab
            // This is critical because prefab references don't update when the scene instance moves
            
            string boneName = animationSpawnPoint.name;
            
            
            // CRITICAL FIX: Always find the scene instance bone, even if animationSpawnPoint is pointing to prefab
            Transform sceneInstanceBone = FindChildRecursive(transform, boneName);
            
            if (sceneInstanceBone != null)
            {
                // Check if the current reference is from a different scene (prefab vs instance)
                bool isFromPrefab = animationSpawnPoint.gameObject.scene.name != gameObject.scene.name;
                
                if (isFromPrefab || sceneInstanceBone != animationSpawnPoint)
                {
                    animationSpawnPoint = sceneInstanceBone;
                }
            }
        }
    }
    
    /// <summary>
    /// Debug method to check spawn point status
    /// </summary>
    [ContextMenu("Check Animation Spawn Point Status")]
    public void CheckAnimationSpawnPointStatus()
    {
    }
    
    /// <summary>
    /// Force refresh the spawn point reference (for debugging)
    /// </summary>
    [ContextMenu("Force Refresh Spawn Point Reference")]
    public void ForceRefreshSpawnPointReference()
    {
        
        if (animationSpawnPoint != null)
        {
            string boneName = animationSpawnPoint.name;
            
            // Find the bone in the current scene instance
            Transform sceneInstanceBone = FindChildRecursive(transform, boneName);
            
            if (sceneInstanceBone != null)
            {
                
                if (sceneInstanceBone != animationSpawnPoint)
                {
                    animationSpawnPoint = sceneInstanceBone;
                }
            }
        }
        
    }
    
    /// <summary>
    /// Auto-setup method for prefab compatibility
    /// </summary>
    [ContextMenu("Auto-Setup for Prefab")]
    public void AutoSetupForPrefab()
    {
        
        // Find BowlingController
        if (bowlingController == null)
        {
            bowlingController = FindObjectOfType<BowlingController>();
        }
        
        // Find Animation Spawn Point
        if (animationSpawnPoint == null)
        {
            Transform rightHand = FindChildRecursive(transform, "RightHand") ?? FindChildRecursive(transform, "Right Hand");
            if (rightHand != null)
            {
                animationSpawnPoint = rightHand;
            }
        }
        
    }
    
    /// <summary>
    /// Debug animated spawn point position
    /// </summary>
    [ContextMenu("Debug Animated Spawn Point")]
    public void DebugAnimatedSpawnPoint()
    {
        
        if (animationSpawnPoint != null)
        {
            
            // Check if this is a bone in an armature
            if (animationSpawnPoint.parent != null && animationSpawnPoint.parent.name.Contains("Armature"))
            {
                
                // Check if there's an Animator component
                Animator animator = GetComponent<Animator>();
            }
        }
        
    }
    
    /// <summary>
    /// Recursively search for a child transform by name
    /// </summary>
    private Transform FindChildRecursive(Transform parent, string childName)
    {
        // Check direct children first
        Transform child = parent.Find(childName);
        if (child != null)
            return child;
        
        // Search in all children recursively
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindChildRecursive(parent.GetChild(i), childName);
            if (found != null)
                return found;
        }
        
        return null;
    }
    
    /// <summary>
    /// Test spawn point position update
    /// </summary>
    [ContextMenu("Test Spawn Point Position Update")]
    public void TestSpawnPointPositionUpdate()
    {
        
        if (animationSpawnPoint != null)
        {
            
            // Check if this is a bone in the avatar
            if (animationSpawnPoint.parent != null)
            {
            }
            
            // Wait a frame and check again
            StartCoroutine(CheckPositionAfterFrame());
        }
        
    }
    
    /// <summary>
    /// Check if the bone is actually being animated
    /// </summary>
    [ContextMenu("Check Bone Animation Status")]
    public void CheckBoneAnimationStatus()
    {
        
        if (animationSpawnPoint != null)
        {
            
            // Check if this is a bone in the avatar
            if (animationSpawnPoint.parent != null)
            {
            }
            
            // Check if the bone is actually a bone in the avatar
            Animator animator = GetComponent<Animator>();
        }
        
    }
    
    private System.Collections.IEnumerator CheckPositionAfterFrame()
    {
        yield return null; // Wait one frame
    }
    
    /// <summary>
    /// Helper method to get all child names for debugging
    /// </summary>
    private string[] GetChildNames(Transform parent)
    {
        string[] names = new string[parent.childCount];
        for (int i = 0; i < parent.childCount; i++)
        {
            names[i] = parent.GetChild(i).name;
        }
        return names;
    }

    /// <summary>
    /// Detects when bowler enters trigger box and stops camera follow + hides target
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        if (!enableTriggerStopCamera) return;

        // Check if it matches trigger box tag (if specified) or accept any trigger
        bool matchesTrigger = string.IsNullOrEmpty(triggerBoxTag) || other.CompareTag(triggerBoxTag);

        if (matchesTrigger)
        {
            
            // Stop camera follow
            BowlerEvents.NotifyStopFollowing();
            
            // Hide target smoothly
            HideTargetSides();
        }
    }
    
    /// <summary>
    /// Find Target by tag and store reference to Sides child for efficient access
    /// </summary>
    private bool FindTargetSides()
    {
        // Use cached reference if already found and still valid
        if (targetSidesTransform != null && targetSidesTransform.gameObject.activeInHierarchy)
        {
            return true;
        }
        
        // Find Target by tag (handles late instantiation)
        GameObject targetGO = null;
        try
        {
            targetGO = GameObject.FindGameObjectWithTag(targetTag);
        }
        catch (UnityException)
        {
            // Tag not defined
            return false;
        }
        
        if (targetGO == null)
        {
            return false;
        }
        
        // Find Sides child (use existing recursive search method)
        Transform sidesTransform = targetGO.transform.Find(sidesChildName);
        if (sidesTransform == null)
        {
            // Try searching recursively using existing method
            sidesTransform = FindChildRecursive(targetGO.transform, sidesChildName);
        }
        
        if (sidesTransform == null)
        {
            return false;
        }
        
        targetSidesTransform = sidesTransform;
        
        // Store original scale (only once)
        if (!originalScaleStored)
        {
            originalSidesScale = targetSidesTransform.localScale;
            originalScaleStored = true;
        }
        
        return true;
    }
    
    /// <summary>
    /// Smoothly hide target Sides by shrinking scale
    /// </summary>
    private void HideTargetSides()
    {
        if (!FindTargetSides())
        {
            return; // Target not found - no error, just skip
        }
        
        // Stop any ongoing animation
        if (currentScaleCoroutine != null)
        {
            StopCoroutine(currentScaleCoroutine);
        }
        
        currentScaleCoroutine = StartCoroutine(AnimateTargetScale(Vector3.zero, hideDuration));
    }
    
    /// <summary>
    /// Smoothly show target Sides by restoring scale
    /// </summary>
    public void ShowTargetSides()
    {
        if (!FindTargetSides() || !originalScaleStored)
        {
            return; // Target not found or scale not stored
        }
        
        // Stop any ongoing animation
        if (currentScaleCoroutine != null)
        {
            StopCoroutine(currentScaleCoroutine);
        }
        
        currentScaleCoroutine = StartCoroutine(AnimateTargetScale(originalSidesScale, showDuration));
    }
    
    /// <summary>
    /// Smoothly animate target scale to target value
    /// </summary>
    private System.Collections.IEnumerator AnimateTargetScale(Vector3 targetScale, float duration)
    {
        if (targetSidesTransform == null)
        {
            yield break;
        }
        
        Vector3 startScale = targetSidesTransform.localScale;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            
            // Smooth easing curve for professional feel
            t = Mathf.SmoothStep(0f, 1f, t);
            
            targetSidesTransform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }
        
        // Ensure final value
        targetSidesTransform.localScale = targetScale;
        currentScaleCoroutine = null;
    }
    
    /// <summary>
    /// Pick a new random Y rotation for bowling state - called when entering bowling state
    /// </summary>
    public void PickRandomYRotationForBowling()
    {
        if (enableYawJitter)
        {
            // Pick random value between min and max
            targetYRotation = Random.Range(minYRotation, maxYRotation);
            
            // Immediately apply using cached transform
            Vector3 e = cachedTransform.rotation.eulerAngles;
            e.y = targetYRotation;
            cachedTransform.rotation = Quaternion.Euler(e);
            
        }
        else
        {
            // If disabled, ensure it's 0
            targetYRotation = 0f;
            Vector3 e = cachedTransform.rotation.eulerAngles;
            e.y = 0f;
            cachedTransform.rotation = Quaternion.Euler(e);
        }
    }
    
    /// <summary>
    /// Get the original scale of the target Sides (for restoration)
    /// </summary>
    public Vector3 GetOriginalSidesScale()
    {
        if (originalScaleStored)
        {
            return originalSidesScale;
        }
        // If not stored yet, try to find and store it
        if (FindTargetSides() && targetSidesTransform != null)
        {
            originalSidesScale = targetSidesTransform.localScale;
            originalScaleStored = true;
            return originalSidesScale;
        }
        return Vector3.one; // Fallback
    }
    
    /// <summary>
    /// Set target Y rotation - called when bowler is reset to spawn
    /// </summary>
    public void SetTargetYRotation(float baseYRotation)
    {
        if (enableYawJitter)
        {
            // Random jitter between min and max
            float jitter = Random.Range(minYRotation, maxYRotation);
            targetYRotation = baseYRotation + jitter;
            // Clamp to ensure it stays within min to max range
            targetYRotation = Mathf.Clamp(targetYRotation, minYRotation, maxYRotation);
        }
        else
        {
            // No jitter - keep at 0
            targetYRotation = 0f;
        }
        
        // Immediately apply target Y rotation so Inspector reflects decimal value right away
        Vector3 e = cachedTransform.rotation.eulerAngles;
        e.y = targetYRotation;
        cachedTransform.rotation = Quaternion.Euler(e);

    }
    
    /// <summary>
    /// Enforce Y rotation in LateUpdate to prevent drift
    /// </summary>
    void LateUpdate()
    {
        // Enforce Y rotation with a tiny epsilon to avoid unnecessary writes
        Vector3 euler = cachedTransform.rotation.eulerAngles;
        float currentY = euler.y;
        // Normalize to [-180, 180] for stable comparison
        if (currentY > 180f) currentY -= 360f;
        if (Mathf.Abs(currentY - targetYRotation) > 0.0005f)
        {
            euler.y = targetYRotation;
            cachedTransform.rotation = Quaternion.Euler(euler);
        }
    }
}
