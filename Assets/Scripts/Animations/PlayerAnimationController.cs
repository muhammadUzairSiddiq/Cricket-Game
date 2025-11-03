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
    [SerializeField, Tooltip("Child name to hide (Sides)")] private string sidesChildName = "Sides";
    [SerializeField, Tooltip("Duration to shrink/hide target")] private float hideDuration = 0.3f;
    [SerializeField, Tooltip("Duration to show/restore target")] private float showDuration = 0.3f;
    
    // CRITICAL FIX: Store the last animated position to ensure consistency
    private Vector3 lastAnimatedSpawnPosition;
    private bool hasValidAnimatedPosition = false;
    
    // Target hide state
    private Transform targetSidesTransform;
    private Vector3 originalSidesScale;
    private bool originalScaleStored = false;
    private Coroutine currentScaleCoroutine;
    
    void Awake()
    {
        InitializeReferences();
        EnsureTriggerDetectionSetup();
        
        // CRITICAL FIX: Always refresh spawn point reference to ensure we're using scene instance, not prefab
        if (animationSpawnPoint != null)
        {
            Debug.Log("🎬 🔄 AWAKE: Forcing spawn point reference refresh...");
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
            if (bowlingController != null)
            {
                if (enableDebugLogs)
                    Debug.Log($"🎬 PlayerAnimationController: Auto-found BowlingController: {bowlingController.name}");
            }
            else
            {
                Debug.LogError("🎬 PlayerAnimationController: No BowlingController found in scene! Please ensure there's a BowlingController in the scene.");
            }
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
                if (enableDebugLogs)
                    Debug.Log($"🎬 PlayerAnimationController: Auto-found Animation Spawn Point: {rightHand.name}");
            }
            else
            {
                Debug.LogError($"🎬 PlayerAnimationController: No Animation Spawn Point found! Please assign 'RightHand' transform in the Inspector or ensure it exists as a child of {gameObject.name}.");
            }
        }
        
        // Validate setup
        if (bowlingController != null && animationSpawnPoint != null)
        {
            if (enableDebugLogs)
                Debug.Log($"🎬 PlayerAnimationController: Setup complete for {gameObject.name} - BowlingController: {bowlingController.name}, Spawn Point: {animationSpawnPoint.name}");
        }
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
        Debug.Log($"🧪 Testing OnBallReleased on {gameObject.name}");
        OnBallReleased();
    }

    /// <summary>
    /// Context menu to check PlayerAnimationController status
    /// </summary>
    [ContextMenu("Check PlayerAnimationController Status")]
    public void CheckPlayerAnimationControllerStatus()
    {
        Debug.Log($"🎬 === PLAYER ANIMATION CONTROLLER STATUS ===");
        Debug.Log($"🎬 GameObject: {gameObject.name}");
        Debug.Log($"🎬 BowlingController: {(bowlingController != null ? bowlingController.name : "NULL")}");
        Debug.Log($"🎬 Animation Spawn Point: {(animationSpawnPoint != null ? animationSpawnPoint.name : "NULL")}");
        Debug.Log($"🎬 Enable Debug Logs: {enableDebugLogs}");
        
        if (animationSpawnPoint != null)
        {
            Debug.Log($"🎬 Spawn Point Position: {animationSpawnPoint.position}");
            Debug.Log($"🎬 Spawn Point Parent: {(animationSpawnPoint.parent != null ? animationSpawnPoint.parent.name : "NULL")}");
        }
        
        Debug.Log($"🎬 ==========================================");
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
            if (enableDebugLogs)
                Debug.Log($"🎬 Animation started - stored original position: {originalPosition}");
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
            if (enableDebugLogs)
                Debug.Log($"🎬 Animation midpoint - starting root motion");
        }
    }
    
    /// <summary>
    /// Called at the end of bowling animation
    /// </summary>
    public void OnAnimationEnd()
    {
        if (enableManualRootMotion)
        {
            if (enableDebugLogs)
                Debug.Log($"🎬 Animation ended - root motion complete");
        }
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
        
        if (enableDebugLogs)
            Debug.Log($"🎬 Root motion complete - new position: {transform.position}");
    }

    /// <summary>
    /// Alternative method to just create a ball without bowling (for setup animations)
    /// </summary>
    public void OnBallCreated()
    {
        if (enableDebugLogs)
            Debug.Log("🎬 OnBallCreated() called from animation event");
        
        if (bowlingController == null)
        {
            Debug.LogError("🎬 PlayerAnimationController: BowlingController not found! Cannot create ball.");
            return;
        }
        
        bowlingController.InstantiateNewBall();
    }
    
    /// <summary>
    /// Called from animation event to destroy old ball before creating new one
    /// </summary>
    public void OnDestroyOldBall()
    {
        if (enableDebugLogs)
        {
            Debug.Log("🎬 OnDestroyOldBall() called from animation event");
        }
        
        if (bowlingController == null)
        {
            Debug.LogError("🎬 PlayerAnimationController: BowlingController not found! Cannot destroy old ball.");
            return;
        }
        
        // Get current ball instance and destroy it
        GameObject currentBall = bowlingController.GetCurrentBallInstance();
        if (currentBall != null)
        {
            if (enableDebugLogs)
            {
                Debug.Log("🎬 Destroying old ball instance");
            }
            Destroy(currentBall);
        }
        else
        {
            if (enableDebugLogs)
            {
                Debug.Log("🎬 No old ball to destroy");
            }
        }
    }
    
    /// <summary>
    /// Alternative method to just bowl existing ball (for release animations)
    /// </summary>
    public void OnBallBowled()
    {
        if (enableDebugLogs)
            Debug.Log("🎬 OnBallBowled() called from animation event");
        
        if (bowlingController == null)
        {
            Debug.LogError("🎬 PlayerAnimationController: BowlingController not found! Cannot bowl ball.");
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
        if (enableDebugLogs)
            Debug.Log("🎬 OnBallReleasedSmart() called from animation event");
        
        if (bowlingController == null)
        {
            Debug.LogError("🎬 PlayerAnimationController: BowlingController not found! Cannot release ball.");
            return;
        }
        
        // Check if we need to create a new ball (if none exists or previous one is done)
        if (bowlingController.GetCurrentBallInstance() == null)
        {
            if (enableDebugLogs)
                Debug.Log("🎬 Creating new ball (none exists)");
            bowlingController.InstantiateNewBall();
        }
        else
        {
            if (enableDebugLogs)
                Debug.Log("🎬 Ball already exists - skipping creation");
        }
        
        // Bowl the current ball
        if (enableDebugLogs)
            Debug.Log("🎬 Bowling the ball");
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
            if (enableDebugLogs)
                Debug.Log($"🎬 Root motion stopped for spawn position switch");
        }
    }

    /// <summary>
    /// Stop all movement coroutines (called during spawn position switching)
    /// </summary>
    public void StopAllMovement()
    {
        StopAllCoroutines();
        isMovingRoot = false;
        if (enableDebugLogs)
            Debug.Log($"🎬 All movement coroutines stopped for spawn position switch");
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
            if (enableDebugLogs)
            {
                Debug.Log($"🎬 Current animated spawn position: {currentPosition}");
                Debug.Log($"🎬 Spawn point name: {animationSpawnPoint.name}");
                Debug.Log($"🎬 Spawn point parent: {(animationSpawnPoint.parent != null ? animationSpawnPoint.parent.name : "NULL")}");
                Debug.Log($"🎬 Spawn point local position: {animationSpawnPoint.localPosition}");
                Debug.Log($"🎬 Spawn point world position: {animationSpawnPoint.position}");
                Debug.Log($"🎬 Spawn point scene: {animationSpawnPoint.gameObject.scene.name}");
            }
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
            if (enableDebugLogs)
            {
                Debug.Log($"🎬 Current spawn point position: {animationSpawnPoint.position}");
                Debug.Log($"🎬 Spawn point local position: {animationSpawnPoint.localPosition}");
            }
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
            
            if (enableDebugLogs)
            {
                Debug.Log($"🎬 Refreshing spawn point reference from: {animationSpawnPoint.name}");
                Debug.Log($"🎬 Spawn point scene: {animationSpawnPoint.gameObject.scene.name}");
                Debug.Log($"🎬 Current GameObject scene: {gameObject.scene.name}");
            }
            
            // CRITICAL FIX: Always find the scene instance bone, even if animationSpawnPoint is pointing to prefab
            Transform sceneInstanceBone = FindChildRecursive(transform, boneName);
            
            if (sceneInstanceBone != null)
            {
                // Check if the current reference is from a different scene (prefab vs instance)
                bool isFromPrefab = animationSpawnPoint.gameObject.scene.name != gameObject.scene.name;
                
                if (isFromPrefab || sceneInstanceBone != animationSpawnPoint)
                {
                    animationSpawnPoint = sceneInstanceBone;
                    if (enableDebugLogs)
                    {
                        Debug.Log($"🎬 ✅ Refreshed spawn point to scene instance: {animationSpawnPoint.name}");
                        Debug.Log($"🎬 New position: {animationSpawnPoint.position}");
                        Debug.Log($"🎬 New scene: {animationSpawnPoint.gameObject.scene.name}");
                    }
                }
                else
                {
                    if (enableDebugLogs)
                    {
                        Debug.Log($"🎬 ✅ Spawn point already points to scene instance: {animationSpawnPoint.name}");
                    }
                }
            }
            else
            {
                Debug.LogError($"🎬 ❌ Could not find scene instance of bone: {boneName}");
            }
        }
    }
    
    /// <summary>
    /// Debug method to check spawn point status
    /// </summary>
    [ContextMenu("Check Animation Spawn Point Status")]
    public void CheckAnimationSpawnPointStatus()
    {
        Debug.Log("🎬 === ANIMATION SPAWN POINT STATUS ===");
        Debug.Log($"🎬 GameObject: {gameObject.name}");
        Debug.Log($"🎬 Animation Spawn Point: {(animationSpawnPoint != null ? animationSpawnPoint.name : "NULL - NOT ASSIGNED!")}");
        if (animationSpawnPoint != null)
        {
            Debug.Log($"🎬 Position: {animationSpawnPoint.position}");
            Debug.Log($"🎬 Rotation: {animationSpawnPoint.rotation.eulerAngles}");
            Debug.Log($"🎬 Scene: {animationSpawnPoint.gameObject.scene.name}");
        }
        Debug.Log($"🎬 Bowling Controller: {(bowlingController != null ? bowlingController.name : "NULL")}");
        Debug.Log("🎬 ======================================");
    }
    
    /// <summary>
    /// Force refresh the spawn point reference (for debugging)
    /// </summary>
    [ContextMenu("Force Refresh Spawn Point Reference")]
    public void ForceRefreshSpawnPointReference()
    {
        Debug.Log("🎬 === FORCE REFRESHING SPAWN POINT REFERENCE ===");
        Debug.Log($"🎬 Current GameObject: {gameObject.name}");
        Debug.Log($"🎬 Current GameObject scene: {gameObject.scene.name}");
        
        if (animationSpawnPoint != null)
        {
            string boneName = animationSpawnPoint.name;
            Debug.Log($"🎬 Current spawn point: {boneName}");
            Debug.Log($"🎬 Current spawn point scene: {animationSpawnPoint.gameObject.scene.name}");
            Debug.Log($"🎬 Current spawn point position: {animationSpawnPoint.position}");
            
            // Find the bone in the current scene instance
            Transform sceneInstanceBone = FindChildRecursive(transform, boneName);
            
            if (sceneInstanceBone != null)
            {
                Debug.Log($"🎬 Found scene instance bone: {sceneInstanceBone.name}");
                Debug.Log($"🎬 Scene instance bone position: {sceneInstanceBone.position}");
                Debug.Log($"🎬 Scene instance bone scene: {sceneInstanceBone.gameObject.scene.name}");
                
                if (sceneInstanceBone != animationSpawnPoint)
                {
                    animationSpawnPoint = sceneInstanceBone;
                    Debug.Log($"🎬 ✅ FORCE REFRESHED to scene instance: {animationSpawnPoint.name}");
                    Debug.Log($"🎬 New position: {animationSpawnPoint.position}");
                    Debug.Log($"🎬 New scene: {animationSpawnPoint.gameObject.scene.name}");
                }
                else
                {
                    Debug.Log($"🎬 ✅ Already using scene instance: {animationSpawnPoint.name}");
                }
            }
            else
            {
                Debug.LogError($"🎬 ❌ Could not find scene instance of bone: {boneName}");
                Debug.LogError($"🎬 Available children: {string.Join(", ", GetChildNames(transform))}");
            }
        }
        else
        {
            Debug.LogError("🎬 ❌ No spawn point assigned!");
        }
        
        Debug.Log("🎬 ==============================================");
    }
    
    /// <summary>
    /// Auto-setup method for prefab compatibility
    /// </summary>
    [ContextMenu("Auto-Setup for Prefab")]
    public void AutoSetupForPrefab()
    {
        Debug.Log($"🎬 === AUTO-SETUP FOR {gameObject.name} ===");
        
        // Find BowlingController
        if (bowlingController == null)
        {
            bowlingController = FindObjectOfType<BowlingController>();
            if (bowlingController != null)
            {
                Debug.Log($"🎬 ✅ Auto-assigned BowlingController: {bowlingController.name}");
            }
            else
            {
                Debug.LogError("🎬 ❌ No BowlingController found in scene!");
            }
        }
        else
        {
            Debug.Log($"🎬 ✅ BowlingController already assigned: {bowlingController.name}");
        }
        
        // Find Animation Spawn Point
        if (animationSpawnPoint == null)
        {
            Transform rightHand = FindChildRecursive(transform, "RightHand") ?? FindChildRecursive(transform, "Right Hand");
            if (rightHand != null)
            {
                animationSpawnPoint = rightHand;
                Debug.Log($"🎬 ✅ Auto-assigned Animation Spawn Point: {rightHand.name}");
            }
            else
            {
                Debug.LogError($"🎬 ❌ No RightHand transform found in {gameObject.name} or its children!");
            }
        }
        else
        {
            Debug.Log($"🎬 ✅ Animation Spawn Point already assigned: {animationSpawnPoint.name}");
        }
        
        Debug.Log("🎬 ======================================");
    }
    
    /// <summary>
    /// Debug animated spawn point position
    /// </summary>
    [ContextMenu("Debug Animated Spawn Point")]
    public void DebugAnimatedSpawnPoint()
    {
        Debug.Log("🎬 === ANIMATED SPAWN POINT DEBUG ===");
        
        if (animationSpawnPoint != null)
        {
            Debug.Log($"🎬 Spawn Point Name: {animationSpawnPoint.name}");
            Debug.Log($"🎬 Spawn Point Position: {animationSpawnPoint.position}");
            Debug.Log($"🎬 Spawn Point Rotation: {animationSpawnPoint.rotation.eulerAngles}");
            Debug.Log($"🎬 Spawn Point Parent: {(animationSpawnPoint.parent != null ? animationSpawnPoint.parent.name : "NULL")}");
            
            // Check if this is a bone in an armature
            if (animationSpawnPoint.parent != null && animationSpawnPoint.parent.name.Contains("Armature"))
            {
                Debug.Log("🎬 ✅ This appears to be a bone in an armature");
                
                // Check if there's an Animator component
                Animator animator = GetComponent<Animator>();
                if (animator != null)
                {
                    Debug.Log($"🎬 Animator found: {animator.name}");
                    Debug.Log($"🎬 Animator Controller: {(animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "NULL")}");
                    Debug.Log($"🎬 Avatar: {(animator.avatar != null ? animator.avatar.name : "NULL")}");
                    Debug.Log($"🎬 Apply Root Motion: {animator.applyRootMotion}");
                }
                else
                {
                    Debug.LogWarning("🎬 ❌ No Animator component found on this GameObject");
                }
            }
            else
            {
                Debug.LogWarning("🎬 ❌ This doesn't appear to be a bone in an armature");
            }
        }
        else
        {
            Debug.LogError("🎬 ❌ No animation spawn point assigned!");
        }
        
        Debug.Log("🎬 ======================================");
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
        Debug.Log("🎬 === TESTING SPAWN POINT POSITION UPDATE ===");
        
        if (animationSpawnPoint != null)
        {
            Debug.Log($"🎬 Initial position: {animationSpawnPoint.position}");
            Debug.Log($"🎬 Initial local position: {animationSpawnPoint.localPosition}");
            Debug.Log($"🎬 Parent: {(animationSpawnPoint.parent != null ? animationSpawnPoint.parent.name : "NULL")}");
            
            // Check if this is a bone in the avatar
            if (animationSpawnPoint.parent != null)
            {
                Debug.Log($"🎬 BONE HIERARCHY: {animationSpawnPoint.parent.name} -> {animationSpawnPoint.name}");
                if (animationSpawnPoint.parent.parent != null)
                {
                    Debug.Log($"🎬 BONE HIERARCHY: {animationSpawnPoint.parent.parent.name} -> {animationSpawnPoint.parent.name} -> {animationSpawnPoint.name}");
                }
            }
            
            // Wait a frame and check again
            StartCoroutine(CheckPositionAfterFrame());
        }
        else
        {
            Debug.LogError("🎬 ❌ No animation spawn point assigned!");
        }
        
        Debug.Log("🎬 ============================================");
    }
    
    /// <summary>
    /// Check if the bone is actually being animated
    /// </summary>
    [ContextMenu("Check Bone Animation Status")]
    public void CheckBoneAnimationStatus()
    {
        Debug.Log("🎬 === CHECKING BONE ANIMATION STATUS ===");
        
        if (animationSpawnPoint != null)
        {
            Debug.Log($"🎬 Bone Name: {animationSpawnPoint.name}");
            Debug.Log($"🎬 Current Position: {animationSpawnPoint.position}");
            Debug.Log($"🎬 Current Local Position: {animationSpawnPoint.localPosition}");
            Debug.Log($"🎬 Parent: {(animationSpawnPoint.parent != null ? animationSpawnPoint.parent.name : "NULL")}");
            
            // Check if this is a bone in the avatar
            if (animationSpawnPoint.parent != null)
            {
                Debug.Log($"🎬 BONE HIERARCHY: {animationSpawnPoint.parent.name} -> {animationSpawnPoint.name}");
                if (animationSpawnPoint.parent.parent != null)
                {
                    Debug.Log($"🎬 BONE HIERARCHY: {animationSpawnPoint.parent.parent.name} -> {animationSpawnPoint.parent.name} -> {animationSpawnPoint.name}");
                }
            }
            
            // Check if the bone is actually a bone in the avatar
            Animator animator = GetComponent<Animator>();
            if (animator != null)
            {
                Debug.Log($"🎬 Animator found: {animator.name}");
                Debug.Log($"🎬 Avatar: {(animator.avatar != null ? animator.avatar.name : "NULL")}");
                Debug.Log($"🎬 Controller: {(animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "NULL")}");
                
                if (animator.avatar == null)
                {
                    Debug.LogError("🎬 ❌ CRITICAL: Avatar is NULL! Bones won't animate without an Avatar!");
                }
                else
                {
                    Debug.Log("🎬 ✅ Avatar is assigned");
                }
            }
            else
            {
                Debug.LogError("🎬 ❌ No Animator component found!");
            }
        }
        else
        {
            Debug.LogError("🎬 ❌ No animation spawn point assigned!");
        }
        
        Debug.Log("🎬 ========================================");
    }
    
    private System.Collections.IEnumerator CheckPositionAfterFrame()
    {
        yield return null; // Wait one frame
        
        if (animationSpawnPoint != null)
        {
            Debug.Log($"🎬 After frame position: {animationSpawnPoint.position}");
            Debug.Log($"🎬 After frame local position: {animationSpawnPoint.localPosition}");
        }
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
            if (enableDebugLogs)
                Debug.Log($"🎬 PlayerAnimationController: Bowler entered trigger box: {other.name} - Stopping camera follow");
            
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
            if (enableDebugLogs)
                Debug.LogWarning($"🎬 PlayerAnimationController: Could not find '{sidesChildName}' child in Target GameObject");
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
}
