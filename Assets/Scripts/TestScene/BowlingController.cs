using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CricketBowlingAnimations;
using System;

namespace CricketGame
{
    /// <summary>
    /// Continuous Bowling Test System - WITH BOUNCE PHYSICS
    /// Automatically bowls ball to target with realistic cricket physics and bouncing, waits 3 seconds, returns to original position, and repeats
    /// </summary>
    public class BowlingController : MonoBehaviour
    {
        [Header("Test Setup")]
        [SerializeField] private GameObject ball;
        [SerializeField] private Transform target;
        [SerializeField] private Transform ballSpawnPoint;
        [Tooltip("Umpire-side wicket (bowler end) reference for length calculation")]
        [SerializeField] private Transform umpireWicket;
        [Tooltip("Batsman-side wicket (striker end) reference for length calculation")]
        [SerializeField] private Transform batsmanWicket;
        
        [Header("Dynamic Bowling Settings")]
        [SerializeField] private bool useDynamicSettings = true;
        
        [Header("Ball Settings Reference")]
        [SerializeField] private BallSettingsSO ballSettingsSO; // ScriptableObject with all ball settings
        [SerializeField] private SpeedController speedController; // Speed controller reference
        
        [Header("Length Zone Visualization")]
        [SerializeField] private bool showBowlingZones = true;
        [SerializeField] private Transform pitchingArea; // Reference to your manually created Pitching Area
        [SerializeField] private Transform yorkerZone; // Reference to your "Yorker" zone
        [SerializeField] private Transform fullTossZone; // Reference to your "Full toss" zone
        [SerializeField] private Transform lengthZone; // Reference to your "Length" zone
        [SerializeField] private Transform slotZone; // Reference to your "Slot" zone
        [SerializeField] private Transform shortZone; // Reference to your "Short" zone
        [SerializeField] private float zoneHeight = 0.1f;
        [SerializeField] private float zoneWidth = 2f;
        
        [Header("Bowling Settings")]
        [SerializeField] private float returnSpeed = 15f;
        [SerializeField] private float waitTimeAfterLanding = 3f;
        
        [Header("Smooth Movement Settings")]
        [SerializeField] private bool useSmoothMovement = true; // Enable smooth ball movement
        [SerializeField] private float smoothnessFactor = 0.95f; // How smooth the movement is (0-1)
        [SerializeField] private float velocitySmoothing = 0.98f; // Velocity smoothing factor
        [SerializeField] private float minVelocityThreshold = 0.01f; // Minimum velocity to consider movement
        
        [Header("Delivery System")]
        [SerializeField] private DeliverySystem deliverySystem;
        
        
        [Header("Controls")]
        [SerializeField] private KeyCode startKey = KeyCode.Space;
        
        // Toggle to allow manual key control (S = new ball, Space = bowl). When false, keys are ignored
        [Header("Input Settings")]
        [Tooltip("Enable manual key input (S to spawn ball, Space to bowl). Disable to drive from animation/bowler hand.")]
        [SerializeField] private bool enableManualKeyInput = false;
        [SerializeField] private KeyCode stopKey = KeyCode.Escape;
        
        [Header("Bowler Prefab Selection")]
        [Tooltip("Select which bowler prefab to instantiate")]
        public GameObject selectedBowlerPrefab;
        
        [Header("Available Bowler Prefabs")]
        [Tooltip("All available bowler prefabs. Drag and drop your bowler prefabs here.")]
        public GameObject[] availableBowlerPrefabs;
        
        [Header("Auto-Instantiation Settings")]
        [Tooltip("Automatically instantiate the selected bowler when the scene starts")]
        public bool autoInstantiateBowler = true;

        [Header("Spawn Point Mapping")]
        [Tooltip("Map each bowler prefab to its two spawn positions in the scene")]
        public BowlerSpawnMapping[] bowlerSpawnMappings = new BowlerSpawnMapping[7];
        
        [Tooltip("Position where the bowler will be instantiated")]
        public Vector3 bowlerSpawnPosition = new Vector3(3.56f, 0.619f, 32.86f);
        
        [Tooltip("Rotation for the instantiated bowler")]
        public Vector3 bowlerSpawnRotation = Vector3.zero;
        
        // Private variables
        // 🎯 REMOVED: originalBallPosition (was cached, now use spawnPoint.position dynamically)
        private Rigidbody ballRigidbody;
        private bool isRunning = false;
        private GameObject currentBowlerInstance; // Store the instantiated bowler
        private PlayerAnimationController playerAnimationController; // Cached reference to selected bowler's controller
        private bool isReturning = false;
        private bool hasLanded = false;
        private Coroutine bowlingCoroutine;
        private int currentBounces = 0; // ?? NEW: Track number of bounces
        private bool isBouncing = false; // ?? NEW: Track if ball is currently bouncing
        private Vector3 lastBouncePosition; // ?? NEW: Store position of last bounce
        private Transform spawnPoint; // ?? NEW: Internal spawn point reference
        // ?? Length factor along pitch: 0 = near umpire (short/bouncer), 1 = near batsman (yorker)
        private float currentLength01 = 0.5f;
        
        // Smooth movement variables
        private Vector3 previousPosition;
        private Vector3 smoothedVelocity;
        private Vector3 velocityAcceleration;
        private float lastUpdateTime;
        
        private readonly Dictionary<int, InstanceSpawnSnapshot> bowlerInstanceSnapshots = new Dictionary<int, InstanceSpawnSnapshot>();
        private readonly Dictionary<string, bool> autoSpawnToggle = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        private Transform spawnPointsRoot;
        
        void Awake()
        {
            // Get PlayerAnimationController from selected bowler
            UpdatePlayerAnimationController();
        }
        
        /// <summary>
        /// Update PlayerAnimationController based on selected bowler prefab
        /// </summary>
        public void UpdatePlayerAnimationController()
        {
            if (selectedBowlerPrefab != null)
            {
                playerAnimationController = selectedBowlerPrefab.GetComponent<PlayerAnimationController>();
                if (playerAnimationController != null)
                {
                    Debug.Log($"🎯 Found PlayerAnimationController in selected bowler: {selectedBowlerPrefab.name}");
                }
                else
                {
                    Debug.LogWarning($"🎯 No PlayerAnimationController found in selected bowler: {selectedBowlerPrefab.name}");
                }
            }
            else
            {
                Debug.LogWarning("🎯 No bowler prefab selected");
            }
        }
        
        /// <summary>
        /// Instantiate the selected bowler in the scene
        /// </summary>
        public void InstantiateSelectedBowler()
        {
            // CRITICAL FIX: Destroy ALL existing bowlers in the scene (both runtime and editor-created)
            PlayerAnimationController[] allBowlers = FindObjectsOfType<PlayerAnimationController>();
            
            foreach (PlayerAnimationController bowler in allBowlers)
            {
                if (bowler != null && bowler.gameObject != null)
                {
                    bool isSceneObject = bowler.gameObject.scene.IsValid();
                    
                    if (Application.isPlaying)
                    {
                        if (isSceneObject)
                        {
                            bowler.gameObject.SetActive(false);
                            bowler.enabled = false;
                        }
                        else
                        {
                            Destroy(bowler.gameObject);
                        }
                    }
                    else
                    {
                        DestroyImmediate(bowler.gameObject);
                    }
                }
            }
            
            currentBowlerInstance = null;
            playerAnimationController = null;
            
            if (selectedBowlerPrefab != null)
            {
                BowlerSpawnMapping mapping = GetSpawnMappingForPrefab(selectedBowlerPrefab);
                Vector3 spawnPos = bowlerSpawnPosition;
                Quaternion spawnRot = Quaternion.Euler(bowlerSpawnRotation);
                
                if (mapping != null)
                {
                    Transform currentSpawn = mapping.useSpawn01 ? mapping.spawn01 : mapping.spawn02;
                    if (currentSpawn != null)
                    {
                        spawnPos = currentSpawn.position;
                        spawnRot = currentSpawn.rotation;
                    }
                }
                
                currentBowlerInstance = Instantiate(selectedBowlerPrefab, spawnPos, spawnRot);
                currentBowlerInstance.name = $"{selectedBowlerPrefab.name}(Clone)";
                
                Transform initialSpawn = GetMappingSpawnTransform(currentBowlerInstance) ?? GetAutoResolvedSpawn(currentBowlerInstance, false);
                if (initialSpawn == null)
                {
                    initialSpawn = CreateRuntimeSnapshotTransform(currentBowlerInstance.name, spawnPos, spawnRot);
                }
                CacheInstanceSnapshot(currentBowlerInstance, spawnPos, spawnRot, initialSpawn);
                
                PlayerAnimationController instantiatedController = currentBowlerInstance.GetComponent<PlayerAnimationController>();
                if (instantiatedController != null)
                {
                    playerAnimationController = instantiatedController;
                    
                    // CRITICAL: Wait a frame before refreshing spawn point to ensure Animator is initialized
                    StartCoroutine(RefreshBowlerAfterInstantiation(instantiatedController));
                    
                    BowlerProfile profile = currentBowlerInstance.GetComponent<BowlerProfile>();
                    if (profile != null && deliverySystem != null)
                    {
                        DeliveryType defaultDelivery = profile.GetDefaultDeliveryType();
                        deliverySystem.SetDeliveryType(defaultDelivery);
                    }
                    
                    NotifyBowlerReady();
                }
            }
        }
        
        /// <summary>
        /// Refresh bowler spawn point reference after instantiation to fix runtime spawn issues
        /// </summary>
        private System.Collections.IEnumerator RefreshBowlerAfterInstantiation(PlayerAnimationController controller)
        {
            // Wait a few frames for Animator to initialize and bones to update
            yield return null;
            yield return null;
            
            // Now refresh the spawn point reference
            controller.ForceRefreshSpawnPointReference();
            
            CacheInstanceSnapshot(controller.gameObject,
                                   controller.transform.position,
                                   controller.transform.rotation,
                                   GetMappingSpawnTransform(controller.gameObject) ?? GetAutoResolvedSpawn(controller.gameObject, false));
            
            Debug.Log("🎯 Spawn point refreshed after bowler instantiation");
        }
        
        /// <summary>
        /// Notify GameplayInputHandler that bowler is ready
        /// </summary>
        private void NotifyBowlerReady()
        {
            // Find GameplayInputHandler and refresh its references
            GameplayInputHandler inputHandler = FindObjectOfType<GameplayInputHandler>();
            if (inputHandler != null)
            {
                Debug.Log("🎯 Notifying GameplayInputHandler that bowler is ready");
                inputHandler.RefreshComponentReferences();
            }
        }
        
        /// <summary>
        /// Select a bowler prefab from the available list
        /// </summary>
        public void SelectBowlerPrefab(int index)
        {
            if (availableBowlerPrefabs != null && index >= 0 && index < availableBowlerPrefabs.Length)
            {
                selectedBowlerPrefab = availableBowlerPrefabs[index];
                Debug.Log($"🎯 Selected bowler prefab: {selectedBowlerPrefab.name}");
                
                // Auto-instantiate if enabled
                if (autoInstantiateBowler)
                {
                    InstantiateSelectedBowler();
                }
            }
            else
            {
                Debug.LogWarning($"🎯 Invalid bowler prefab index: {index}");
            }
        }
        
        /// <summary>
        /// Get PlayerAnimationController from selected bowler (public getter)
        /// </summary>
        public PlayerAnimationController GetPlayerAnimationController()
        {
            // OPTIMIZED: Use cached reference to avoid expensive FindObjectOfType calls
            if (playerAnimationController != null && playerAnimationController.gameObject.activeInHierarchy && playerAnimationController.enabled)
            {
                return playerAnimationController;
            }
            
            // Only refresh cache when necessary (performance optimization)
            RefreshPlayerAnimationControllerCache();
            return playerAnimationController;
        }
        
        /// <summary>
        /// OPTIMIZED: Refresh PlayerAnimationController cache only when needed
        /// </summary>
        private void RefreshPlayerAnimationControllerCache()
        {
            PlayerAnimationController[] allBowlers = FindObjectsOfType<PlayerAnimationController>();
            foreach (PlayerAnimationController bowler in allBowlers)
            {
                if (bowler != null && bowler.gameObject.activeInHierarchy && bowler.enabled)
                {
                    playerAnimationController = bowler;
                    return;
                }
            }
            playerAnimationController = null;
        }
        
        void Start()
        {
            Debug.Log("🎯 === BOWLING CONTROLLER START ===");
            Debug.Log($"🎯 Auto-instantiate bowler: {autoInstantiateBowler}");
            Debug.Log($"🎯 Selected bowler prefab: {(selectedBowlerPrefab != null ? selectedBowlerPrefab.name : "NULL")}");
            
            SetupTest();
            
            // Auto-instantiate bowler if enabled
            if (autoInstantiateBowler && selectedBowlerPrefab != null)
            {
                Debug.Log("🎯 Auto-instantiation ENABLED - calling InstantiateSelectedBowler()");
                InstantiateSelectedBowler();
            }
            else
            {
                Debug.Log("🎯 Auto-instantiation DISABLED or no prefab selected - skipping bowler instantiation");
            }
            
            // Using existing bowling zones (manually created under Pitching Area)
            Debug.Log("🎯 Using existing bowling zones from Pitching Area");
            
            // Debug current settings
            Debug.Log($"🎯 DEBUG: enableManualKeyInput = {enableManualKeyInput}");
            PlayerAnimationController animController = GetPlayerAnimationController();
            Debug.Log($"🎯 DEBUG: playerAnimationController = {(animController != null ? animController.name : "NULL")}");
            if (animController != null)
            {
                Transform animSpawn = animController.GetAnimationSpawnPoint();
                Debug.Log($"🎯 DEBUG: animationSpawnPoint = {(animSpawn != null ? animSpawn.name : "NULL")}");
            }
        }
        
        void Update()
        {
            HandleInput();
            
            // Update smooth movement if enabled
            if (useSmoothMovement && ball != null)
            {
                UpdateSmoothMovement();
            }
        }
        
        /// <summary>
        /// Setup the test system
        /// </summary>
        void SetupTest()
        {
            if (ball == null)
            {
                Debug.LogError("Ball not assigned! Please assign a ball GameObject.");
                return;
            }
            
            if (target == null)
            {
                Debug.LogError("Target not assigned! Please assign a target Transform.");
                return;
            }
            
            if (ballSpawnPoint == null)
            {
                Debug.LogError("Ball spawn point not assigned! Please assign a spawn point Transform.");
                return;
            }
            
            // Store spawn point reference
            spawnPoint = ballSpawnPoint;
            
            // 🎯 DYNAMIC SPAWN: Don't cache position - always use current spawnPoint.position
            // This allows spawn point to be moved at runtime!
            
            // Initialize smooth movement variables
            previousPosition = ball.transform.position;
            smoothedVelocity = Vector3.zero;
            velocityAcceleration = Vector3.zero;
            lastUpdateTime = Time.time;
            
            // Setup ball physics
            SetupBallPhysics();
            
            // System initialized
        }
        
        /// <summary>
        /// Update smooth movement calculations
        /// </summary>
        void UpdateSmoothMovement()
        {
            if (ball == null) return;
            
            float deltaTime = Time.deltaTime;
            float currentTime = Time.time;
            
            // Calculate actual velocity from position change
            Vector3 currentPosition = ball.transform.position;
            Vector3 actualVelocity = (currentPosition - previousPosition) / deltaTime;
            
            // Smooth the velocity using exponential smoothing
            smoothedVelocity = Vector3.Lerp(smoothedVelocity, actualVelocity, velocitySmoothing);
            
            // Apply smooth velocity if ball has rigidbody and is moving
            if (ballRigidbody != null && ballRigidbody.linearVelocity.magnitude > minVelocityThreshold)
            {
                // Only apply smoothing if the velocity difference is significant
                if (Vector3.Distance(smoothedVelocity, ballRigidbody.linearVelocity) > 0.1f)
                {
                    ballRigidbody.linearVelocity = Vector3.Lerp(ballRigidbody.linearVelocity, smoothedVelocity, smoothnessFactor * deltaTime * 10f);
                }
            }
            
            // Update previous position for next frame
            previousPosition = currentPosition;
            lastUpdateTime = currentTime;
        }
        
        /// <summary>
        /// Apply dynamic bowling settings based on target position
        /// </summary>
        void ApplyDynamicBowlingSettings(BallSettings ballSettings)
        {
            Debug.Log($"<color=#FF0000>🚨 ApplyDynamicBowlingSettings STARTED!</color>");
            Debug.Log($"<color=#FFD700>🔍 DEBUGGING DYNAMIC SETTINGS: useDynamicSettings={useDynamicSettings}, target={target != null}, pitchingArea={pitchingArea != null}</color>");
            
            if (!useDynamicSettings || target == null || pitchingArea == null)
            {
                Debug.Log($"<color=#FF0000>❌ DYNAMIC BOWLING INACTIVE: useDynamicSettings={useDynamicSettings}, target={target != null}, pitchingArea={pitchingArea != null}</color>");
                // Apply basic settings even if dynamic is disabled
                ApplyBasicBallSettings(ballSettings);
                return;
            }
            
            Debug.Log($"<color=#FFD700>🔍 ZONE REFERENCES: yorkerZone={yorkerZone != null}, fullTossZone={fullTossZone != null}, lengthZone={lengthZone != null}, slotZone={slotZone != null}, shortZone={shortZone != null}</color>");
            
            // Check if ballSettingsSO is assigned
            if (ballSettingsSO == null)
            {
                Debug.LogError("🚨 ballSettingsSO is not assigned! Please assign the BallSettingsSO ScriptableObject in the Inspector.");
                return;
            }
            
            // Store original values for comparison
            float originalSpeed = ballSettingsSO.GlobalBallSpeed;
            float originalArc = ballSettingsSO.ArcHeight;
            float originalBounce = ballSettingsSO.BounceForce;
            
            // Get current bowling length based on zone detection
            BowlingLength lengthCategory = GetCurrentBowlingLength();
            
            // Adjust settings based on length
            AdjustBallSettingsForLength(ballSettings, lengthCategory);
            
            // Apply rotation to spawn point based on bowling length
            ApplyBowlingRotation(lengthCategory);
            
            // Colorful debug information with before/after comparison
            string lengthColor = GetLengthColor(lengthCategory);
            string speedColor = originalSpeed != ballSettingsSO.GlobalBallSpeed ? "<color=#00FF00>" : "<color=#FF0000>";
            string arcColor = originalArc != ballSettingsSO.ArcHeight ? "<color=#00FF00>" : "<color=#FF0000>";
            string bounceColor = originalBounce != ballSettingsSO.BounceForce ? "<color=#00FF00>" : "<color=#FF0000>";
            
            Debug.Log($"{lengthColor}🎯 BOWLING LENGTH: {lengthCategory} (Zone-based detection)</color>");
            Debug.Log($"{speedColor}⚡ GLOBAL BALL SPEED: {originalSpeed:F1} → {ballSettingsSO.GlobalBallSpeed:F1} m/s (applies to all lengths)</color>");
            Debug.Log($"{arcColor}📈 ARC HEIGHT: {originalArc:F1} → {ballSettingsSO.ArcHeight:F1} m</color>");
            Debug.Log($"{bounceColor}🏀 BOUNCE FORCE: {originalBounce:F2} → {ballSettingsSO.BounceForce:F2}</color>");
            Debug.Log($"<color=#FFD700>✅ DYNAMIC SETTINGS APPLIED SUCCESSFULLY!</color>");
        }
        
        /// <summary>
        /// Determine bowling length category based on percentage
        /// </summary>
        BowlingLength GetBowlingLength(float percentage)
        {
            if (percentage <= 0.1f) // Yorker: 0-10%
                return BowlingLength.Yorker;
            else if (percentage <= 0.3f) // Full Length: 10-30%
                return BowlingLength.FullLength;
            else if (percentage <= 0.5f) // Good Length: 30-50%
                return BowlingLength.GoodLength;
            else if (percentage <= 0.7f) // Short Length: 50-70%
                return BowlingLength.ShortLength;
            else // Bouncer: 70-100%
                return BowlingLength.Bouncer;
        }
        
        /// <summary>
        /// Adjust ball settings based on length category using single BallSettings component
        /// </summary>
        void AdjustBallSettingsForLength(BallSettings targetBallSettings, BowlingLength length)
        {
            Debug.Log($"<color=#FFD700>🔍 AdjustBallSettingsForLength called for {length}</color>");
            Debug.Log($"<color=#FFD700>🔍 ballSettingsSO reference: {ballSettingsSO != null}</color>");
            
            if (ballSettingsSO == null)
            {
                Debug.LogWarning($"⚠️ No BallSettings reference found! Using hardcoded values for {length}.");
                // Use hardcoded values as fallback
                ApplyHardcodedSettings(targetBallSettings, length);
                return;
            }
            
            Debug.Log($"<color=#FFD700>🔍 Using Inspector BallSettings for {length}</color>");
            
            // Apply global settings (all settings are now global for simplicity)
            targetBallSettings.SetArcHeight(ballSettingsSO.ArcHeight); // Use global arc height
            targetBallSettings.SetGravity(ballSettingsSO.Gravity); // Use global gravity
            targetBallSettings.SetBounceForce(ballSettingsSO.BounceForce); // Use global bounce force
            targetBallSettings.SetBounceFriction(ballSettingsSO.BounceFriction); // Use global bounce friction
            targetBallSettings.SetMaxBounces(ballSettingsSO.MaxBounces);
            targetBallSettings.SetUseRealisticPhysics(ballSettingsSO.UseRealisticPhysics);
            
            Debug.Log($"✅ Applied {length} settings from single BallSettings component");
        }
        
        /// <summary>
        /// Apply basic ball settings when dynamic settings are disabled
        /// </summary>
        void ApplyBasicBallSettings(BallSettings targetBallSettings)
        {
            Debug.Log("🎯 Applying basic ball settings (dynamic disabled)");
            
            // Apply default cricket ball settings
            targetBallSettings.SetBallSpeed(12f);
            targetBallSettings.SetArcHeight(1.2f);
            targetBallSettings.SetBounceForce(1.0f);
            targetBallSettings.SetBounceFriction(0.85f);
            targetBallSettings.SetGravity(9.81f);
            targetBallSettings.SetMaxBounces(3);
            targetBallSettings.SetUseRealisticPhysics(true);
            
            Debug.Log("✅ Basic ball settings applied: Speed=12, Arc=1.2, Bounce=1.0, Physics=true");
        }
        
        /// <summary>
        /// Apply hardcoded settings as fallback when BallSettings reference is not assigned
        /// </summary>
        void ApplyHardcodedSettings(BallSettings targetBallSettings, BowlingLength length)
        {
            // Set global speed first (same for all lengths now)
            targetBallSettings.SetBallSpeed(12f); // Global speed for all lengths
            
            switch (length)
            {
                case BowlingLength.Yorker:
                    targetBallSettings.SetArcHeight(1.5f);
                    targetBallSettings.SetBounceForce(1.2f);
                    targetBallSettings.SetBounceFriction(0.9f);
                    break;
                    
                case BowlingLength.FullLength:
                    targetBallSettings.SetArcHeight(1.2f);
                    targetBallSettings.SetBounceForce(0.9f);
                    targetBallSettings.SetBounceFriction(0.8f);
                    break;
                    
                case BowlingLength.GoodLength:
                    targetBallSettings.SetArcHeight(1.3f);
                    targetBallSettings.SetBounceForce(1.0f);
                    targetBallSettings.SetBounceFriction(0.85f);
                    break;
                    
                case BowlingLength.ShortLength:
                    targetBallSettings.SetArcHeight(1.1f);
                    targetBallSettings.SetBounceForce(0.8f);
                    targetBallSettings.SetBounceFriction(0.75f);
                    break;
                    
                case BowlingLength.Bouncer:
                    targetBallSettings.SetArcHeight(1.0f);
                    targetBallSettings.SetBounceForce(0.7f);
                    targetBallSettings.SetBounceFriction(0.7f);
                    break;
            }
            
            Debug.Log($"✅ Applied hardcoded {length} settings: Global Speed={targetBallSettings.BallSpeed}, Arc={targetBallSettings.ArcHeight}, Bounce={targetBallSettings.BounceForce}");
        }
        
        /// <summary>
        /// Get color for bowling length category
        /// </summary>
        string GetLengthColor(BowlingLength length)
        {
            switch (length)
            {
                case BowlingLength.Yorker: return "<color=#FF0000>"; // Red
                case BowlingLength.FullLength: return "<color=#FF8C00>"; // Orange
                case BowlingLength.GoodLength: return "<color=#00FF00>"; // Green
                case BowlingLength.ShortLength: return "<color=#0000FF>"; // Blue
                case BowlingLength.Bouncer: return "<color=#800080>"; // Purple
                default: return "<color=#FFFFFF>"; // White
            }
        }
        
        // Zone creation methods removed - using existing manually created zones
        
        /// <summary>
        /// Get current bowling length based on target position relative to existing zones
        /// </summary>
        public BowlingLength GetCurrentBowlingLength()
        {
            if (target == null || pitchingArea == null)
            {
                Debug.LogWarning("⚠️ Target or Pitching Area is null!");
                return BowlingLength.GoodLength;
            }
            
            // Get target position
            Vector3 targetPos = target.position;
            
            // Check which zone the target is closest to
            float minDistance = float.MaxValue;
            BowlingLength closestLength = BowlingLength.GoodLength;
            string closestZoneName = "Unknown";
            
            // Check Yorker zone
            if (yorkerZone != null)
            {
                float distance = Vector3.Distance(targetPos, yorkerZone.position);
                Debug.Log($"🎯 Target distance to Yorker zone: {distance:F2} (Zone pos: {yorkerZone.position})");
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestLength = BowlingLength.Yorker;
                    closestZoneName = "Yorker";
                }
            }
            else
            {
                Debug.LogWarning("⚠️ Yorker zone reference is null!");
            }
            
            // Check Full Toss zone
            if (fullTossZone != null)
            {
                float distance = Vector3.Distance(targetPos, fullTossZone.position);
                Debug.Log($"🎯 Target distance to Full Toss zone: {distance:F2} (Zone pos: {fullTossZone.position})");
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestLength = BowlingLength.FullLength;
                    closestZoneName = "Full Toss";
                }
            }
            else
            {
                Debug.LogWarning("⚠️ Full Toss zone reference is null!");
            }
            
            // Check Length zone (Good Length)
            if (lengthZone != null)
            {
                float distance = Vector3.Distance(targetPos, lengthZone.position);
                Debug.Log($"🎯 Target distance to Length zone: {distance:F2} (Zone pos: {lengthZone.position})");
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestLength = BowlingLength.GoodLength;
                    closestZoneName = "Length";
                }
            }
            else
            {
                Debug.LogWarning("⚠️ Length zone reference is null!");
            }
            
            // Check Slot zone (Short Length)
            if (slotZone != null)
            {
                float distance = Vector3.Distance(targetPos, slotZone.position);
                Debug.Log($"🎯 Target distance to Slot zone: {distance:F2} (Zone pos: {slotZone.position})");
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestLength = BowlingLength.ShortLength;
                    closestZoneName = "Slot";
                }
            }
            else
            {
                Debug.LogWarning("⚠️ Slot zone reference is null!");
            }
            
            // Check Short zone (Bouncer)
            if (shortZone != null)
            {
                float distance = Vector3.Distance(targetPos, shortZone.position);
                Debug.Log($"🎯 Target distance to Short zone: {distance:F2} (Zone pos: {shortZone.position})");
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestLength = BowlingLength.Bouncer;
                    closestZoneName = "Short";
                }
            }
            else
            {
                Debug.LogWarning("⚠️ Short zone reference is null!");
            }
            
            // Real-time debug output
            Debug.Log($"🎯 TARGET STATUS: I'm at position {targetPos}, closest to {closestZoneName} zone (distance: {minDistance:F2}), using {closestLength} bowling settings!");
            
            return closestLength;
        }
        
        /// <summary>
        /// Apply rotation to ball spawn point based on bowling length
        /// </summary>
        void ApplyBowlingRotation(BowlingLength length)
        {
            if (spawnPoint == null) return;
            
            float rotationX = GetRotationForLength(length);
            
            // 🎯 NEW: Calculate Y rotation to aim at target (left/right angle)
            float rotationY = 0f;
            if (target != null)
            {
                Vector3 directionToTarget = (target.position - spawnPoint.position).normalized;
                rotationY = Mathf.Atan2(directionToTarget.x, directionToTarget.z) * Mathf.Rad2Deg;
            }
            
            // 🎯 CORRECT: Use X rotation for downward angle + Y rotation for left/right aiming
            spawnPoint.rotation = Quaternion.Euler(rotationX, rotationY, spawnPoint.rotation.eulerAngles.z);
            Debug.Log($"🎯 Applied {length} rotation: X={rotationX}° (downward), Y={rotationY}° (left/right) to spawn point");
        }
        
        /// <summary>
        /// Get X rotation value for a specific bowling length from single BallSettings component
        /// X rotation controls downward angle (pitch angle) of the ball trajectory
        /// </summary>
        float GetRotationForLength(BowlingLength length)
        {
            if (ballSettingsSO == null) return 0f;
            
            // Use dynamic rotation based on current ball speed
            return 0f; // X rotation removed - no longer used
        }
        
        /// <summary>
        /// Setup ball physics components
        /// </summary>
        void SetupBallPhysics()
        {
            ballRigidbody = ball.GetComponent<Rigidbody>();
            if (ballRigidbody == null)
            {
                ballRigidbody = ball.AddComponent<Rigidbody>();
            }
            
            // ?? IMPROVED: Configure rigidbody for realistic cricket ball physics with smooth movement
            ballRigidbody.mass = 0.16f; // Standard cricket ball weight
            ballRigidbody.linearDamping = 0.02f; // Very low drag for realistic air resistance
            ballRigidbody.angularDamping = 0.02f; // Low angular drag
            ballRigidbody.useGravity = true; // Always use gravity for cricket ball
            ballRigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
            ballRigidbody.interpolation = useSmoothMovement ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None;
            ballRigidbody.solverIterations = 10; // Higher solver iterations for smoother physics
            ballRigidbody.solverVelocityIterations = 10; // Higher velocity iterations for better accuracy
            
            // Add collider if missing
            if (ball.GetComponent<Collider>() == null)
            {
                SphereCollider sphereCollider = ball.AddComponent<SphereCollider>();
                sphereCollider.radius = 0.036f; // Standard cricket ball radius
            }
            
            // ?? NEW: Add bounce physics component for realistic cricket ball bouncing
            CricketBallBounce bounceComponent = ball.GetComponent<CricketBallBounce>();
            if (bounceComponent == null)
            {
                bounceComponent = ball.AddComponent<CricketBallBounce>();
                bounceComponent.Initialize(this);
            }
            
            // Add trail renderer for visual effect
            TrailRenderer trail = ball.GetComponent<TrailRenderer>();
            if (trail == null)
            {
                trail = ball.AddComponent<TrailRenderer>();
            }
            
            // OPTIMIZED: Only set trail settings if they haven't been configured in Inspector
            if (trail.time <= 0f) trail.time = 0.5f; // Only set default if not configured
            if (trail.startWidth <= 0f) trail.startWidth = 0.08f; // Only set default if not configured
            if (trail.endWidth <= 0f) trail.endWidth = 0.02f; // Only set default if not configured
            if (trail.minVertexDistance <= 0f) trail.minVertexDistance = 0.1f; // Only set default if not configured
            trail.emitting = true; // Always ensure trail is emitting
            
            // ?? FORCE: Create and apply light white visible material
            Material trailMaterial = new Material(Shader.Find("Sprites/Default"));
            trailMaterial.color = new Color(1f, 1f, 1f, 0.6f); // Light white with 60% opacity
            trail.sharedMaterial = trailMaterial;
            
            // ?? FORCE: Set trail colors to light white and moderately transparent
            trail.startColor = new Color(1f, 1f, 1f, 0.7f); // Light white with 70% opacity
            trail.endColor = new Color(1f, 1f, 1f, 0.1f); // White with 10% opacity (fade out)
            
            // ?? FORCE: Ensure trail is visible
            trail.enabled = true;
            trail.autodestruct = false;
            
            Debug.Log("?? Trail renderer FORCED to light white visible color!");
            // FIXED: Use sharedMaterial for prefab access
            if (trail.sharedMaterial != null)
            {
                Debug.Log($"?? Trail material color: {trail.sharedMaterial.color}");
            }
            else
            {
                Debug.Log("?? Trail material: NULL");
            }
            Debug.Log($"?? Trail start color: {trail.startColor}");
            Debug.Log($"?? Trail end color: {trail.endColor}");
        }
        
        /// <summary>
        /// ?? NEW: Setup physics for instantiated ball instances
        /// </summary>
        void SetupBallPhysicsForInstance(GameObject ballInstance)
        {
            Rigidbody instanceRigidbody = ballInstance.GetComponent<Rigidbody>();
            if (instanceRigidbody == null)
            {
                instanceRigidbody = ballInstance.AddComponent<Rigidbody>();
            }
            
            // ?? CRITICAL: Configure rigidbody for realistic cricket ball physics with smooth movement
            instanceRigidbody.mass = 0.16f; // Standard cricket ball weight
            instanceRigidbody.linearDamping = 0.02f; // Very low drag for realistic air resistance
            instanceRigidbody.angularDamping = 0.02f; // Low angular drag
            instanceRigidbody.useGravity = true; // Always use gravity for cricket ball
            instanceRigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
            instanceRigidbody.interpolation = useSmoothMovement ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None;
            instanceRigidbody.solverIterations = 10; // Higher solver iterations for smoother physics
            instanceRigidbody.solverVelocityIterations = 10; // Higher velocity iterations for better accuracy
            
            // Add collider if missing
            if (ballInstance.GetComponent<Collider>() == null)
            {
                SphereCollider sphereCollider = ballInstance.AddComponent<SphereCollider>();
                sphereCollider.radius = 0.036f; // Standard cricket ball radius
            }
            
            // ?? CRITICAL: Add bounce physics component for realistic cricket ball bouncing
            CricketBallBounce bounceComponent = ballInstance.GetComponent<CricketBallBounce>();
            if (bounceComponent == null)
            {
                bounceComponent = ballInstance.AddComponent<CricketBallBounce>();
                bounceComponent.Initialize(this);
                Debug.Log("🎳 Bounce component added to ball instance");
            }
            else
            {
                bounceComponent.Initialize(this);
                Debug.Log("🎳 Bounce component initialized for ball instance");
            }
            
            // 🎯 CRITICAL: Add BallWicketCollision component for wicket breaking
            if (ballInstance.GetComponent<BallWicketCollision>() == null)
            {
                ballInstance.AddComponent<BallWicketCollision>();
                Debug.Log("🎳 BallWicketCollision component added to ball instance");
            }
            else
            {
                Debug.Log("🎳 BallWicketCollision already exists on ball");
            }
            
            // Add trail renderer for visual effect
            TrailRenderer trail = ballInstance.GetComponent<TrailRenderer>();
            if (trail == null)
            {
                trail = ballInstance.AddComponent<TrailRenderer>();
            }
            
            // OPTIMIZED: Only set trail settings if they haven't been configured in Inspector
            if (trail.time <= 0f) trail.time = 0.5f; // Only set default if not configured
            if (trail.startWidth <= 0f) trail.startWidth = 0.08f; // Only set default if not configured
            if (trail.endWidth <= 0f) trail.endWidth = 0.02f; // Only set default if not configured
            if (trail.minVertexDistance <= 0f) trail.minVertexDistance = 0.1f; // Only set default if not configured
            trail.emitting = true; // Always ensure trail is emitting
            
            // ?? FORCE: Create and apply light white visible material
            Material trailMaterial = new Material(Shader.Find("Sprites/Default"));
            trailMaterial.color = new Color(1f, 1f, 1f, 0.6f); // Light white with 60% opacity
            trail.sharedMaterial = trailMaterial;
            
            // ?? FORCE: Set trail colors to light white and moderately transparent
            trail.startColor = new Color(1f, 1f, 1f, 0.7f); // Light white with 70% opacity
            trail.endColor = new Color(1f, 1f, 1f, 0.1f); // White with 10% opacity (fade out)
            
            // ?? FORCE: Ensure trail is visible
            trail.enabled = true;
            trail.autodestruct = false;
            
            Debug.Log("?? Instance trail renderer FORCED to light white visible color!");
            Debug.Log($"?? Trail material color: {trail.material.color}");
            Debug.Log($"?? Trail start color: {trail.startColor}");
            Debug.Log($"?? Trail end color: {trail.endColor}");
            
            Debug.Log("?? Ball instance physics setup complete!");
        }
        
        /// <summary>
        /// Handle input controls
        /// </summary>
        void HandleInput()
        {
            // ?? NEW: Instantiate new ball with S key (only if manual input enabled)
            if (enableManualKeyInput && Input.GetKeyDown(KeyCode.S))
            {
                Debug.Log("🎯 S key pressed - Creating new ball");
                InstantiateNewBall();
            }
            
            // ?? NEW: Bowl current ball with SPACE key (only if manual input enabled)
            if (enableManualKeyInput && Input.GetKeyDown(KeyCode.Space))
            {
                Debug.Log("🎯 SPACE key pressed - Attempting to bowl ball");
                BowlCurrentBall();
            }
            
            // Manual ball reset with R key (kept for compatibility)
            if (Input.GetKeyDown(KeyCode.R))
            {
                Debug.Log("🎯 R key pressed - Resetting ball");
                ResetBall();
            }
            
            // ?? NEW: Dynamic settings are now integrated directly into the script
        }
        
        /// <summary>
        /// ?? NEW APPROACH: Simple ball management system
        /// </summary>
        
        // ?? NEW: Ball lifecycle management
        private GameObject currentBallInstance;
        private bool ballIsBowled = false;
        
        /// <summary>
        /// ?? NEW: Instantiate new ball at spawn point
        /// </summary>
        public void InstantiateNewBall()
        {
            Debug.Log("🎯 InstantiateNewBall called");
            
            // Destroy existing ball instance if any (NOT the prefab reference!)
            // This is needed for manual ball creation (S key) - different from auto-destroy
            if (currentBallInstance != null)
            {
                Destroy(currentBallInstance);
                currentBallInstance = null;
                Debug.Log("🎯 Destroyed previous ball instance for new ball creation");
            }
            
            // Instantiate new ball at spawn point
            if (ball != null)
            {
                // Determine which spawn point to use based on manual input setting
                Transform spawnPointToUse = null;
                string spawnPointType = "";
                
                if (enableManualKeyInput)
                {
                    // Use BowlingController's spawn point for manual testing
                    spawnPointToUse = spawnPoint;
                    spawnPointType = "Manual (BowlingController)";
                    Debug.Log($"🎯 SPAWN POINT: Manual mode enabled - using BowlingController spawn point");
                }
                else
                {
                    // Use PlayerAnimationController's spawn point for animation-driven bowling
                    PlayerAnimationController animController = GetPlayerAnimationController();
                    if (animController != null)
                    {
                        // CRITICAL: Force refresh spawn point to ensure we use scene instance, not prefab
                        animController.ForceRefreshSpawnPointReference();
                        
                        spawnPointToUse = animController.GetAnimationSpawnPoint();
                        if (spawnPointToUse != null)
                        {
                            spawnPointType = "Animation (PlayerAnimationController)";
                            Debug.Log($"🎯 SPAWN POINT: Animation mode - using PlayerAnimationController spawn point: {spawnPointToUse.name}");
                            
                            // Refresh the spawn point position to get current world position
                            animController.RefreshSpawnPointPosition();
                            
                            // Get the current animated position
                            Vector3 animatedPosition = animController.GetCurrentAnimatedSpawnPosition();
                            Debug.Log($"🎯 ANIMATED POSITION: Current {spawnPointToUse.name} position: {animatedPosition}");
                        }
                        else
                        {
                            // Animation spawn point not assigned
                            spawnPointToUse = spawnPoint;
                            spawnPointType = "Fallback (Animation Spawn Point Not Assigned)";
                            Debug.LogWarning("🎯 PlayerAnimationController found but Animation Spawn Point is not assigned! Using BowlingController spawn point as fallback");
                        }
                    }
                    else
                    {
                        // Fallback to BowlingController's spawn point if PlayerAnimationController not found
                        spawnPointToUse = spawnPoint;
                        spawnPointType = "Fallback (PlayerAnimationController Not Found)";
                        Debug.LogWarning("🎯 PlayerAnimationController not found - using BowlingController spawn point as fallback");
                    }
                }
                
                if (spawnPointToUse != null)
                {
                    // Get the current world position of the spawn point (important for animated bowlers)
                    Vector3 currentSpawnPosition = Vector3.zero;
                    Quaternion currentSpawnRotation = Quaternion.identity;
                    
                    // For animated spawn points, ensure we get the current animated world position
                    if (spawnPointType.Contains("Animation"))
                    {
                        // CRITICAL: Use the animated position that was already calculated
                        PlayerAnimationController animController = GetPlayerAnimationController();
                        if (animController != null)
                        {
                            currentSpawnPosition = animController.GetCurrentAnimatedSpawnPosition();
                            Debug.Log($"🎯 ANIMATED SPAWN: Using ANIMATED world position: {currentSpawnPosition}");
                        }
                        else
                        {
                            currentSpawnPosition = spawnPointToUse.position;
                            Debug.Log($"🎯 ANIMATED SPAWN: Fallback to transform position: {currentSpawnPosition}");
                        }
                        
                        currentSpawnRotation = spawnPointToUse.rotation;
                        
                        Debug.Log($"🎯 ANIMATED SPAWN: Bone {spawnPointToUse.name}, Position: {currentSpawnPosition}");
                        Debug.Log($"🎯 ANIMATED SPAWN: Spawn point parent: {(spawnPointToUse.parent != null ? spawnPointToUse.parent.name : "NULL")}");
                    }
                    else
                    {
                        // Manual spawn points
                        currentSpawnPosition = spawnPointToUse.position;
                        currentSpawnRotation = spawnPointToUse.rotation;
                    }
                    
                    currentBallInstance = Instantiate(ball, currentSpawnPosition, currentSpawnRotation);
                    Debug.Log($"🎯 DYNAMIC SPAWN: Ball instantiated at {spawnPointType} position: {currentSpawnPosition}");
                    Debug.Log($"🎯 DYNAMIC SPAWN: Spawn rotation: {currentSpawnRotation.eulerAngles}");
                    Debug.Log($"🎯 Ball instance created: {currentBallInstance != null}");
                }
                else
                {
                    Debug.LogError("🎯 No spawn point available! Please assign spawn points in the Inspector.");
                    return;
                }
                
                // Reset state
                ballIsBowled = false;
                hasLanded = false;
                ResetBounceState();
                
                // ?? FIXED: Keep original ball prefab reference, only update instance references
                ballRigidbody = currentBallInstance.GetComponent<Rigidbody>();
                Debug.Log($"🎯 Ball rigidbody: {ballRigidbody != null}");
                
                // ?? CRITICAL: Setup physics for the new ball instance
                SetupBallPhysicsForInstance(currentBallInstance);
                
                // ?? NEW: Apply dynamic bowling settings to the instantiated ball
                BallSettings ballSettings = currentBallInstance.GetComponent<BallSettings>();
                if (ballSettings != null)
                {
                    Debug.Log("🎯 BallSettings found on ball instance");
                    ApplyDynamicBowlingSettings(ballSettings);
                }
                else
                {
                    Debug.LogWarning("🎯 No BallSettings component found on ball instance!");
                }
                
                // Ensure the target has a trigger to notify exact contact
                if (target != null)
                {
                    var trigger = target.GetComponent<TargetHitTrigger>();
                    if (trigger == null)
                    {
                        trigger = target.gameObject.AddComponent<TargetHitTrigger>();
                    }
                    trigger.GetType().GetField("bowlingController", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)?.SetValue(trigger, this);
                }

                Debug.Log("🎯 Ball ready for bowling! Press SPACE to bowl");
            }
            else
            {
                Debug.LogError($"🎯 Cannot instantiate ball - ball: {ball != null}, spawnPoint: {spawnPoint != null}");
            }
        }
        
        /// <summary>
        /// ?? NEW: Bowl the current ball
        /// </summary>
        public void BowlCurrentBall()
        {
            if (currentBallInstance == null)
            {
                return;
            }
            
            if (ballIsBowled)
            {
                return;
            }
            
            // CRITICAL: Re-enable physics when bowling starts
            Rigidbody ballRigidbody = currentBallInstance.GetComponent<Rigidbody>();
            if (ballRigidbody != null)
            {
                ballRigidbody.isKinematic = false; // Re-enable physics for bowling
            }
            
            ballIsBowled = true;
            
            // Start bowling process
            StartCoroutine(BowlAndDestroy());
        }
        
        /// <summary>
        /// ?? NEW: Bowl ball - ball will destroy itself after 5 seconds (only if auto-destroy enabled)
        /// </summary>
        IEnumerator BowlAndDestroy()
        {
            Debug.Log("🏏 === BOWL AND DESTROY STARTED ===");
            
            // Bowl the ball
            Debug.Log("🏏 Starting BowlToTarget coroutine...");
            yield return StartCoroutine(BowlToTarget());
            
            Debug.Log("🏏 ✅ BowlToTarget completed - ball has finished bowling to target");
            
            // Wait for ball to land and bounce
            Debug.Log("🏏 Starting WaitForLanding coroutine...");
            yield return StartCoroutine(WaitForLanding());
            
            Debug.Log("🏏 ✅ WaitForLanding completed - ball has landed and finished bouncing");
            
            // Alternative: Simple fixed wait (uncomment if WaitForLanding is problematic)
            // Debug.Log("🏏 Using simple fixed wait instead of WaitForLanding...");
            // yield return new WaitForSeconds(2f);
            // Debug.Log("🏏 ✅ Fixed wait completed");
            
            // Reset ball state so new balls can be bowled
            ballIsBowled = false;
            hasLanded = false;
            ResetBounceState();
            
            // Check if auto-destroy is enabled
            Debug.Log($"🏏 Checking auto-destroy: ballSettingsSO = {(ballSettingsSO != null ? "FOUND" : "NULL")}");
            if (ballSettingsSO != null)
            {
                Debug.Log($"🏏 EnableAutoDestroy = {ballSettingsSO.EnableAutoDestroy}");
                Debug.Log($"🏏 DestroyDelay = {ballSettingsSO.DestroyDelay}");
            }
            
            if (ballSettingsSO != null && ballSettingsSO.EnableAutoDestroy)
            {
                Debug.Log($"🏏 ✅ Auto-destroy ENABLED - destroying ball in {ballSettingsSO.DestroyDelay} seconds");
                Debug.Log($"🏏 Current ball instance: {(currentBallInstance != null ? currentBallInstance.name : "NULL")}");
                
                // Wait for destroy delay
                yield return new WaitForSeconds(ballSettingsSO.DestroyDelay);
                
                Debug.Log("🏏 Destroy delay completed - attempting to destroy ball");
                
                // Destroy the ball
                if (currentBallInstance != null)
                {
                    Debug.Log($"🏏 🗑️ DESTROYING ball instance: {currentBallInstance.name}");
                    Destroy(currentBallInstance);
                    currentBallInstance = null;
                    Debug.Log("🏏 ✅ Ball instance destroyed and reference cleared");
                }
                else
                {
                    Debug.LogError("🏏 ❌ Ball instance is NULL - cannot destroy");
                }
            }
            else
            {
                Debug.Log("🏏 ❌ Auto-destroy DISABLED - ball remains in scene");
                if (ballSettingsSO == null)
                {
                    Debug.LogError("🏏 ❌ BallSettingsSO is NULL!");
                }
            }
            
            Debug.Log("🏏 === BOWL AND DESTROY COMPLETED ===");
            Debug.Log("?? Ball state reset - ready for new ball! Press S to create a new ball when ready!");
        }
        
        /// <summary>
        /// ?? NEW: Start continuous bowling test (kept for compatibility)
        /// </summary>
        public void StartContinuousBowling()
        {
            Debug.Log("?? Continuous bowling disabled - use S for new ball, SPACE to bowl!");
        }
        
        /// <summary>
        /// ?? NEW: Stop continuous bowling test (kept for compatibility)
        /// </summary>
        public void StopContinuousBowling()
        {
            Debug.Log("?? Continuous bowling disabled - use S for new ball, SPACE to bowl!");
        }
        
        /// <summary>
        /// ?? WORKING: Bowl the ball to target with working return system
        /// </summary>
        IEnumerator BowlToTarget()
        {
            Debug.Log("?? Bowling ball to target...");
            
            // ?? FIXED: Use current ball instance instead of prefab reference
            GameObject ballToBowl = currentBallInstance != null ? currentBallInstance : ball;
            Rigidbody ballRigidbodyToUse = ballToBowl.GetComponent<Rigidbody>();
            
            if (ballToBowl == null)
            {
                Debug.LogError("?? No ball to bowl!");
                yield break;
            }
            
            // ?? NEW: Get ball settings from BallSettings component
            BallSettings ballSettings = ballToBowl.GetComponent<BallSettings>();
            if (ballSettings == null)
            {
                Debug.LogError("?? Ball missing BallSettings component! Please add BallSettings to your BALL prefab.");
                yield break;
            }
            
            // ?? SAFETY: Ensure ball is at correct starting position
            // 🎯 DYNAMIC: Use the same spawn point that was used for instantiation
            // CRITICAL: Always get the CURRENT spawn point, not a cached reference
            Transform correctSpawnPoint;
            if (enableManualKeyInput)
            {
                correctSpawnPoint = spawnPoint;
            }
            else
            {
                // Always get the current spawn point from the bowler
                Transform currentSpawn = GetCurrentBowlerSpawnPosition();
                if (currentSpawn != null)
                {
                    correctSpawnPoint = currentSpawn;
                    // Update internal references to match
                    spawnPoint = currentSpawn;
                    ballSpawnPoint = currentSpawn;
                }
                else
                {
                    PlayerAnimationController animCtrl = GetPlayerAnimationController();
                    correctSpawnPoint = animCtrl != null ? animCtrl.GetAnimationSpawnPoint() : spawnPoint;
                }
            }
            
            // Get the current animated position for comparison (important for animated bowlers)
            Vector3 expectedPosition;
            if (enableManualKeyInput)
            {
                expectedPosition = spawnPoint.position;
            }
            else
            {
                PlayerAnimationController animController = GetPlayerAnimationController();
                if (animController != null)
                {
                    expectedPosition = animController.GetCurrentAnimatedSpawnPosition();
                }
                else
                {
                    expectedPosition = correctSpawnPoint.position;
                }
            }
            
            if (Vector3.Distance(ballToBowl.transform.position, expectedPosition) > 0.1f)
            {
                Debug.LogWarning($"?? Ball not at correct spawn position! Forcing reset to current animated position...");
                ballToBowl.transform.position = expectedPosition;
                if (ballRigidbodyToUse != null)
                {
                    ballRigidbodyToUse.linearVelocity = Vector3.zero;
                    ballRigidbodyToUse.angularVelocity = Vector3.zero;
                }
            }
            
            // Reset landing flag and bounce state
            hasLanded = false;
            ResetBounceState();
            
            // Reset delivery system for new ball
            ResetDeliverySystem();
            
            // Calculate trajectory to target
            Vector3 targetPosition = target.position;
            
            // Use the same spawn point that was used for ball creation (important for animated bowlers)
            Vector3 startPosition;
            if (enableManualKeyInput)
            {
                startPosition = spawnPoint.position;
            }
            else
            {
                PlayerAnimationController animController = GetPlayerAnimationController();
                if (animController != null)
                {
                    // Refresh the spawn point position to get current world position
                    animController.RefreshSpawnPointPosition();
                    startPosition = animController.GetCurrentAnimatedSpawnPosition();
                    Debug.Log($"🎯 TRAJECTORY: Using current animated spawn position: {startPosition}");
                }
                else
                {
                    startPosition = ballToBowl.transform.position;
                    Debug.LogWarning("🎯 TRAJECTORY: No PlayerAnimationController found, using ball position as fallback");
                }
            }
            
            // 🎯 VERIFICATION: Log spawn and start positions
            Debug.Log($"═══════════════════════════════════════════════════════");
            Debug.Log($"🎯 DYNAMIC SPAWN VERIFICATION:");
            Debug.Log($"   Spawn Point Transform Position: {correctSpawnPoint.position}");
            Debug.Log($"   Ball Current Position: {ballToBowl.transform.position}");
            Debug.Log($"   Trajectory Start Position: {startPosition}");
            Debug.Log($"   Target Position: {targetPosition}");
            Debug.Log($"   Ball vs Trajectory Match: {Vector3.Distance(ballToBowl.transform.position, startPosition) < 0.01f}");
            
            // Calculate horizontal distance to target
            Vector3 horizontalStart = new Vector3(startPosition.x, 0, startPosition.z);
            Vector3 horizontalTarget = new Vector3(targetPosition.x, 0, targetPosition.z);
            float horizontalDistance = Vector3.Distance(horizontalStart, horizontalTarget);
            
            // 🎯 BOWLING DIRECTION: Calculate actual bowling direction
            Vector3 bowlingDirection = (targetPosition - startPosition).normalized;
            Vector3 bowlingDirectionHorizontal = (horizontalTarget - horizontalStart).normalized;
            Debug.Log($"   Bowling Direction (3D): {bowlingDirection}");
            Debug.Log($"   Bowling Direction (Horizontal): {bowlingDirectionHorizontal}");
            Debug.Log($"═══════════════════════════════════════════════════════");

            // ?? LENGTH-BASED LOGIC: compute length factor using wicket references if available
            if (umpireWicket != null && batsmanWicket != null)
            {
                Vector3 umpXZ = new Vector3(umpireWicket.position.x, 0f, umpireWicket.position.z);
                Vector3 batXZ = new Vector3(batsmanWicket.position.x, 0f, batsmanWicket.position.z);
                Vector3 targXZ = new Vector3(targetPosition.x, 0f, targetPosition.z);
                float totalPitch = Mathf.Max(0.01f, Vector3.Distance(umpXZ, batXZ));
                float distFromUmpire = Vector3.Distance(umpXZ, targXZ);
                currentLength01 = Mathf.Clamp01(distFromUmpire / totalPitch);
            }
            else
            {
                // Fallback: estimate using spawn as umpire end and target relative to it
                Vector3 umpXZ = new Vector3(ballSpawnPoint.position.x, 0f, ballSpawnPoint.position.z);
                Vector3 batXZ = new Vector3(targetPosition.x, 0f, targetPosition.z + Mathf.Sign((targetPosition - ballSpawnPoint.position).z) * 10f);
                float totalPitch = Mathf.Max(0.01f, Vector3.Distance(umpXZ, batXZ));
                float distFromUmpire = Vector3.Distance(umpXZ, new Vector3(targetPosition.x, 0f, targetPosition.z));
                currentLength01 = Mathf.Clamp01(distFromUmpire / totalPitch);
            }
            
            // ?? NEW: Apply dynamic bowling settings FIRST
            Debug.Log($"<color=#FFD700>🔍 CALLING ApplyDynamicBowlingSettings...</color>");
            Debug.Log($"<color=#FF0000>🚨 CRITICAL DEBUG: ballSettings reference = {ballSettings != null}</color>");
            ApplyDynamicBowlingSettings(ballSettings);
            Debug.Log($"<color=#FFD700>🔍 ApplyDynamicBowlingSettings completed!</color>");
            
            // Check if ballSettingsSO is assigned
            if (ballSettingsSO == null)
            {
                Debug.LogError("🚨 ballSettingsSO is not assigned in BowlToTarget! Please assign the BallSettingsSO ScriptableObject in the Inspector.");
                yield break; // Exit the coroutine
            }
            
            // ?? NEW: Read ball speed from speed controller if available, otherwise use ball settings
            float ballSpeed;
            if (speedController != null)
            {
                ballSpeed = speedController.GetCurrentSpeed();
                Debug.Log($"🎯 SPEED FROM SLIDER: {ballSpeed} m/s");
            }
            else
            {
                ballSpeed = ballSettingsSO.GlobalBallSpeed;
                Debug.Log($"🎯 SPEED FROM BALL SETTINGS: {ballSpeed} m/s");
            }
            float arcHeight = ballSettingsSO.ArcHeight;
            // Use global gravity (no longer per-section)
            float gravity = ballSettingsSO.Gravity;
            
            // 🎯 DELIVERY TRAJECTORY: Calculate delivery-modified target position
            Vector3 finalTargetPosition = CalculateDeliveryTrajectory(startPosition, targetPosition, ballSpeed);
            
            // 🎯 ANALYTIC BALLISTIC SOLVER: Compute exact initial velocity to hit swing-modified target with given speed
            Vector3 initialVelocity = SolveBallisticVelocity(startPosition, finalTargetPosition, ballSpeed, gravity, false /*low arc*/);
            
            // If no feasible solution with this speed (too fast/too slow), gently reduce speed until feasible
            if (initialVelocity == Vector3.zero)
            {
                float trySpeed = ballSpeed;
                for (int i = 0; i < 6; i++)
                {
                    trySpeed *= 0.95f; // reduce 5% and try again
                    initialVelocity = SolveBallisticVelocity(startPosition, finalTargetPosition, trySpeed, gravity, false);
                    if (initialVelocity != Vector3.zero) { ballSpeed = trySpeed; break; }
                }
            }
            
            // 🎯 SPEED 12 FIX: Special handling for speed 12 which seems to have targeting issues
            if (Mathf.Abs(ballSpeed - 12f) < 0.5f)
            {
                // Try high-arc solution for speed 12 if low-arc failed
                if (initialVelocity == Vector3.zero)
                {
                    initialVelocity = SolveBallisticVelocity(startPosition, finalTargetPosition, ballSpeed, gravity, true);
                }
                
                // Apply extra horizontal damping for speed 12
                if (initialVelocity != Vector3.zero)
                {
                    Vector3 horizontalVel = new Vector3(initialVelocity.x, 0, initialVelocity.z);
                    horizontalVel *= 0.92f; // 8% horizontal speed reduction for speed 12
                    initialVelocity = new Vector3(horizontalVel.x, initialVelocity.y, horizontalVel.z);
                    Debug.Log($"🎯 SPEED 12 FIX: Applied extra horizontal damping, new speed = {initialVelocity.magnitude:F1}");
                }
            }
            
            // If still zero, fall back to prior simple approach to avoid stalling
            if (initialVelocity == Vector3.zero)
            {
                Vector3 toTargetXZ = new Vector3(targetPosition.x - startPosition.x, 0f, targetPosition.z - startPosition.z);
                float fallbackTime = Mathf.Max(0.1f, toTargetXZ.magnitude / Mathf.Max(1f, ballSpeed));
                float yVel = (targetPosition.y - startPosition.y + 0.5f * gravity * fallbackTime * fallbackTime) / fallbackTime;
                Vector3 dirXZ = toTargetXZ.normalized;
                Vector3 velXZ = dirXZ * (toTargetXZ.magnitude / fallbackTime);
                initialVelocity = new Vector3(velXZ.x, yVel, velXZ.z);
            }
            
            // 🎯 CRITICAL: Use the correct spawn point for trajectory calculation
            // Ensure we're using the current spawn point, not a stale reference
            Transform trajectorySpawnPoint = correctSpawnPoint != null ? correctSpawnPoint : spawnPoint;
            
            // 🎯 SIMPLIFIED: Use spawn point's forward direction directly (includes both X and Y rotation)
            Vector3 horizontalDirection;
            if (trajectorySpawnPoint != null)
            {
                // Use spawn point's forward direction which already includes both X and Y rotation
                horizontalDirection = trajectorySpawnPoint.forward;
                // Remove Y component to keep it horizontal
                horizontalDirection.y = 0f;
                horizontalDirection = horizontalDirection.normalized;
                
                Debug.Log($"🎯 ROTATION APPLIED: Spawn rotation {trajectorySpawnPoint.rotation.eulerAngles}, Forward direction {trajectorySpawnPoint.forward}, Horizontal direction {horizontalDirection}");
            }
            else
            {
                // Fallback to direct target direction
                horizontalDirection = (horizontalTarget - horizontalStart).normalized;
            }
            
            // 🎯 SIMPLIFIED X ROTATION: Apply smaller, more controlled rotation effect (post ballistic)
            float preAdjustY = initialVelocity.y;
            float rotationXRadians = trajectorySpawnPoint != null ? trajectorySpawnPoint.rotation.eulerAngles.x * Mathf.Deg2Rad : 0f;
            float rotationEffect = Mathf.Sin(rotationXRadians); // This gives us the downward component
            
            // 🎯 REDUCED ROTATION EFFECT: Much smaller impact on trajectory
            float rotationMultiplier = 0.1f; // Reduced from 0.5f to 0.1f
            float adjustedYVelocity = initialVelocity.y + (rotationEffect * ballSpeed * rotationMultiplier);

            // Debug log using correct spawn point
            if (trajectorySpawnPoint != null && Mathf.Abs(trajectorySpawnPoint.rotation.eulerAngles.x) > 1f)
            {
                Debug.Log($"🎯 X ROTATION: {trajectorySpawnPoint.rotation.eulerAngles.x:F1}°, Effect: {rotationEffect:F3}, Y Velocity: {preAdjustY:F2} → {adjustedYVelocity:F2}");
            }

            // 🎯 HIGH-SPEED DOWNWARD ASSIST: ensure >14 m/s lands on target by adding extra downward component
            if (ballSpeed >= 14f)
            {
                // Add small progressive downward adjustment proportional to overspeed
                float overspeed = Mathf.Clamp01((ballSpeed - 14f) / 8f); // 0..1 around 14..22 m/s
                float downwardAssist = overspeed * ballSpeed * 0.08f; // gentle but effective
                adjustedYVelocity -= downwardAssist;
                Debug.Log($"🎯 HIGH-SPEED ASSIST: speed={ballSpeed:F1}, overspeed={overspeed:F2}, assist={downwardAssist:F2}, Y={preAdjustY:F2}->{adjustedYVelocity:F2}");
            }
            
            // 🎯 DEBUG: Log rotation effect
            if (spawnPoint != null && Mathf.Abs(spawnPoint.rotation.eulerAngles.x) > 1f)
            {
                Debug.Log($"🎯 X ROTATION: {spawnPoint.rotation.eulerAngles.x:F1}°, Effect: {rotationEffect:F3}, Y Velocity: {preAdjustY:F2} → {adjustedYVelocity:F2}");
            }
            
            // 🎯 SPEED-BASED CORRECTION: mild damping at very high speeds
            float horizontalSpeed = new Vector2(initialVelocity.x, initialVelocity.z).magnitude;
            
            // Apply speed correction factor (higher speed needs more correction)
            float speedCorrectionFactor = 1f;
            if (ballSpeed > 12f)
            {
                // For high speeds, reduce horizontal speed to prevent overshooting
                speedCorrectionFactor = 0.82f + (15f - ballSpeed) * 0.015f; // Slightly stronger correction
                speedCorrectionFactor = Mathf.Clamp(speedCorrectionFactor, 0.68f, 1f);
            }
            
            horizontalSpeed *= speedCorrectionFactor;
            Vector3 horizontalVelocity = initialVelocity;
            horizontalVelocity.y = 0f;
            if (horizontalVelocity.sqrMagnitude > 0.0001f)
            {
                horizontalVelocity = horizontalVelocity.normalized * horizontalSpeed;
            }
            
            Debug.Log($"🎯 SPEED CORRECTION: BallSpeed={ballSpeed:F1}, CorrectionFactor={speedCorrectionFactor:F2}, FinalHorizontalSpeed={horizontalSpeed:F1}");
            
            // 🎯 BOUNCER FIX: Ensure minimum horizontal velocity even with extreme rotations
            float minHorizontalSpeed = 2f; // Minimum horizontal speed to prevent ball from going straight down
            if (horizontalVelocity.magnitude < minHorizontalSpeed)
            {
                Debug.LogWarning($"🎯 BOUNCER FIX: Horizontal velocity too low ({horizontalVelocity.magnitude:F2}), applying minimum speed");
                horizontalVelocity = horizontalDirection * minHorizontalSpeed;
            }
            
            // Combine velocities with rotation-adjusted Y velocity
            initialVelocity = new Vector3(horizontalVelocity.x, adjustedYVelocity, horizontalVelocity.z);
            
            // Compute timeToReach analytically from vertical motion; fallback to horizontal if needed
            float timeToReach;
            {
                float a_t = -0.5f * gravity;
                float b_t = adjustedYVelocity;
                float c_t = targetPosition.y - startPosition.y;
                float disc_t = b_t * b_t - 4f * a_t * c_t;
                if (disc_t >= 0f)
                {
                    float sqrt_t = Mathf.Sqrt(disc_t);
                    float t1 = (-b_t + sqrt_t) / (2f * a_t);
                    float t2 = (-b_t - sqrt_t) / (2f * a_t);
                    // choose the larger positive time
                    timeToReach = Mathf.Max(t1, t2);
                    if (timeToReach <= 0f || float.IsNaN(timeToReach))
                    {
                        timeToReach = horizontalDistance / Mathf.Max(0.1f, horizontalSpeed);
                    }
                }
                else
                {
                    timeToReach = horizontalDistance / Mathf.Max(0.1f, horizontalSpeed);
                }
            }
            
            // 🎯 DEBUG: Log trajectory calculations
            Debug.Log($"🎯 TRAJECTORY CALC: Distance={horizontalDistance:F1}m, Time={timeToReach:F2}s, HorizontalSpeed={horizontalSpeed:F1}m/s, YVelocity={adjustedYVelocity:F1}m/s");
            
            Debug.Log($"🎯 ROTATION EFFECT: X rotation {spawnPoint?.rotation.eulerAngles.x ?? 0f}°, Rotation effect {rotationEffect:F3}, Y velocity {preAdjustY:F2} → {adjustedYVelocity:F2}");
            
            // Apply velocity to ball with smooth movement
            Debug.Log($"🎯 APPLYING VELOCITY: UseRealisticPhysics={ballSettingsSO.UseRealisticPhysics}, useSmoothMovement={useSmoothMovement}");
            Debug.Log($"🎯 VELOCITY VALUES: initialVelocity={initialVelocity}, magnitude={initialVelocity.magnitude:F2}");
            Debug.Log($"🎯 RIGIDBODY STATUS: ballRigidbodyToUse={ballRigidbodyToUse != null}, isKinematic={ballRigidbodyToUse?.isKinematic}");
            
            // 🎯 SPEED BOOST: Set initial speed for speed boost system
            BallSpeedBoost ballSpeedBoost = currentBallInstance?.GetComponent<BallSpeedBoost>();
            if (ballSpeedBoost == null)
            {
                // Try to add BallSpeedBoost component if missing
                ballSpeedBoost = currentBallInstance?.AddComponent<BallSpeedBoost>();
                if (ballSpeedBoost != null)
                {
                    Debug.Log($"🎯 SPEED BOOST: Added BallSpeedBoost component to ball instance");
                }
            }
            
            if (ballSpeedBoost != null)
            {
                ballSpeedBoost.SetInitialSpeed(ballSpeed);
                ballSpeedBoost.CheckConfiguration(); // Debug configuration
                DeliveryType currentDelivery = deliverySystem?.GetCurrentDeliveryType() ?? DeliveryType.Flat;
                Debug.Log($"🎯 SPEED BOOST: Set initial speed to {ballSpeed:F1} m/s for {currentDelivery} delivery");
                Debug.Log($"🎯 DELIVERY TYPE: Currently using {currentDelivery} delivery system");
            }
            else
            {
                Debug.LogWarning($"🎯 SPEED BOOST: Could not add BallSpeedBoost component! Speed boost will not work.");
            }
            
            // 🎯 SWING TRAJECTORY: Calculate swing-modified trajectory (already calculated above)
            
             // 🎯 PATH FOLLOWER CHECK: Use PathFollower for ALL deliveries for 100% accuracy
            // PathFollower guarantees ball reaches target - more accurate than physics!
            bool useKinematicForCurvedPath = false;
            if (deliverySystem != null)
            {
                DeliveryType currentDelivery = deliverySystem.GetCurrentDeliveryType();
                if (currentDelivery == DeliveryType.Flat || currentDelivery == DeliveryType.SeamIn || currentDelivery == DeliveryType.SeamOut || 
                    currentDelivery == DeliveryType.Inswing || currentDelivery == DeliveryType.Outswing || 
                    currentDelivery == DeliveryType.LegSpin || currentDelivery == DeliveryType.OffSpin)
                {
                    // Get appropriate delivery component and check if path follower enabled
                    bool curvedEnabled = false;
                    if (currentDelivery == DeliveryType.Flat)
                    {
                        FlatDelivery flatDelivery = deliverySystem.GetComponent<FlatDelivery>() ?? deliverySystem.transform.GetComponent<FlatDelivery>();
                        curvedEnabled = flatDelivery != null && flatDelivery.IsCurvedPathEnabled();
                    }
                    else if (currentDelivery == DeliveryType.SeamIn)
                    {
                        SeamInDelivery seamInDelivery = deliverySystem.GetComponent<SeamInDelivery>() ?? deliverySystem.transform.GetComponent<SeamInDelivery>();
                        curvedEnabled = seamInDelivery != null && seamInDelivery.IsCurvedPathEnabled();
                    }
                    else if (currentDelivery == DeliveryType.SeamOut)
                    {
                        SeamOutDelivery seamOutDelivery = deliverySystem.GetComponent<SeamOutDelivery>() ?? deliverySystem.transform.GetComponent<SeamOutDelivery>();
                        curvedEnabled = seamOutDelivery != null && seamOutDelivery.IsCurvedPathEnabled();
                    }
                    else if (currentDelivery == DeliveryType.Inswing)
                    {
                        InswingDelivery inswingDelivery = deliverySystem.GetComponent<InswingDelivery>() ?? deliverySystem.transform.GetComponent<InswingDelivery>();
                        curvedEnabled = inswingDelivery != null && inswingDelivery.IsCurvedPathEnabled();
                    }
                    else if (currentDelivery == DeliveryType.Outswing)
                    {
                        OutswingDelivery outswingDelivery = deliverySystem.GetComponent<OutswingDelivery>() ?? deliverySystem.transform.GetComponent<OutswingDelivery>();
                        curvedEnabled = outswingDelivery != null && outswingDelivery.IsCurvedPathEnabled();
                    }
                    else if (currentDelivery == DeliveryType.LegSpin)
                    {
                        LegSpinDelivery legSpinDelivery = deliverySystem.GetComponent<LegSpinDelivery>() ?? deliverySystem.transform.GetComponent<LegSpinDelivery>();
                        curvedEnabled = legSpinDelivery != null && legSpinDelivery.IsCurvedPathEnabled();
                    }
                    else if (currentDelivery == DeliveryType.OffSpin)
                    {
                        OffSpinDelivery offSpinDelivery = deliverySystem.GetComponent<OffSpinDelivery>() ?? deliverySystem.transform.GetComponent<OffSpinDelivery>();
                        curvedEnabled = offSpinDelivery != null && offSpinDelivery.IsCurvedPathEnabled();
                    }

                    if (curvedEnabled)
                    {
                        useKinematicForCurvedPath = true;
                        Debug.Log("🎯 CURVED PATH: Using PathFollower for curved delivery");
                    }
                    else
                    {
                        Debug.LogWarning($"🎯 PATH FOLLOWER: Delivery not found or path disabled for {currentDelivery}");
                    }
                }
            }
            
            // 🎯 USE PATH FOLLOWER FOR SWING CURVED PATH
            // 🎯 DEBUG: Check current delivery type
            if (deliverySystem != null)
            {
                DeliveryType currentType = deliverySystem.GetCurrentDeliveryType();
                Debug.Log($"🎯 CURRENT DELIVERY TYPE: {currentType}");
            }
            
            if (useKinematicForCurvedPath)
            {
                Debug.Log("🎯 Using PathFollower for delivery path (100% ACCURACY guaranteed)");
                
                // Use normal ball speed for all deliveries
                float effectiveSpeed = ballSpeed;
                
                Vector3[] path = null;
                float addedArc = 0f;
                // Generate path based on current delivery type
                if (deliverySystem.GetCurrentDeliveryType() == DeliveryType.Flat)
                {
                    FlatDelivery flatDelivery = deliverySystem.GetComponent<FlatDelivery>() ?? deliverySystem.transform.GetComponent<FlatDelivery>();
                    if (flatDelivery != null)
                    {
                        path = flatDelivery.GetCurvedPathPoints(startPosition, targetPosition, ballSpeed, 30);
                        addedArc = flatDelivery.pathArcHeight;
                        Debug.Log("🎯 FLAT DELIVERY: Straight path generated (100% accurate)");
                    }
                }
                else if (deliverySystem.GetCurrentDeliveryType() == DeliveryType.SeamIn)
                {
                    SeamInDelivery seamInDelivery = deliverySystem.GetComponent<SeamInDelivery>() ?? deliverySystem.transform.GetComponent<SeamInDelivery>();
                    if (seamInDelivery != null)
                    {
                        path = seamInDelivery.GetCurvedPathPoints(startPosition, targetPosition, ballSpeed, 30);
                        addedArc = seamInDelivery.pathArcHeight;
                        Debug.Log("🎯 SEAM IN DELIVERY: Straight path generated (100% accurate)");
                    }
                }
                else if (deliverySystem.GetCurrentDeliveryType() == DeliveryType.SeamOut)
                {
                    SeamOutDelivery seamOutDelivery = deliverySystem.GetComponent<SeamOutDelivery>() ?? deliverySystem.transform.GetComponent<SeamOutDelivery>();
                    if (seamOutDelivery != null)
                    {
                        path = seamOutDelivery.GetCurvedPathPoints(startPosition, targetPosition, ballSpeed, 30);
                        addedArc = seamOutDelivery.pathArcHeight;
                        Debug.Log("🎯 SEAM OUT DELIVERY: Straight path generated (100% accurate)");
                    }
                }
                else if (deliverySystem.GetCurrentDeliveryType() == DeliveryType.Inswing)
                {
                    InswingDelivery inswingDelivery = deliverySystem.GetComponent<InswingDelivery>() ?? deliverySystem.transform.GetComponent<InswingDelivery>();
                    if (inswingDelivery != null)
                    {
                        path = inswingDelivery.GetCurvedPathPoints(startPosition, targetPosition, ballSpeed, 30);
                        addedArc = inswingDelivery.pathArcHeight;
                    }
                }
                else if (deliverySystem.GetCurrentDeliveryType() == DeliveryType.Outswing)
                {
                    OutswingDelivery outswingDelivery = deliverySystem.GetComponent<OutswingDelivery>() ?? deliverySystem.transform.GetComponent<OutswingDelivery>();
                    if (outswingDelivery != null)
                    {
                        path = outswingDelivery.GetCurvedPathPoints(startPosition, targetPosition, ballSpeed, 30);
                        addedArc = outswingDelivery.pathArcHeight;
                    }
                }
                else if (deliverySystem.GetCurrentDeliveryType() == DeliveryType.LegSpin)
                {
                    LegSpinDelivery legSpin = deliverySystem.GetComponent<LegSpinDelivery>() ?? deliverySystem.transform.GetComponent<LegSpinDelivery>();
                    if (legSpin != null)
                    {
                        path = legSpin.GetCurvedPathPoints(startPosition, targetPosition, ballSpeed, 30);
                        addedArc = legSpin.pathArcHeight;
                    }
                }
                else if (deliverySystem.GetCurrentDeliveryType() == DeliveryType.OffSpin)
                {
                    OffSpinDelivery offSpin = deliverySystem.GetComponent<OffSpinDelivery>() ?? deliverySystem.transform.GetComponent<OffSpinDelivery>();
                    if (offSpin != null)
                    {
                        path = offSpin.GetCurvedPathPoints(startPosition, targetPosition, ballSpeed, 30);
                        addedArc = offSpin.pathArcHeight;
                    }
                }

                if (path != null)
                {
                    // 🎯 PATH VERIFICATION: Show path details
                    Debug.Log($"🎯 PATH GENERATED SUCCESSFULLY:");
                    Debug.Log($"   Path Points: {path.Length}");
                    Debug.Log($"   Start Point: {path[0]}");
                    Debug.Log($"   End Point: {path[path.Length - 1]}");
                    Debug.Log($"   Mid Point: {path[path.Length / 2]}");
                    Debug.Log($"   ✅ Path created from current spawn position to current target!");
                    
                    // Ensure physics won't fight the scripted motion
                    Rigidbody rbFollow = currentBallInstance.GetComponent<Rigidbody>();
                    if (rbFollow != null)
                    {
                        rbFollow.linearVelocity = Vector3.zero;
                        rbFollow.angularVelocity = Vector3.zero;
                        rbFollow.isKinematic = true;
                    }

                    // Compute final forward direction of the path (used to resume physics)
                    Vector3 finalDir = (path[path.Length - 1] - path[path.Length - 2]).normalized;

                    // 🎯 OBSTACLE DETECTION: Check if delivery wants to disable obstacles
                    // Note: PathFollower now intelligently ignores ground/plane objects automatically
                    bool shouldDisableObstacles = false;
                    DeliveryType currentDel = deliverySystem.GetCurrentDeliveryType();
                    if (currentDel == DeliveryType.Flat)
                    {
                        FlatDelivery flatDel = deliverySystem.GetComponent<FlatDelivery>();
                        shouldDisableObstacles = flatDel != null && flatDel.disableObstacleDetection;
                    }
                    else if (currentDel == DeliveryType.SeamIn)
                    {
                        SeamInDelivery seamInDel = deliverySystem.GetComponent<SeamInDelivery>();
                        shouldDisableObstacles = seamInDel != null && seamInDel.disableObstacleDetection;
                    }
                    else if (currentDel == DeliveryType.SeamOut)
                    {
                        SeamOutDelivery seamOutDel = deliverySystem.GetComponent<SeamOutDelivery>();
                        shouldDisableObstacles = seamOutDel != null && seamOutDel.disableObstacleDetection;
                    }
                    else if (currentDel == DeliveryType.LegSpin)
                    {
                        LegSpinDelivery legSpinDel = deliverySystem.GetComponent<LegSpinDelivery>();
                        shouldDisableObstacles = legSpinDel != null && !legSpinDel.enableCurvedPath; // Disable for straight leg spin only if requested
                    }
                    else if (currentDel == DeliveryType.OffSpin)
                    {
                        OffSpinDelivery offSpinDel = deliverySystem.GetComponent<OffSpinDelivery>();
                        shouldDisableObstacles = offSpinDel != null && !offSpinDel.enableCurvedPath; // Disable for straight off spin only if requested
                    }
                    
                    if (shouldDisableObstacles)
                    {
                        Debug.Log($"🎯 OBSTACLE DETECTION: Will be DISABLED for {currentDel} delivery");
                    }
                    else
                    {
                        Debug.Log($"🎯 OBSTACLE DETECTION: ENABLED for {currentDel} delivery (ignores ground/plane automatically)");
                    }

                    var follower = currentBallInstance.AddComponent<PathFollower>();
                    follower.Initialize(path, effectiveSpeed, addedArc, () =>
                    {
                        hasLanded = true;
                        Debug.Log("🎯 PathFollower complete");
                        // Re-enable physics so the ball can bounce and roll towards wicket
                        if (rbFollow != null)
                        {
                            rbFollow.isKinematic = false;
                            rbFollow.useGravity = true;
                            // Resume motion along the last path direction
                            float resumeSpeed = Mathf.Max(10f, ballSpeed * 1.0f);
                            // Ensure a clear downward component so CricketBallBounce detects the bounce
                            float downwardSpeed = Mathf.Max(4.5f, resumeSpeed * 0.35f);
                            Vector3 resumeVelocity = finalDir * resumeSpeed + Vector3.down * downwardSpeed;
                            // Nudge position slightly above target so it falls onto pitch
                            currentBallInstance.transform.position = targetPosition + Vector3.up * 0.03f;
                            rbFollow.linearVelocity = resumeVelocity;
                            // Explicitly trigger first bounce using controller bounce logic for reliability
                            OnBallBounce(currentBallInstance.transform.position, resumeVelocity);
                        }
                    }, shouldDisableObstacles); // 🎯 PASS disable obstacles flag!
                    
                    // 🎯 DEBUG: Verify PathFollower initialization
                    Debug.Log($"🎯 PATHFOLLOWER INIT: Speed={effectiveSpeed}, Arc={addedArc}, PathLength={path.Length}");
                    Debug.Log($"🎯 PATHFOLLOWER SETTINGS: ObstacleDetection={follower.IsObstacleDetectionEnabled}, Radius={follower.ObstacleCheckRadius}");
                    
                    follower.Begin();
                }
                else
                {
                    // Fallback to simple kinematic move
                    StartCoroutine(MoveBallKinematic(startPosition, targetPosition, timeToReach));
                }
            }
            else if (!ballSettingsSO.UseRealisticPhysics)
            {
                Debug.Log("🎯 Using kinematic movement");
                StartCoroutine(MoveBallKinematic(startPosition, targetPosition, timeToReach));
            }
            else
            {
            // 🎯 USE PHYSICS FOR STRAIGHT DELIVERIES
                if (useSmoothMovement)
                {
                    Debug.Log("🎯 Using smooth velocity application");
                    // Apply velocity smoothly over time
                    StartCoroutine(ApplySmoothVelocity(ballRigidbodyToUse, initialVelocity));
                }
                else
                {
                    Debug.Log("🎯 Using direct velocity application");
                ballRigidbodyToUse.linearVelocity = initialVelocity;
                }
            }
            
            Debug.Log($"?? Ball launched with velocity: {initialVelocity.magnitude:F1} m/s");
            Debug.Log($"?? Expected time to target: {timeToReach:F2} seconds");
            Debug.Log($"<color=#00FF00>✅ FINAL BALL SETTINGS: Speed={ballSpeed}, Arc={arcHeight}, Gravity={gravity}</color>");
            Debug.Log($"<color=#FFD700>🎯 Using ball settings: Speed={ballSpeed}, Arc={arcHeight}, Bounce={ballSettingsSO.BounceForce}, Gravity={gravity}</color>");
            
            // Wait for ball to reach target area
            float waitTime = Mathf.Clamp(timeToReach, 0.1f, 5f);
            yield return new WaitForSeconds(waitTime);
            
            // Mark as landed (time-based fallback). Exact contact handled by TargetHitTrigger.
            hasLanded = true;
            Debug.Log("?? Ball reached target window (time-based)");

            // Also trigger speed boost here as a fallback (exact event may also trigger it)
            TryApplySpeedBoostOnTargetHit(ballSpeed);
        }

        // --- Delivery System ---
        /// <summary>
        /// Calculate delivery-modified trajectory target position
        /// </summary>
        private Vector3 CalculateDeliveryTrajectory(Vector3 startPos, Vector3 targetPos, float ballSpeed)
        {
            if (deliverySystem == null)
            {
                Debug.LogWarning("🎯 DELIVERY: No delivery system assigned, using flat delivery");
                return targetPos;
            }
            
            Vector3 deliveryTarget = deliverySystem.CalculateTrajectory(startPos, targetPos, ballSpeed);
            
            Debug.Log($"🎯 DELIVERY: {deliverySystem.GetCurrentDeliveryType()} trajectory calculated at speed {ballSpeed:F1} m/s");
            
            return deliveryTarget;
        }
        
        /// <summary>
        /// Reset delivery system for new ball
        /// </summary>
        private void ResetDeliverySystem()
        {
            if (deliverySystem != null)
            {
                deliverySystem.ResetDelivery();
                Debug.Log("🎯 DELIVERY: Reset delivery system for new ball");
            }
        }
        
        /// <summary>
        /// Switch to flat delivery
        /// </summary>
        public void SwitchToFlatDelivery()
        {
            if (deliverySystem != null)
            {
                deliverySystem.SetDeliveryType(DeliveryType.Flat);
                Debug.Log("🎯 DELIVERY: Switched to Flat delivery");
            }
        }
        
        
        /// <summary>
        /// Switch to inswing delivery
        /// </summary>
        public void SwitchToInSwingDelivery()
        {
            if (deliverySystem != null)
            {
                deliverySystem.SetDeliveryType(DeliveryType.Inswing);
                Debug.Log("🎯 DELIVERY: Switched to Inswing delivery");
            }
        }
        
        /// <summary>
        /// Switch to seam out delivery
        /// </summary>
        public void SwitchToOutSwingDelivery()
        {
            if (deliverySystem != null)
            {
                deliverySystem.SetDeliveryType(DeliveryType.SeamOut);
                Debug.Log("🎯 DELIVERY: Switched to Seam Out delivery");
            }
        }

        /// <summary>
        /// Public setter to change delivery type externally (e.g., from BowlerProfile hotkeys)
        /// </summary>
        public void SetDeliveryType(DeliveryType type)
        {
            if (deliverySystem != null)
            {
                deliverySystem.SetDeliveryType(type);
                Debug.Log($"🎯 DELIVERY: Set via external request to {type}");
            }
        }

        /// <summary>
        /// Switch the current bowler's spawn position (TAB key)
        /// </summary>
        public void SwitchCurrentBowlerSpawnPosition()
        {
            if (currentBowlerInstance == null)
            {
                Debug.LogWarning("🎯 No current bowler instance to switch spawn position");
                return;
            }

            // Find the mapping for the current bowler
            BowlerSpawnMapping mapping = GetSpawnMappingForBowler(currentBowlerInstance);
            if (mapping == null)
            {
                Debug.LogWarning($"🎯 No spawn mapping found for current bowler: {currentBowlerInstance.name}");
                return;
            }

            // Switch the spawn position
            mapping.useSpawn01 = !mapping.useSpawn01;
            Transform newSpawnPoint = mapping.useSpawn01 ? mapping.spawn01 : mapping.spawn02;

            if (newSpawnPoint != null)
            {
                Debug.Log($"🎯 Switching spawn point for {currentBowlerInstance.name} to {(mapping.useSpawn01 ? "Spawn01" : "Spawn02")} at {newSpawnPoint.position}");
                // Disable any movement systems that might interfere
                Rigidbody rb = currentBowlerInstance.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.isKinematic = true; // Temporarily disable physics
                }

                // Stop any ongoing movement that might interfere
                PlayerAnimationController playerController = currentBowlerInstance.GetComponent<PlayerAnimationController>();
                if (playerController != null)
                {
                    playerController.StopAllMovement();
                }

                // Disable root motion temporarily to prevent animation interference
                Animator animator = currentBowlerInstance.GetComponent<Animator>();
                bool wasRootMotionEnabled = false;
                if (animator != null)
                {
                    wasRootMotionEnabled = animator.applyRootMotion;
                    animator.applyRootMotion = false; // Disable root motion
                }

                // Temporarily disable the GameObject to prevent any interference
                currentBowlerInstance.SetActive(false);

                // Update the bowler's position INSTANTLY
                currentBowlerInstance.transform.position = newSpawnPoint.position;
                currentBowlerInstance.transform.rotation = newSpawnPoint.rotation;

                // Force immediate position update
                currentBowlerInstance.transform.SetPositionAndRotation(newSpawnPoint.position, newSpawnPoint.rotation);

                // Re-enable the GameObject immediately
                currentBowlerInstance.SetActive(true);

                // Re-enable systems after position change (with small delay)
                StartCoroutine(ReEnableSystemsAfterDelay(rb, animator, wasRootMotionEnabled));

                // Update the spawn point references
                spawnPoint = newSpawnPoint;
                ballSpawnPoint = newSpawnPoint;
                CacheInstanceSnapshot(currentBowlerInstance, newSpawnPoint.position, newSpawnPoint.rotation, newSpawnPoint);

                // Set delivery system to bowler's default delivery when switching spawn positions
                BowlerProfile profile = currentBowlerInstance.GetComponent<BowlerProfile>();
                if (profile != null && deliverySystem != null)
                {
                    DeliveryType defaultDelivery = profile.GetDefaultDeliveryType();
                    deliverySystem.SetDeliveryType(defaultDelivery);
                    Debug.Log($"🎯 DELIVERY: Reset to bowler default: {defaultDelivery} when switching spawn position");
                }

                // Update AnimationTester's original position to match new spawn position
                AnimationTester animationTester = currentBowlerInstance.GetComponent<AnimationTester>();
                if (animationTester != null)
                {
                    animationTester.UpdateOriginalPosition();
                    Debug.Log($"🎬 AnimationTester: Updated original position to new spawn position");
                }

                Debug.Log($"🏏 Switched {currentBowlerInstance.name} to {(mapping.useSpawn01 ? "Spawn01" : "Spawn02")} at {newSpawnPoint.name}");
            }
            else
            {
                Debug.LogError($"🎯 Spawn position {(mapping.useSpawn01 ? "Spawn01" : "Spawn02")} is null for {currentBowlerInstance.name}");
            }
        }

        /// <summary>
        /// Get the spawn mapping for a specific bowler instance
        /// </summary>
        private BowlerSpawnMapping GetSpawnMappingForBowler(GameObject bowlerInstance)
        {
            if (bowlerInstance == null) return null;

            // Find the original prefab name (remove "(Clone)" suffix)
            string prefabName = bowlerInstance.name.Replace("(Clone)", "");
            
            // Find the mapping that matches this prefab
            foreach (var mapping in bowlerSpawnMappings)
            {
                if (mapping != null && mapping.bowlerPrefab != null && 
                    mapping.bowlerPrefab.name == prefabName)
                {
                    return mapping;
                }
            }

            return null;
        }

        /// <summary>
        /// Get the spawn mapping for a specific bowler prefab
        /// </summary>
        private BowlerSpawnMapping GetSpawnMappingForPrefab(GameObject bowlerPrefab)
        {
            if (bowlerPrefab == null) return null;
            
            // Find the mapping that matches this prefab
            foreach (var mapping in bowlerSpawnMappings)
            {
                if (mapping != null && mapping.bowlerPrefab == bowlerPrefab)
                {
                    return mapping;
                }
            }

            return null;
        }

        /// <summary>
        /// Get the current spawn position for the current bowler
        /// </summary>
        public Transform GetCurrentBowlerSpawnPosition()
        {
            if (currentBowlerInstance == null) return null;

            BowlerSpawnMapping mapping = GetSpawnMappingForBowler(currentBowlerInstance);
            if (mapping != null)
            {
                Transform mappingTransform = mapping.useSpawn01 ? mapping.spawn01 : mapping.spawn02;
                if (mappingTransform != null)
                {
                    return mappingTransform;
                }
            }

            Transform snapshotTransform = GetInstanceSpawnTransform(currentBowlerInstance);
            if (snapshotTransform != null)
            {
                return snapshotTransform;
            }

            if (spawnPoint != null)
            {
                return spawnPoint;
            }

            if (ballSpawnPoint != null)
            {
                return ballSpawnPoint;
            }

            return null;
        }

        /// <summary>
        /// Get the current bowler instance
        /// </summary>
        public GameObject GetCurrentBowlerInstance()
        {
            return currentBowlerInstance;
        }

        /// <summary>
        /// Reset bowler to spawn position without destroying/recreating
        /// </summary>
        public void ResetBowlerToSpawn()
        {
            if (currentBowlerInstance == null)
            {
                Debug.LogWarning("🎯 No current bowler instance to reset");
                return;
            }

            Transform spawnPos = GetCurrentBowlerSpawnPosition();
            if (spawnPos == null)
            {
                Debug.LogWarning("🎯 No spawn position found for current bowler");
                return;
            }

            // Disable physics temporarily
            Rigidbody rb = currentBowlerInstance.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            // Stop any movement
            PlayerAnimationController playerController = currentBowlerInstance.GetComponent<PlayerAnimationController>();
            if (playerController != null)
            {
                playerController.StopAllMovement();
            }

            // Disable root motion temporarily
            Animator animator = currentBowlerInstance.GetComponent<Animator>();
            bool wasRootMotionEnabled = false;
            if (animator != null)
            {
                wasRootMotionEnabled = animator.applyRootMotion;
                animator.applyRootMotion = false;
            }

            // Temporarily disable GameObject
            currentBowlerInstance.SetActive(false);

            // Reset position and rotation
            currentBowlerInstance.transform.SetPositionAndRotation(spawnPos.position, spawnPos.rotation);

            // Set Y rotation to 0 (or use PlayerAnimationController's method)
            if (playerController != null)
            {
                playerController.SetTargetYRotation(0f);
            }

            // Re-enable GameObject
            currentBowlerInstance.SetActive(true);

            // CRITICAL: Update spawn point references to match the reset position
            // This ensures ball trajectory is calculated from the correct spawn point
            spawnPoint = spawnPos;
            ballSpawnPoint = spawnPos;
            
            // CRITICAL: Update PlayerAnimationController's spawn point reference if it exists
            if (playerController != null)
            {
                // Refresh the spawn point reference in PlayerAnimationController
                playerController.RefreshSpawnPointPosition();
            }

            // Re-enable systems
            StartCoroutine(ReEnableSystemsAfterDelay(rb, animator, wasRootMotionEnabled));

            Debug.Log($"🏏 Reset {currentBowlerInstance.name} to spawn position: {spawnPos.name} at {spawnPos.position}");
            Debug.Log($"🏏 Updated spawnPoint and ballSpawnPoint references to: {spawnPoint.name}");
        }

        /// <summary>
        /// Re-enable systems after position change to ensure instant teleportation
        /// </summary>
        private System.Collections.IEnumerator ReEnableSystemsAfterDelay(Rigidbody rb, Animator animator, bool wasRootMotionEnabled)
        {
            // Wait for one frame to ensure position change is processed
            yield return null;

            // Re-enable physics
            if (rb != null)
            {
                rb.isKinematic = false;
            }

            // Re-enable root motion
            if (animator != null)
            {
                animator.applyRootMotion = wasRootMotionEnabled;
            }

            Debug.Log($"🏏 Systems re-enabled after instant position change");
        }

        /// <summary>
        /// Test method to switch to LegSpin delivery
        /// </summary>
        [ContextMenu("Test LegSpin Delivery")]
        public void TestLegSpinDelivery()
        {
            if (deliverySystem != null)
            {
                deliverySystem.SwitchToLegSpinDelivery();
                Debug.Log("🎯 TEST: Switched to LegSpin delivery");
                
                LegSpinDelivery legSpin = deliverySystem.GetComponent<LegSpinDelivery>();
                if (legSpin != null)
                {
                    Debug.Log($"🎯 TEST: LegSpin component found");
                    Debug.Log($"🎯 TEST: Post-bounce spin enabled: {legSpin.enablePostBounceSpinEffect}");
                    Debug.Log($"🎯 TEST: Post-bounce spin strength: {legSpin.postBounceSpinStrength:F2}");
                }
                else
                {
                    Debug.LogError("🎯 TEST: LegSpin component NOT found!");
                }
            }
            else
            {
                Debug.LogError("🎯 TEST: DeliverySystem not found!");
            }
        }

        // --- Target hit integration ---
        public void OnTargetTouched(Rigidbody ballBody)
        {
            if (ballBody == null) return;
            // Apply post-target effects exactly on contact
            float currentSpeed = speedController != null ? speedController.GetCurrentSpeed() : (ballSettingsSO != null ? ballSettingsSO.GlobalBallSpeed : 12f);
            TryApplySpeedBoostOnTargetHit(currentSpeed);
        }

        private void TryApplySpeedBoostOnTargetHit(float ballSpeed)
        {
            BallSpeedBoost targetSpeedBoost = currentBallInstance?.GetComponent<BallSpeedBoost>();
            if (targetSpeedBoost != null)
            {
                targetSpeedBoost.OnTargetHit();
                DeliveryType currentDelivery = deliverySystem?.GetCurrentDeliveryType() ?? DeliveryType.Flat;
                Debug.Log($"🎯 SPEED BOOST: Triggered for {currentDelivery} delivery with initial speed {ballSpeed:F1} m/s");
            }
        }
        
        
        // --- Analytic ballistic solver ---
        // Returns initial velocity that hits target with given speed and gravity. If infeasible, returns Vector3.zero
        private Vector3 SolveBallisticVelocity(Vector3 start, Vector3 end, float speed, float gravity, bool highArc)
        {
            Vector3 delta = end - start;
            Vector3 deltaXZ = new Vector3(delta.x, 0f, delta.z);
            float dx = deltaXZ.magnitude;
            float dy = delta.y;
            float v = Mathf.Max(0.1f, speed);
            float g = Mathf.Max(0.01f, gravity);

            float v2 = v * v;
            float underSqrt = v2 * v2 - g * (g * dx * dx + 2f * dy * v2);
            if (underSqrt < 0f)
            {
                return Vector3.zero; // No solution with this speed
            }

            float sqrt = Mathf.Sqrt(underSqrt);
            // Two possible angles: low arc and high arc
            float tanThetaLow = (v2 - sqrt) / (g * dx);
            float tanThetaHigh = (v2 + sqrt) / (g * dx);
            float tanTheta = highArc ? tanThetaHigh : tanThetaLow;
            float cosTheta = 1f / Mathf.Sqrt(1f + tanTheta * tanTheta);
            float sinTheta = tanTheta * cosTheta;

            Vector3 dirXZ = dx > 0.0001f ? deltaXZ.normalized : Vector3.forward;
            Vector3 v0 = dirXZ * (v * cosTheta);
            v0.y = v * sinTheta;
            return v0;
        }
        
        /// <summary>
        /// Apply velocity smoothly over time for realistic ball movement
        /// </summary>
        IEnumerator ApplySmoothVelocity(Rigidbody rb, Vector3 targetVelocity)
        {
            Vector3 startVelocity = rb.linearVelocity;
            float duration = 0.1f; // Short duration for smooth transition
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                t = Mathf.SmoothStep(0f, 1f, t); // Smooth interpolation
                
                rb.linearVelocity = Vector3.Lerp(startVelocity, targetVelocity, t);
                yield return null;
            }
            
            rb.linearVelocity = targetVelocity;
        }
        
        /// <summary>
        /// Move ball using kinematic movement with realistic cricket arc and rotation
        /// </summary>
        IEnumerator MoveBallKinematic(Vector3 startPos, Vector3 endPos, float duration)
        {
            float elapsed = 0f;
            
            // ?? NEW: Get ball settings for kinematic movement
            BallSettings ballSettings = currentBallInstance?.GetComponent<BallSettings>();
            float arcHeight = ballSettings != null ? ballSettingsSO.ArcHeight : 1f;
            float gravity = ballSettings != null ? ballSettingsSO.Gravity : 9.81f;
            
            // ?? FIXED: Use realistic cricket bowling arc (much lower)
            float realisticArcHeight = arcHeight * 0.2f; // Same reduction as physics version
            
            // 🎯 SIMPLIFIED KINEMATIC: Use simpler duration calculation
            Vector3 horizontalStart = new Vector3(startPos.x, 0, startPos.z);
            Vector3 horizontalEnd = new Vector3(endPos.x, 0, endPos.z);
            float horizontalDistance = Vector3.Distance(horizontalStart, horizontalEnd);
            
            // Use ball speed for kinematic movement (simpler and more predictable)
            float ballSpeed = ballSettingsSO != null ? ballSettingsSO.GlobalBallSpeed : 12f;
            float baseDuration = horizontalDistance / ballSpeed;
            
            // Apply small correction for arc height
            float arcTimeFactor = 1f + (realisticArcHeight * 0.1f);
            duration = baseDuration * arcTimeFactor;
            
            // 🎯 CRITICAL: Get current spawn point for trajectory direction
            // Always use the current spawn point, not a stale reference
            Transform currentSpawnForTrajectory = GetCurrentBowlerSpawnPosition();
            if (currentSpawnForTrajectory == null)
            {
                currentSpawnForTrajectory = spawnPoint; // Fallback to cached reference
            }
            
            // 🎯 SIMPLIFIED: Use spawn point's forward direction directly (includes both X and Y rotation)
            Vector3 trajectoryDirection;
            if (currentSpawnForTrajectory != null)
            {
                // Use spawn point's forward direction which already includes both X and Y rotation
                trajectoryDirection = currentSpawnForTrajectory.forward;
                // Remove Y component to keep it horizontal
                trajectoryDirection.y = 0f;
                trajectoryDirection = trajectoryDirection.normalized;
                
                Debug.Log($"🎯 KINEMATIC ROTATION: Applied spawn rotation {currentSpawnForTrajectory.rotation.eulerAngles} to trajectory direction {trajectoryDirection}");
            }
            else
            {
                // Fallback to direct target direction
                trajectoryDirection = (endPos - startPos).normalized;
            }
            
            // 🎯 CRITICAL FIX: Apply X rotation effect to arc height for kinematic movement
            float rotationXRadians = currentSpawnForTrajectory != null ? currentSpawnForTrajectory.rotation.eulerAngles.x * Mathf.Deg2Rad : 0f;
            float rotationEffect = Mathf.Sin(rotationXRadians);
            float adjustedArcHeight = realisticArcHeight + (rotationEffect * 2f); // Adjust arc based on rotation
            
            // 🎯 CURVED PATH: Check if current delivery supports curved path
            bool useCurvedPath = false;
            
            if (deliverySystem != null)
            {
                DeliveryType currentDelivery = deliverySystem.GetCurrentDeliveryType();
                if (currentDelivery == DeliveryType.Inswing)
                {
                    InswingDelivery inswingDelivery = deliverySystem.GetComponent<InswingDelivery>();
                    if (inswingDelivery != null && inswingDelivery.IsCurvedPathEnabled())
                    {
                        useCurvedPath = true;
                        Debug.Log($"🎯 CURVED PATH: Using curved path for Inswing delivery");
                    }
                }
                else if (currentDelivery == DeliveryType.LegSpin)
                {
                    LegSpinDelivery legSpinDelivery = deliverySystem.GetComponent<LegSpinDelivery>();
                    if (legSpinDelivery != null && legSpinDelivery.IsCurvedPathEnabled())
                    {
                        useCurvedPath = true;
                        Debug.Log($"🎯 CURVED PATH: Using curved path for LegSpin delivery");
                    }
                }
            }
            
            // 🎯 OBSTACLE DETECTION: Track previous position for collision detection
            Vector3 previousPos = startPos;
            float obstacleCheckRadius = 0.1f; // Ball radius for obstacle detection
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                Vector3 currentPos;
                
                if (useCurvedPath)
                {
                    // 🎯 CURVED PATH: Follow Bezier curve
                    InswingDelivery inswingDelivery = deliverySystem.GetComponent<InswingDelivery>();
                    if (inswingDelivery == null)
                    {
                        // Try to find it on the same GameObject as DeliverySystem
                        inswingDelivery = deliverySystem.transform.GetComponent<InswingDelivery>();
                    }
                    
                    if (inswingDelivery != null)
                    {
                        currentPos = inswingDelivery.GetCurvedPathPoint(startPos, endPos, ballSpeed, t);
                        
                        // Apply arc height to curved path
                        float arcCurve = Mathf.Sin(t * Mathf.PI) * adjustedArcHeight;
                        currentPos.y += arcCurve;
                        
                        // Apply downward angle for curved path
                        if (spawnPoint != null)
                        {
                            float kinematicRotationXRadians = spawnPoint.rotation.eulerAngles.x * Mathf.Deg2Rad;
                            float downwardAngle = Mathf.Sin(kinematicRotationXRadians) * t * 0.5f;
                            currentPos.y -= downwardAngle;
                        }
                    }
                    else
                    {
                        // Fallback to straight path
                        currentPos = startPos + trajectoryDirection * Vector3.Distance(startPos, endPos) * t;
                        float arcCurve = Mathf.Sin(t * Mathf.PI) * adjustedArcHeight;
                        currentPos.y += arcCurve;
                    }
                }
                else
                {
                    // 🎯 STRAIGHT PATH: Use rotated trajectory direction
                    currentPos = startPos + trajectoryDirection * Vector3.Distance(startPos, endPos) * t;
                
                // Use a more realistic arc curve for cricket bowling with X rotation effect
                // Cricket balls follow a gentle, low arc, not a high parabola
                float arcCurve = Mathf.Sin(t * Mathf.PI) * adjustedArcHeight;
                currentPos.y += arcCurve;
                
                // 🎯 CRITICAL FIX: Apply additional downward angle based on X rotation
                if (spawnPoint != null)
                {
                    float kinematicRotationXRadians = spawnPoint.rotation.eulerAngles.x * Mathf.Deg2Rad;
                    float downwardAngle = Mathf.Sin(kinematicRotationXRadians) * t * 0.5f; // Progressive downward angle
                    currentPos.y -= downwardAngle;
                    }
                }
                
                // 🎯 OBSTACLE DETECTION: Check for obstacles between previous and current position
                Vector3 movementDirection = (currentPos - previousPos).normalized;
                float movementDistance = Vector3.Distance(previousPos, currentPos);
                
                if (movementDistance > 0.001f) // Only check if there's actual movement
                {
                    // Cast a sphere along the movement path to detect obstacles
                    RaycastHit[] hits = Physics.SphereCastAll(previousPos, obstacleCheckRadius, movementDirection, movementDistance);
                    
                    foreach (RaycastHit hit in hits)
                    {
                        // Skip self-collision and target collision (target is handled by TargetHitTrigger)
                        if (hit.collider.gameObject == ball || hit.collider.gameObject == target.gameObject)
                            continue;
                            
                        // Check if this is an obstacle (has Rigidbody or specific tag)
                        if (hit.collider.attachedRigidbody != null || hit.collider.CompareTag("Obstacle"))
                        {
                            Debug.Log($"🎯 OBSTACLE HIT: Ball hit obstacle {hit.collider.name} during kinematic movement");
                            
                            // Apply physics response to the obstacle
                            ApplyObstaclePhysicsResponse(hit, movementDirection, ballSpeed);
                            
                            // Adjust ball position to collision point
                            currentPos = hit.point + hit.normal * obstacleCheckRadius;
                            
                            // Apply bounce/deflection based on obstacle properties
                            Vector3 reflectedDirection = Vector3.Reflect(movementDirection, hit.normal);
                            float bounceForce = 0.7f; // Configurable bounce strength
                            
                            // Update trajectory direction for remaining movement
                            trajectoryDirection = reflectedDirection;
                            trajectoryDirection.y = 0f; // Keep horizontal
                            trajectoryDirection = trajectoryDirection.normalized;
                            
                            Debug.Log($"🎯 OBSTACLE DEFLECTION: New direction {trajectoryDirection}");
                            break; // Only handle first obstacle hit
                        }
                    }
                }
                
                ball.transform.position = currentPos;
                previousPos = currentPos;
                
                yield return null;
            }
            
            // Ensure ball is exactly at target
            ball.transform.position = endPos;
        }
        
        /// <summary>
        /// Apply physics response to obstacles hit during kinematic movement
        /// </summary>
        private void ApplyObstaclePhysicsResponse(RaycastHit hit, Vector3 ballDirection, float ballSpeed)
        {
            Rigidbody obstacleRb = hit.collider.attachedRigidbody;
            if (obstacleRb != null)
            {
                // Apply force to the obstacle
                Vector3 forceDirection = hit.normal;
                float forceMagnitude = ballSpeed * 0.5f; // Configurable force multiplier
                Vector3 force = forceDirection * forceMagnitude;
                
                obstacleRb.AddForceAtPosition(force, hit.point, ForceMode.Impulse);
                
                Debug.Log($"🎯 OBSTACLE FORCE: Applied {force.magnitude:F1}N force to {hit.collider.name}");
            }
            
            // Optional: Add visual/audio effects here
            // Example: Particle effects, sound effects, etc.
        }
        
        /// <summary>
        /// ?? WORKING: Wait for ball to land on target and finish bouncing
        /// </summary>
        IEnumerator WaitForLanding()
        {
            Debug.Log("?? Waiting for ball to finish bouncing and settle...");
            Debug.Log($"?? Initial state: isBouncing={isBouncing}, currentBounces={currentBounces}");
            
            // Wait for initial landing
            yield return new WaitForSeconds(0.3f);
            Debug.Log($"?? After 0.3s wait: isBouncing={isBouncing}, currentBounces={currentBounces}");
            
            // Wait for bounces to complete
            int waitCount = 0;
            while (isBouncing && currentBounces < 3) // Default max bounces
            {
                yield return new WaitForSeconds(0.1f);
                waitCount++;
                Debug.Log($"?? Waiting for bounce completion: isBouncing={isBouncing}, currentBounces={currentBounces}, waitCount={waitCount}");
                
                // Safety break to prevent infinite loop
                if (waitCount > 50) // 5 seconds max
                {
                    Debug.Log("?? ⚠️ WAIT FOR LANDING TIMEOUT - forcing completion");
                    break;
                }
            }
            
            // Additional wait to ensure ball has settled
            yield return new WaitForSeconds(0.5f);
            
            Debug.Log($"?? Ball has settled after {currentBounces} bounces");
            
            // ?? FORCE: Stop bouncing if still bouncing
            if (isBouncing)
            {
                isBouncing = false;
                Debug.Log("?? Force stopped bouncing - ball ready to return");
            }
            
            // ?? CRITICAL: Ensure ball is ready for return
            if (ballRigidbody != null)
            {
                // Force stop any remaining movement
                ballRigidbody.linearVelocity = Vector3.zero;
                ballRigidbody.angularVelocity = Vector3.zero;
                Debug.Log("?? Ball movement stopped - ready for return");
            }
        }
        
        /// <summary>
        /// Handle ball bounce event from CricketBallBounce component
        /// </summary>
        public void OnBallBounce(Vector3 bouncePosition, Vector3 bounceVelocity)
        {
            currentBounces++;
            isBouncing = true;
            lastBouncePosition = bouncePosition;
            
            Debug.Log($"?? Ball bounced! Bounce #{currentBounces} at {bouncePosition}");
            Debug.Log($"?? Bounce velocity: {bounceVelocity.magnitude:F1} m/s");
            
            // ?? NEW: Get bounce settings from BallSettings component
            GameObject ballToBounce = currentBallInstance != null ? currentBallInstance : ball;
            BallSettings ballSettings = ballToBounce?.GetComponent<BallSettings>();
            
            if (ballSettings != null && currentBounces <= ballSettingsSO.MaxBounces)
            {
                // Calculate bounce velocity with friction
                Vector3 newVelocity = bounceVelocity * ballSettingsSO.BounceFriction;
                
                // Bounce force tuning
                // Identify bouncer lengths (short/mid) and reduce bounce to avoid too-high hop
                bool isBouncerLength = currentLength01 <= 0.35f; // shorter distance → bouncer
                float enhancedBounceForce;
                if (isBouncerLength)
                {
                    // Reduce bounce to ~60% of base for bouncers (user request)
                    enhancedBounceForce = ballSettingsSO.BounceForce * 0.6f;
                }
                else
                {
                    // Gentle scaling for other lengths; avoid exceeding base by much
                    enhancedBounceForce = ballSettingsSO.BounceForce;
                    if (currentBounces == 1)
                        enhancedBounceForce *= 0.9f; // reduced first-bounce lift
                    else if (currentBounces == 2)
                        enhancedBounceForce *= 0.8f;
                    else
                        enhancedBounceForce *= 0.7f;

                    // Length-based scaling with max 1.0 to avoid extra boost
                    float lengthBounceScale = Mathf.Lerp(1.0f, 0.65f, currentLength01);
                    lengthBounceScale = Mathf.Clamp(lengthBounceScale, 0.65f, 1.0f);
                enhancedBounceForce *= lengthBounceScale;
                }
                
                // Apply enhanced bounce force to Y velocity (bounce upward)
                newVelocity.y = Mathf.Abs(bounceVelocity.y) * enhancedBounceForce;
                
                // ?? ADDITIONAL: Preserve some horizontal momentum for realistic cricket bounce
                newVelocity.x *= 0.9f; // Keep 90% of horizontal velocity
                newVelocity.z *= 0.9f; // Keep 90% of horizontal velocity
                
                // 🎯 SPIN SWING EFFECT: Add lateral spin AFTER bounce (realistic cricket physics!)
                // 🎯 DYNAMIC LATERAL CALCULATION: Works from ANY spawn point/direction
                if (deliverySystem != null && currentBounces == 1)
                {
                    DeliveryType currentDelivery = deliverySystem.GetCurrentDeliveryType();
                    
                    // Check for Leg Spin
                    if (currentDelivery == DeliveryType.LegSpin)
                    {
                        LegSpinDelivery legSpinDelivery = deliverySystem.GetComponent<LegSpinDelivery>() ?? deliverySystem.transform.GetComponent<LegSpinDelivery>();
                        if (legSpinDelivery != null && legSpinDelivery.enablePostBounceSpinEffect)
                        {
                            // Simple lateral spin calculation based on ball speed and spin strength
                            float ballSpeed = bounceVelocity.magnitude;
                            
                            // 🎯 CRITICAL FIX: Calculate lateral direction based on CURRENT ball direction
                            // This works from ANY spawn point, not just hardcoded X-axis
                            Vector3 forwardDirection = new Vector3(bounceVelocity.x, 0, bounceVelocity.z).normalized; // Ball's horizontal direction
                            Vector3 lateralDirection = Vector3.Cross(Vector3.up, forwardDirection).normalized; // Right direction relative to ball movement
                            
                            // Apply lateral spin: postBounceSpinStrength controls direction and intensity
                            // Positive = spin right (relative to ball direction), Negative = spin left
                            float spinStrength = ballSpeed * legSpinDelivery.postBounceSpinStrength;
                            Vector3 lateralSpinVelocity = lateralDirection * spinStrength;
                            
                            // Add lateral spin to velocity
                            newVelocity += lateralSpinVelocity;
                            
                            string spinDirection = spinStrength > 0 ? "RIGHT →" : spinStrength < 0 ? "← LEFT" : "NONE";
                            Debug.Log($"🎯 LEG SPIN BOUNCE SWING: Applied lateral spin {spinStrength:F2} m/s (strength: {legSpinDelivery.postBounceSpinStrength:F2})");
                            Debug.Log($"🎯 LEG SPIN: Ball speed {ballSpeed:F1} → Lateral movement {spinStrength:F2} {spinDirection}");
                            Debug.Log($"🎯 LEG SPIN: Forward: {forwardDirection}, Lateral: {lateralDirection}");
                        }
                    }
                    // Check for Off Spin
                    else if (currentDelivery == DeliveryType.OffSpin)
                    {
                        OffSpinDelivery offSpinDelivery = deliverySystem.GetComponent<OffSpinDelivery>() ?? deliverySystem.transform.GetComponent<OffSpinDelivery>();
                        if (offSpinDelivery != null && offSpinDelivery.enablePostBounceSpinEffect)
                        {
                            // Simple lateral spin calculation based on ball speed and spin strength
                            float ballSpeed = bounceVelocity.magnitude;
                            
                            // 🎯 CRITICAL FIX: Calculate lateral direction based on CURRENT ball direction
                            // This works from ANY spawn point, not just hardcoded X-axis
                            Vector3 forwardDirection = new Vector3(bounceVelocity.x, 0, bounceVelocity.z).normalized; // Ball's horizontal direction
                            Vector3 lateralDirection = Vector3.Cross(Vector3.up, forwardDirection).normalized; // Right direction relative to ball movement
                            
                            // Apply lateral spin: postBounceSpinStrength controls direction and intensity
                            // Positive = spin right (relative to ball direction), Negative = spin left
                            float spinStrength = ballSpeed * offSpinDelivery.postBounceSpinStrength;
                            Vector3 lateralSpinVelocity = lateralDirection * spinStrength;
                            
                            // Add lateral spin to velocity
                            newVelocity += lateralSpinVelocity;
                            
                            string spinDirection = spinStrength > 0 ? "RIGHT →" : spinStrength < 0 ? "← LEFT" : "NONE";
                            Debug.Log($"🎯 OFF SPIN BOUNCE SWING: Applied lateral spin {spinStrength:F2} m/s (strength: {offSpinDelivery.postBounceSpinStrength:F2})");
                            Debug.Log($"🎯 OFF SPIN: Ball speed {ballSpeed:F1} → Lateral movement {spinStrength:F2} {spinDirection}");
                            Debug.Log($"🎯 OFF SPIN: Forward: {forwardDirection}, Lateral: {lateralDirection}");
                        }
                    }
                    // Check for Seam In
                    else if (currentDelivery == DeliveryType.SeamIn)
                    {
                        SeamInDelivery seamInDelivery = deliverySystem.GetComponent<SeamInDelivery>() ?? deliverySystem.transform.GetComponent<SeamInDelivery>();
                        if (seamInDelivery != null && seamInDelivery.enablePostBounceSeam)
                        {
                            float ballSpeed = bounceVelocity.magnitude;
                            
                            // Calculate lateral direction based on CURRENT ball direction
                            Vector3 forwardDirection = new Vector3(bounceVelocity.x, 0, bounceVelocity.z).normalized; // Ball's horizontal direction
                            Vector3 lateralDirection = Vector3.Cross(Vector3.up, forwardDirection).normalized; // Right direction relative to ball movement
                            
                            // Apply lateral seam movement: postBounceSeamStrength controls direction and intensity
                            // Positive = continues moving right (relative to ball direction), Negative = reverses
                            float seamStrength = ballSpeed * seamInDelivery.postBounceSeamStrength;
                            Vector3 lateralSeamVelocity = lateralDirection * seamStrength;
                            
                            // Add lateral seam movement to velocity
                            newVelocity += lateralSeamVelocity;
                            
                            string seamDirection = seamStrength > 0 ? "RIGHT →" : seamStrength < 0 ? "← LEFT" : "NONE";
                            Debug.Log($"🎯 SEAM IN BOUNCE: Applied lateral seam {seamStrength:F2} m/s (strength: {seamInDelivery.postBounceSeamStrength:F2})");
                            Debug.Log($"🎯 SEAM IN: Ball speed {ballSpeed:F1} → Lateral movement {seamStrength:F2} {seamDirection}");
                            Debug.Log($"🎯 SEAM IN: Forward: {forwardDirection}, Lateral: {lateralDirection}");
                        }
                    }
                    // Check for Seam Out
                    else if (currentDelivery == DeliveryType.SeamOut)
                    {
                        SeamOutDelivery seamOutDelivery = deliverySystem.GetComponent<SeamOutDelivery>() ?? deliverySystem.transform.GetComponent<SeamOutDelivery>();
                        if (seamOutDelivery != null && seamOutDelivery.enablePostBounceSeam)
                        {
                            float ballSpeed = bounceVelocity.magnitude;
                            
                            // Calculate lateral direction based on CURRENT ball direction
                            Vector3 forwardDirection = new Vector3(bounceVelocity.x, 0, bounceVelocity.z).normalized; // Ball's horizontal direction
                            Vector3 lateralDirection = Vector3.Cross(Vector3.up, forwardDirection).normalized; // Right direction relative to ball movement
                            
                            // Apply lateral seam movement: postBounceSeamStrength controls direction and intensity
                            // Positive = continues moving right, but we want LEFT movement for seam out
                            // So we use NEGATIVE of the strength to move LEFT
                            float seamStrength = ballSpeed * (-seamOutDelivery.postBounceSeamStrength); // Negative for LEFT movement
                            Vector3 lateralSeamVelocity = lateralDirection * seamStrength;
                            
                            // Add lateral seam movement to velocity
                            newVelocity += lateralSeamVelocity;
                            
                            string seamDirection = seamStrength > 0 ? "RIGHT →" : seamStrength < 0 ? "← LEFT" : "NONE";
                            Debug.Log($"🎯 SEAM OUT BOUNCE: Applied lateral seam {seamStrength:F2} m/s (strength: {seamOutDelivery.postBounceSeamStrength:F2})");
                            Debug.Log($"🎯 SEAM OUT: Ball speed {ballSpeed:F1} → Lateral movement {seamStrength:F2} {seamDirection}");
                            Debug.Log($"🎯 SEAM OUT: Forward: {forwardDirection}, Lateral: {lateralDirection}");
                        }
                    }
                }
                
                // Apply the enhanced velocity
                Rigidbody ballRigidbodyToBounce = ballToBounce.GetComponent<Rigidbody>();
                if (ballRigidbodyToBounce != null)
                {
                    ballRigidbodyToBounce.linearVelocity = newVelocity;
                }
                
                Debug.Log($"?? Enhanced bounce physics applied! Bounce #{currentBounces}");
                Debug.Log($"?? Enhanced bounce force (length {currentLength01:F2}): {enhancedBounceForce:F2}");
                Debug.Log($"?? New velocity: {newVelocity.magnitude:F1} m/s");
            }
            
            // Stop bouncing if max bounces reached
            if (ballSettings != null && currentBounces >= ballSettingsSO.MaxBounces)
            {
                isBouncing = false;
                Debug.Log("?? Max bounces reached - ball settling");
            }
        }
        
        /// <summary>
        /// Reset bounce state for new bowl
        /// </summary>
        void ResetBounceState()
        {
            currentBounces = 0;
            isBouncing = false;
            lastBouncePosition = Vector3.zero;
        }
        
        /// <summary>
        /// Get the target transform (for bounce component)
        /// </summary>
        public Transform GetTarget()
        {
            return target;
        }
        
        /// <summary>
        /// ?? AGGRESSIVE FIX: Return ball to original position with forced positioning
        /// </summary>
        IEnumerator ReturnBallToOriginal()
        {
            Debug.Log("?? AGGRESSIVE: Returning ball to original position...");
            
            isReturning = true;
            
            Vector3 currentPos = ball.transform.position;
            // 🎯 DYNAMIC: Use current spawn point position, not cached value
            Vector3 targetPos = spawnPoint.position;
            float distance = Vector3.Distance(currentPos, targetPos);
            float timeToReturn = distance / returnSpeed;
            
            Debug.Log($"?? Distance to return: {distance:F2}m, Time: {timeToReturn:F2}s");
            Debug.Log($"?? From: {currentPos} To: {targetPos}");
            
            // ?? AGGRESSIVE STOP: Completely disable physics
            if (ballRigidbody != null)
            {
                ballRigidbody.isKinematic = true;
                ballRigidbody.linearVelocity = Vector3.zero;
                ballRigidbody.angularVelocity = Vector3.zero;
                ballRigidbody.useGravity = false;
                ballRigidbody.linearDamping = 0f;
                ballRigidbody.angularDamping = 0f;
                ballRigidbody.mass = 0.1f; // Make it very light during return
            }
            
            // ?? IMMEDIATE: Force ball to stop moving
            ball.transform.position = currentPos;
            
            // ?? SMOOTH: Fast return movement with smooth positioning
            float elapsed = 0f;
            while (elapsed < timeToReturn)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / timeToReturn;
                
                // Smooth interpolation for better movement
                t = Mathf.SmoothStep(0f, 1f, t);
                
                Vector3 newPosition = Vector3.Lerp(currentPos, targetPos, t);
                
                // ?? SMOOTH: Set position with smooth movement
                if (useSmoothMovement)
                {
                    ball.transform.position = Vector3.Lerp(ball.transform.position, newPosition, 0.8f);
                }
                else
                {
                ball.transform.position = newPosition;
                }
                
                // Debug position every few frames
                if (Mathf.FloorToInt(elapsed * 10) % 5 == 0)
                {
                    Debug.Log($"?? Return progress: {t:P0} - Position: {newPosition}");
                }
                
                yield return null;
            }
            
            // ?? FORCE FINAL: Ensure exact positioning
            ball.transform.position = targetPos;
            Debug.Log($"?? Final position: {ball.transform.position}");
            
            // ?? RESET: Re-enable physics with clean state
            if (ballRigidbody != null)
            {
                ballRigidbody.isKinematic = false;
                ballRigidbody.useGravity = true;
                ballRigidbody.linearVelocity = Vector3.zero;
                ballRigidbody.angularVelocity = Vector3.zero;
                ballRigidbody.linearDamping = 0.02f;
                ballRigidbody.angularDamping = 0.02f;
                ballRigidbody.mass = 1f; // Restore normal mass
            }
            
            // ?? VERIFY: Double-check position
            if (Vector3.Distance(ball.transform.position, targetPos) > 0.01f)
            {
                Debug.LogWarning("?? Position mismatch! Forcing final position...");
                ball.transform.position = targetPos;
            }
            
            isReturning = false;
            Debug.Log("?? Ball successfully returned to original position!");
        }
        
        /// <summary>
        /// ?? ENHANCED: Reset ball to original position immediately with force
        /// </summary>
        public void ResetBall()
        {
            if (ball != null)
            {
                Debug.Log("?? FORCE RESET: Ball to original position...");
                
                // ?? FORCE: Stop all physics immediately
                if (ballRigidbody != null)
                {
                    ballRigidbody.isKinematic = true;
                    ballRigidbody.linearVelocity = Vector3.zero;
                    ballRigidbody.angularVelocity = Vector3.zero;
                    ballRigidbody.useGravity = false;
                }
                
                // ?? FORCE: Set position
                // 🎯 DYNAMIC: Use current spawn point position, not cached value
                ball.transform.position = spawnPoint.position;
                
                // ?? RESET: Physics state
                if (ballRigidbody != null)
                {
                    ballRigidbody.isKinematic = false;
                    ballRigidbody.useGravity = true;
                    ballRigidbody.linearVelocity = Vector3.zero;
                    ballRigidbody.angularVelocity = Vector3.zero;
                }
                
                ResetBounceState();
                Debug.Log("?? Ball FORCE RESET to original position!");
            }
        }
        
        /// <summary>
        /// Get current test status
        /// </summary>
        public string GetTestStatus()
        {
            return $"Test Running: {isRunning}\n" +
                   $"Ball Returning: {isReturning}\n" +
                   $"Has Landed: {hasLanded}\n" +
                   $"Current Bounces: {currentBounces}\n" +
                   $"Is Bouncing: {isBouncing}\n" +
                   $"Ball Position: {ball?.transform.position}\n" +
                   $"Target Position: {target?.position}";
        }
        
        /// <summary>
        /// ?? NEW: Context menu functions for simple ball management
        /// </summary>
        [ContextMenu("Create New Ball")]
        void CreateNewBallContext()
        {
            InstantiateNewBall();
        }
        
        [ContextMenu("Bowl Current Ball")]
        void BowlCurrentBallContext()
        {
            BowlCurrentBall();
        }
        
        [ContextMenu("Reset Ball")]
        void ResetBallContext()
        {
            ResetBall();
        }
        
        [ContextMenu("Clean Up All Bowlers")]
        void CleanUpAllBowlersContext()
        {
            CleanUpAllBowlers();
        }
        
        [ContextMenu("Deactivate Editor Bowlers")]
        void DeactivateEditorBowlersContext()
        {
            DeactivateEditorBowlers();
        }
        
        /// <summary>
        /// Clean up all existing bowlers in the scene (useful for debugging)
        /// </summary>
        public void CleanUpAllBowlers()
        {
            Debug.Log("🎯 === CLEANING UP ALL BOWLERS ===");
            
            // Find all PlayerAnimationController components in the scene
            PlayerAnimationController[] allBowlers = FindObjectsOfType<PlayerAnimationController>();
            Debug.Log($"🎯 Found {allBowlers.Length} existing bowlers to clean up");
            
            foreach (PlayerAnimationController bowler in allBowlers)
            {
                if (bowler != null && bowler.gameObject != null)
                {
                    Debug.Log($"🎯 🗑️ CLEANING UP bowler: {bowler.gameObject.name}");
                    
                    // Check if this is a scene object (editor-created) or runtime object
                    bool isSceneObject = bowler.gameObject.scene.IsValid();
                    Debug.Log($"🎯   Scene object: {isSceneObject}, Scene: {bowler.gameObject.scene.name}");
                    
                    // CRITICAL FIX: Handle editor-created objects differently at runtime
                    if (Application.isPlaying)
                    {
                        if (isSceneObject)
                        {
                            // Editor-created objects: Set inactive instead of destroying
                            Debug.Log($"🎯   Setting editor-created bowler inactive: {bowler.gameObject.name}");
                            bowler.gameObject.SetActive(false);
                            
                            // Also disable the PlayerAnimationController to prevent interference
                            bowler.enabled = false;
                        }
                        else
                        {
                            // Runtime-created objects: Can be destroyed normally
                            Debug.Log($"🎯   Destroying runtime-created bowler: {bowler.gameObject.name}");
                            Destroy(bowler.gameObject);
                        }
                    }
                    else
                    {
                        // In editor mode: Use DestroyImmediate
                        DestroyImmediate(bowler.gameObject);
                    }
                }
            }
            
            // Clear references
            currentBowlerInstance = null;
            playerAnimationController = null;
            
            Debug.Log("🎯 ✅ ALL BOWLERS CLEANED UP");
        }
        
        /// <summary>
        /// Deactivate only editor-created bowlers (leave runtime bowlers active)
        /// </summary>
        public void DeactivateEditorBowlers()
        {
            Debug.Log("🎯 === DEACTIVATING EDITOR BOWLERS ===");
            
            // Find all PlayerAnimationController components in the scene
            PlayerAnimationController[] allBowlers = FindObjectsOfType<PlayerAnimationController>();
            Debug.Log($"🎯 Found {allBowlers.Length} bowlers in scene");
            
            foreach (PlayerAnimationController bowler in allBowlers)
            {
                if (bowler != null && bowler.gameObject != null)
                {
                    // Check if this is a scene object (editor-created)
                    bool isSceneObject = bowler.gameObject.scene.IsValid();
                    Debug.Log($"🎯 Bowler: {bowler.gameObject.name}, Scene object: {isSceneObject}");
                    
                    if (isSceneObject)
                    {
                        Debug.Log($"🎯 🚫 DEACTIVATING editor-created bowler: {bowler.gameObject.name}");
                        bowler.gameObject.SetActive(false);
                        bowler.enabled = false;
                    }
                    else
                    {
                        Debug.Log($"🎯 ✅ KEEPING runtime bowler active: {bowler.gameObject.name}");
                    }
                }
            }
            
            Debug.Log("🎯 ✅ EDITOR BOWLERS DEACTIVATED");
        }
        
        [ContextMenu("Show Status")]
        void ShowStatusContext()
        {
            Debug.Log(GetTestStatus());
        }
        
        // Context menu methods for zone creation removed - using existing manually created zones
        
        /// <summary>
        /// ?? ENHANCED: Draw gizmos for precise visualization and debugging
        /// </summary>
        void OnDrawGizmos()
        {
            if (ball != null && target != null)
            {
                // ?? PRECISE: Draw exact ball trajectory
                Gizmos.color = Color.red;
                Gizmos.DrawLine(ball.transform.position, target.position);
                
                // 🎯 NEW: Draw rotated trajectory direction if spawn point exists
                if (spawnPoint != null)
                {
                    Vector3 horizontalStart = new Vector3(ball.transform.position.x, 0, ball.transform.position.z);
                    Vector3 horizontalTarget = new Vector3(target.position.x, 0, target.position.z);
                    Vector3 horizontalDirection = (horizontalTarget - horizontalStart).normalized;
                    
                    // Apply spawn point X rotation to show actual trajectory (downward angle)
                    Vector3 spawnForward = spawnPoint.forward;
                    Vector3 spawnUp = spawnPoint.up;
                    float forwardComponent = Vector3.Dot(horizontalDirection, spawnForward);
                    float upComponent = Vector3.Dot(horizontalDirection, spawnUp);
                    Vector3 rotatedDirection = (spawnForward * forwardComponent + spawnUp * upComponent).normalized;
                    
                    // Draw the rotated trajectory
                    Gizmos.color = Color.white;
                    Vector3 rotatedEnd = ball.transform.position + rotatedDirection * Vector3.Distance(horizontalStart, horizontalTarget);
                    Gizmos.DrawLine(ball.transform.position, rotatedEnd);
                    
                    // Draw spawn point rotation indicator (forward direction)
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(spawnPoint.position, spawnPoint.position + spawnPoint.forward * 2f);
                    
                    // Draw spawn point up direction (affected by X rotation)
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(spawnPoint.position, spawnPoint.position + spawnPoint.up * 2f);
                }
                
                // ?? PRECISE: Draw target with larger indicator
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(target.position, 0.3f);
                
                // ?? PRECISE: Draw landing zone (where ball should hit)
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(target.position, 0.1f);
                
                // ?? PRECISE: Draw original position
                if (Application.isPlaying)
                {
                    // 🎯 DYNAMIC: Draw current spawn point position
                    Gizmos.color = Color.blue;
                    Gizmos.DrawWireSphere(spawnPoint.position, 0.15f);
                }
                
                // ?? PRECISE: Draw bounce positions
                if (Application.isPlaying && lastBouncePosition != Vector3.zero)
                {
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawWireSphere(lastBouncePosition, 0.1f);
                }
                
                // ?? PRECISE: Draw predicted landing point
                if (Application.isPlaying && ballRigidbody != null)
                {
                    Vector3 velocity = ballRigidbody.linearVelocity;
                    if (velocity.magnitude > 0.1f)
                    {
                        // Calculate where ball will land based on current velocity
                        float timeToLand = Mathf.Abs(ball.transform.position.y - target.position.y) / Mathf.Abs(velocity.y);
                        Vector3 predictedLanding = ball.transform.position + velocity * timeToLand;
                        
                        Gizmos.color = Color.cyan;
                        Gizmos.DrawWireSphere(predictedLanding, 0.2f);
                        Gizmos.DrawLine(ball.transform.position, predictedLanding);
                    }
                }
            }
        }
        
        /// <summary>
        /// Get current ball instance (for animation-driven bowling)
        /// </summary>
        public GameObject GetCurrentBallInstance()
        {
            return currentBallInstance;
        }
        
        /// <summary>
        /// Debug method to check spawn point logic
        /// </summary>
        [ContextMenu("Debug Spawn Point Logic")]
        public void DebugSpawnPointLogic()
        {
            Debug.Log("🎯 === SPAWN POINT DEBUG ===");
            Debug.Log($"🎯 enableManualKeyInput: {enableManualKeyInput}");
            PlayerAnimationController animController = GetPlayerAnimationController();
            Debug.Log($"🎯 playerAnimationController: {(animController != null ? animController.name : "NULL")}");
            Debug.Log($"🎯 spawnPoint: {(spawnPoint != null ? spawnPoint.name : "NULL")}");
            
            if (animController != null)
            {
                Transform animSpawn = animController.GetAnimationSpawnPoint();
                Debug.Log($"🎯 animationSpawnPoint: {(animSpawn != null ? animSpawn.name : "NULL")}");
                if (animSpawn != null)
                {
                    Debug.Log($"🎯 animationSpawnPoint position: {animSpawn.position}");
                }
            }
            
            // Simulate the logic
            if (enableManualKeyInput)
            {
                Debug.Log("🎯 LOGIC RESULT: Would use BowlingController spawn point (Manual mode)");
            }
            else
            {
                if (animController != null)
                {
                    Transform animSpawn = animController.GetAnimationSpawnPoint();
                    if (animSpawn != null)
                    {
                        Debug.Log("🎯 LOGIC RESULT: Would use PlayerAnimationController spawn point (Animation mode)");
                    }
                    else
                    {
                        Debug.Log("🎯 LOGIC RESULT: Would use BowlingController spawn point (Animation spawn point not assigned)");
                    }
                }
                else
                {
                    Debug.Log("🎯 LOGIC RESULT: Would use BowlingController spawn point (PlayerAnimationController not found)");
                }
            }
            Debug.Log("🎯 =========================");
        }
        
        /// <summary>
        /// Test spawn point position during animation
        /// </summary>
        [ContextMenu("Test Spawn Point During Animation")]
        public void TestSpawnPointDuringAnimation()
        {
            Debug.Log("🎯 === TESTING SPAWN POINT DURING ANIMATION ===");
            
            PlayerAnimationController animController = GetPlayerAnimationController();
            if (animController != null)
            {
                Transform animSpawnPoint = animController.GetAnimationSpawnPoint();
                if (animSpawnPoint != null)
                {
                    Debug.Log($"🎯 Initial RightHand position: {animSpawnPoint.position}");
                    Debug.Log($"🎯 Initial RightHand local position: {animSpawnPoint.localPosition}");
                    Debug.Log($"🎯 RightHand parent: {(animSpawnPoint.parent != null ? animSpawnPoint.parent.name : "NULL")}");
                    
                    // Start coroutine to check position changes
                    StartCoroutine(MonitorSpawnPointPosition(animSpawnPoint));
                }
                else
                {
                    Debug.LogError("🎯 ❌ No animation spawn point assigned!");
                }
            }
            else
            {
                Debug.LogError("🎯 ❌ No PlayerAnimationController found!");
            }
            
            Debug.Log("🎯 ===========================================");
        }
        
        private System.Collections.IEnumerator MonitorSpawnPointPosition(Transform spawnPoint)
        {
            Vector3 lastPosition = spawnPoint.position;
            int frameCount = 0;
            
            while (frameCount < 300) // Monitor for 5 seconds at 60fps
            {
                yield return null;
                frameCount++;
                
                Vector3 currentPosition = spawnPoint.position;
                if (Vector3.Distance(currentPosition, lastPosition) > 0.01f)
                {
                    Debug.Log($"🎯 FRAME {frameCount}: RightHand moved from {lastPosition} to {currentPosition}");
                    lastPosition = currentPosition;
                }
                
                if (frameCount % 60 == 0) // Log every second
                {
                    Debug.Log($"🎯 FRAME {frameCount}: RightHand position: {currentPosition}");
                }
            }
            
            Debug.Log("🎯 Spawn point monitoring completed");
        }
        
        /// <summary>
        /// Debug method to check PlayerAnimationController status
        /// </summary>
        [ContextMenu("Check PlayerAnimationController Status")]
        public void CheckPlayerAnimationControllerStatus()
        {
            Debug.Log("🎯 === PLAYER ANIMATION CONTROLLER STATUS ===");
            Debug.Log($"🎯 enableManualKeyInput: {enableManualKeyInput}");
            PlayerAnimationController animController = GetPlayerAnimationController();
            Debug.Log($"🎯 playerAnimationController: {(animController != null ? animController.name : "NULL - Will auto-find at runtime")}");
            
            if (animController != null)
            {
                Transform animSpawn = animController.GetAnimationSpawnPoint();
                Debug.Log($"🎯 animationSpawnPoint: {(animSpawn != null ? animSpawn.name : "NULL")}");
                if (animSpawn != null)
                {
                    Debug.Log($"🎯 animationSpawnPoint position: {animSpawn.position}");
                }
            }
            
            // Show current selected bowler info
            Debug.Log($"🎯 Selected Bowler Prefab: {(selectedBowlerPrefab != null ? selectedBowlerPrefab.name : "NULL")}");
            
            Debug.Log("🎯 ==========================================");
        }
        
        /// <summary>
        /// Get all available bowler prefab names (for UI)
        /// </summary>
        public string[] GetAvailableBowlerNames()
        {
            if (availableBowlerPrefabs == null) return new string[0];
            
            string[] names = new string[availableBowlerPrefabs.Length];
            for (int i = 0; i < availableBowlerPrefabs.Length; i++)
            {
                names[i] = availableBowlerPrefabs[i] != null ? availableBowlerPrefabs[i].name : "NULL";
            }
            return names;
        }
        
        /// <summary>
        /// Check current bowler setup
        /// </summary>
        [ContextMenu("Check Current Bowler")]
        public void CheckCurrentBowler()
        {
            Debug.Log("🎯 === CURRENT BOWLER STATUS ===");
            Debug.Log($"🎯 Selected Bowler Prefab: {(selectedBowlerPrefab != null ? selectedBowlerPrefab.name : "NULL")}");
            
            if (selectedBowlerPrefab != null)
            {
                PlayerAnimationController controller = selectedBowlerPrefab.GetComponent<PlayerAnimationController>();
                if (controller != null)
                {
                    Debug.Log($"🎯 ✅ PlayerAnimationController found in {selectedBowlerPrefab.name}");
                    
                    Transform spawnPoint = controller.GetAnimationSpawnPoint();
                    if (spawnPoint != null)
                    {
                        Debug.Log($"🎯 ✅ Animation Spawn Point: {spawnPoint.name} at {spawnPoint.position}");
                    }
                    else
                    {
                        Debug.LogWarning($"🎯 ❌ No Animation Spawn Point found in {selectedBowlerPrefab.name}");
                    }
                    
                    BowlerProfile profile = selectedBowlerPrefab.GetComponent<BowlerProfile>();
                    if (profile != null)
                    {
                        Debug.Log($"🎯 ✅ BowlerProfile found in {selectedBowlerPrefab.name}");
                        Debug.Log($"🎯 Default Delivery: {profile.GetDefaultDeliveryType()}");
                        Debug.Log($"🎯 Allowed Deliveries: {string.Join(", ", profile.GetAllowedDeliveryTypes())}");
                    }
                    else
                    {
                        Debug.LogWarning($"🎯 ❌ No BowlerProfile found in {selectedBowlerPrefab.name}");
                    }
                }
                else
                {
                    Debug.LogWarning($"🎯 ❌ No PlayerAnimationController found in {selectedBowlerPrefab.name}");
                }
            }
            else
            {
                Debug.LogWarning($"🎯 ❌ No bowler prefab selected");
            }
            
            PlayerAnimationController animController = GetPlayerAnimationController();
            Debug.Log($"🎯 PlayerAnimationController Reference: {(animController != null ? animController.name : "NULL")}");
            Debug.Log("🎯 ==============================");
        }

        [ContextMenu("Select Bowler 0")]
        public void SelectBowler0()
        {
            SelectBowlerPrefab(0);
        }

        [ContextMenu("Select Bowler 1")]
        public void SelectBowler1()
        {
            SelectBowlerPrefab(1);
        }

        [ContextMenu("Select Bowler 2")]
        public void SelectBowler2()
        {
            SelectBowlerPrefab(2);
        }

        [ContextMenu("Select Bowler 3")]
        public void SelectBowler3()
        {
            SelectBowlerPrefab(3);
        }

        [ContextMenu("Select Bowler 4")]
        public void SelectBowler4()
        {
            SelectBowlerPrefab(4);
        }
        
        /// <summary>
        /// Manually instantiate the selected bowler (for testing)
        /// </summary>
        [ContextMenu("Instantiate Selected Bowler")]
        public void InstantiateSelectedBowlerContext()
        {
            Debug.Log("🎯 === MANUALLY INSTANTIATING SELECTED BOWLER ===");
            InstantiateSelectedBowler();
            Debug.Log("🎯 ============================================");
        }
        
        /// <summary>
        /// Destroy the current bowler instance (for testing)
        /// </summary>
        [ContextMenu("Destroy Current Bowler Instance")]
        public void DestroyCurrentBowlerInstance()
        {
            Debug.Log("🎯 === DESTROYING CURRENT BOWLER INSTANCE ===");
            if (currentBowlerInstance != null)
            {
                Debug.Log($"🎯 Destroying: {currentBowlerInstance.name}");
                DestroyImmediate(currentBowlerInstance);
                currentBowlerInstance = null;
                playerAnimationController = null;
                Debug.Log("🎯 ✅ Bowler instance destroyed and references cleared");
            }
            else
            {
                Debug.Log("🎯 No bowler instance to destroy");
            }
            Debug.Log("🎯 =========================================");
        }
        
        /// <summary>
        /// Toggle auto-instantiation on/off
        /// </summary>
        [ContextMenu("Toggle Auto-Instantiation")]
        public void ToggleAutoInstantiation()
        {
            autoInstantiateBowler = !autoInstantiateBowler;
            Debug.Log($"🎯 Auto-instantiation: {(autoInstantiateBowler ? "ENABLED" : "DISABLED")}");
        }
        
        /// <summary>
        /// Manually destroy current ball (for testing)
        /// </summary>
        [ContextMenu("Manually Destroy Current Ball")]
        public void ManuallyDestroyCurrentBall()
        {
            Debug.Log("🏏 === MANUAL BALL DESTRUCTION ===");
            Debug.Log($"🏏 Current ball instance: {(currentBallInstance != null ? currentBallInstance.name : "NULL")}");
            Debug.Log($"🏏 BallSettingsSO: {(ballSettingsSO != null ? "FOUND" : "NULL")}");
            if (ballSettingsSO != null)
            {
                Debug.Log($"🏏 EnableAutoDestroy: {ballSettingsSO.EnableAutoDestroy}");
                Debug.Log($"🏏 DestroyDelay: {ballSettingsSO.DestroyDelay}");
            }
            
            if (currentBallInstance != null)
            {
                Debug.Log($"🏏 🗑️ MANUALLY DESTROYING ball: {currentBallInstance.name}");
                Destroy(currentBallInstance);
                currentBallInstance = null;
                ballIsBowled = false;
                hasLanded = false;
                Debug.Log("🏏 ✅ Ball manually destroyed and state reset");
            }
            else
            {
                Debug.Log("🏏 ❌ No ball to destroy");
            }
            Debug.Log("🏏 ================================");
        }

        private Transform GetFallbackSpawnTransform(PlayerAnimationController controller)
        {
            if (spawnPoint != null)
            {
                return spawnPoint;
            }

            if (ballSpawnPoint != null)
            {
                return ballSpawnPoint;
            }

            if (controller != null)
            {
                Transform animSpawn = controller.GetAnimationSpawnPoint();
                if (animSpawn != null)
                {
                    return animSpawn;
                }
            }

            return GetInstanceSpawnTransform(currentBowlerInstance);
        }

        private Transform GetMappingSpawnTransform(GameObject bowlerInstance)
        {
            if (bowlerInstance == null)
            {
                return null;
            }

            BowlerSpawnMapping mapping = GetSpawnMappingForBowler(bowlerInstance);
            if (mapping == null)
            {
                return null;
            }

            return mapping.useSpawn01 ? mapping.spawn01 : mapping.spawn02;
        }

        private void CacheInstanceSnapshot(GameObject bowlerInstance, Vector3 position, Quaternion rotation, Transform spawnTransform)
        {
            if (bowlerInstance == null)
            {
                return;
            }

            int instanceId = bowlerInstance.GetInstanceID();
            if (!bowlerInstanceSnapshots.TryGetValue(instanceId, out InstanceSpawnSnapshot snapshot))
            {
                snapshot = new InstanceSpawnSnapshot();
                bowlerInstanceSnapshots[instanceId] = snapshot;
            }

            snapshot.WorldPosition = position;
            snapshot.WorldRotation = rotation;
            snapshot.SpawnTransform = spawnTransform;

            string prefabName = bowlerInstance.name.Replace("(Clone)", string.Empty);
            if (!autoSpawnToggle.ContainsKey(prefabName))
            {
                autoSpawnToggle[prefabName] = false;
            }
        }

        private Transform GetInstanceSpawnTransform(GameObject bowlerInstance)
        {
            if (bowlerInstance == null)
            {
                return null;
            }

            if (bowlerInstanceSnapshots.TryGetValue(bowlerInstance.GetInstanceID(), out InstanceSpawnSnapshot snapshot))
            {
                if (snapshot.SpawnTransform == null)
                {
                    snapshot.SpawnTransform = CreateRuntimeSnapshotTransform(bowlerInstance.name, snapshot.WorldPosition, snapshot.WorldRotation);
                }
                return snapshot.SpawnTransform;
            }

            return null;
        }

        private Transform CreateRuntimeSnapshotTransform(string baseName, Vector3 position, Quaternion rotation)
        {
            GameObject temp = new GameObject(string.IsNullOrEmpty(baseName) ? "BowlerSpawn" : $"{baseName}_Spawn");
            temp.transform.SetParent(transform);
            temp.transform.position = position;
            temp.transform.rotation = rotation;
            return temp.transform;
        }

        private void RemoveInstanceSnapshot(GameObject bowlerInstance)
        {
            if (bowlerInstance == null)
            {
                return;
            }

            bowlerInstanceSnapshots.Remove(bowlerInstance.GetInstanceID());
        }

        private Transform GetAutoResolvedSpawn(GameObject bowlerInstance, bool useAlternative)
        {
            string prefabName = bowlerInstance != null ? bowlerInstance.name.Replace("(Clone)", string.Empty) : string.Empty;
            if (string.IsNullOrEmpty(prefabName))
            {
                return null;
            }

            if (spawnPointsRoot == null)
            {
                GameObject rootGO = GameObject.Find("Spawn Points");
                spawnPointsRoot = rootGO != null ? rootGO.transform : null;
            }

            if (spawnPointsRoot == null)
            {
                return null;
            }

            string groupName = ResolveSpawnGroupName(prefabName);
            if (string.IsNullOrEmpty(groupName))
            {
                return null;
            }

            Transform groupTransform = FindChildByName(spawnPointsRoot, groupName);
            if (groupTransform == null)
            {
                return null;
            }

            string spawnNode = useAlternative ? "Spawn 02" : "Spawn 01";
            Transform spawnTransform = FindChildByName(groupTransform, spawnNode);
            if (spawnTransform == null && useAlternative)
            {
                spawnTransform = FindChildByName(groupTransform, "Spawn 01");
            }
            return spawnTransform;
        }

        private bool ToggleAutoSpawn(GameObject bowlerInstance, BowlerSpawnMapping mapping)
        {
            if (bowlerInstance == null)
            {
                return false;
            }

            if (mapping != null)
            {
                return mapping.useSpawn01;
            }

            string prefabName = bowlerInstance.name.Replace("(Clone)", string.Empty);
            if (!autoSpawnToggle.TryGetValue(prefabName, out bool toggle))
            {
                toggle = false;
            }

            toggle = !toggle;
            autoSpawnToggle[prefabName] = toggle;
            return toggle;
        }

        private string ResolveSpawnGroupName(string prefabName)
        {
            string lower = prefabName.ToLowerInvariant();
            if (lower.Contains("fast") || lower.Contains("seam"))
            {
                return "FAST BOWLER SPAWN POINTS";
            }
            if (lower.Contains("leg") || lower.Contains("ortho"))
            {
                return "LEG SPIN & ORTHO BOWLER SPAWN";
            }
            if (lower.Contains("off") || lower.Contains("wrist"))
            {
                return "OFF SPIN & WRIST BOWLER SPAWN";
            }
            return null;
        }

        private static Transform FindChildByName(Transform parent, string childName)
        {
            if (parent == null)
            {
                return null;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (string.Equals(child.name, childName, StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }
            }

            return null;
        }

        private void ApplyBowlerTransform(Vector3 position, Quaternion rotation)
        {
            currentBowlerInstance.SetActive(false);
            currentBowlerInstance.transform.position = position;
            currentBowlerInstance.transform.rotation = rotation;
            currentBowlerInstance.SetActive(true);
        }

        private bool GetAutoSpawnState(GameObject bowlerInstance)
        {
            if (bowlerInstance == null)
            {
                return false;
            }

            string prefabName = bowlerInstance.name.Replace("(Clone)", string.Empty);
            if (autoSpawnToggle.TryGetValue(prefabName, out bool state))
            {
                return state;
            }

            return false;
        }
    }
    
    /// <summary>
    /// Bowling length categories
    /// </summary>
    public enum BowlingLength
    {
        Yorker,      // Very close to batsman
        FullLength,  // Close to batsman
        GoodLength,  // Standard length
        ShortLength, // Short of good length
        Bouncer      // Very short
    }
    
    /// <summary>
    /// Maps a bowler prefab to its two spawn positions in the scene
    /// </summary>
    [System.Serializable]
    public class BowlerSpawnMapping
    {
        [Header("Bowler Prefab")]
        public GameObject bowlerPrefab;
        
        [Header("Spawn Positions")]
        public Transform spawn01;
        public Transform spawn02;
        
        [Header("Current State")]
        public bool useSpawn01 = true; // Which spawn position is currently active
    }
    
    struct VolumeFogState
    {
        public readonly bool Enabled;
        public readonly float MeanFreePath;
        public readonly float BaseHeight;
        public readonly float MaximumHeight;
        public readonly Color Albedo;

        public VolumeFogState(bool enabled, float meanFreePath, float baseHeight, float maximumHeight, Color albedo)
        {
            Enabled = enabled;
            MeanFreePath = meanFreePath;
            BaseHeight = baseHeight;
            MaximumHeight = maximumHeight;
            Albedo = albedo;
        }
    }

    class InstanceSpawnSnapshot
    {
        public Vector3 WorldPosition;
        public Quaternion WorldRotation;
        public Transform SpawnTransform;
    }
}
