using System.Collections;
using UnityEngine;

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
        [SerializeField] private BallSettings ballSettings; // Single BallSettings component with all bowling length settings
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
        [SerializeField] private KeyCode stopKey = KeyCode.Escape;
        
        // Private variables
        private Vector3 originalBallPosition;
        private Rigidbody ballRigidbody;
        private bool isRunning = false;
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
        
        void Start()
        {
            SetupTest();
            // Using existing bowling zones (manually created under Pitching Area)
            Debug.Log("🎯 Using existing bowling zones from Pitching Area");
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
            
            // Store original position
            originalBallPosition = spawnPoint.position;
            
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
            
            // Store original values for comparison
            float originalSpeed = ballSettings.GlobalBallSpeed;
            float originalArc = ballSettings.ArcHeight;
            float originalBounce = ballSettings.BounceForce;
            
            // Get current bowling length based on zone detection
            BowlingLength lengthCategory = GetCurrentBowlingLength();
            
            // Adjust settings based on length
            AdjustBallSettingsForLength(ballSettings, lengthCategory);
            
            // Apply rotation to spawn point based on bowling length
            ApplyBowlingRotation(lengthCategory);
            
            // Colorful debug information with before/after comparison
            string lengthColor = GetLengthColor(lengthCategory);
            string speedColor = originalSpeed != ballSettings.GlobalBallSpeed ? "<color=#00FF00>" : "<color=#FF0000>";
            string arcColor = originalArc != ballSettings.ArcHeight ? "<color=#00FF00>" : "<color=#FF0000>";
            string bounceColor = originalBounce != ballSettings.BounceForce ? "<color=#00FF00>" : "<color=#FF0000>";
            
            Debug.Log($"{lengthColor}🎯 BOWLING LENGTH: {lengthCategory} (Zone-based detection)</color>");
            Debug.Log($"{speedColor}⚡ GLOBAL BALL SPEED: {originalSpeed:F1} → {ballSettings.GlobalBallSpeed:F1} m/s (applies to all lengths)</color>");
            Debug.Log($"{arcColor}📈 ARC HEIGHT: {originalArc:F1} → {ballSettings.ArcHeight:F1} m</color>");
            Debug.Log($"{bounceColor}🏀 BOUNCE FORCE: {originalBounce:F2} → {ballSettings.BounceForce:F2}</color>");
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
            Debug.Log($"<color=#FFD700>🔍 ballSettings reference: {ballSettings != null}</color>");
            
            if (ballSettings == null)
            {
                Debug.LogWarning($"⚠️ No BallSettings reference found! Using hardcoded values for {length}.");
                // Use hardcoded values as fallback
                ApplyHardcodedSettings(targetBallSettings, length);
                return;
            }
            
            Debug.Log($"<color=#FFD700>🔍 Using Inspector BallSettings for {length}</color>");
            
            // 🎯 NEW: Calculate realistic physics bounce values based on speed and energy
            ballSettings.CalculatePhysicsBounce(length);
            
            // Apply settings based on bowling length from the single BallSettings component
            // Note: Speed is now global, only arc, bounce, and rotation are length-specific
            switch (length)
            {
                case BowlingLength.Yorker:
                    Debug.Log($"<color=#FF0000>🔍 Yorker Inspector Values: Global Speed={ballSettings.GlobalBallSpeed}, Arc={ballSettings.YorkerArcHeight}, Bounce={ballSettings.YorkerBounceForce}</color>");
                    targetBallSettings.SetArcHeight(ballSettings.YorkerArcHeight);
                    targetBallSettings.SetBounceForce(ballSettings.YorkerBounceForce);
                    targetBallSettings.SetBounceFriction(ballSettings.YorkerBounceFriction);
                    break;
                    
                case BowlingLength.FullLength:
                    targetBallSettings.SetArcHeight(ballSettings.FullLengthArcHeight);
                    targetBallSettings.SetBounceForce(ballSettings.FullLengthBounceForce);
                    targetBallSettings.SetBounceFriction(ballSettings.FullLengthBounceFriction);
                    break;
                    
                case BowlingLength.GoodLength:
                    targetBallSettings.SetArcHeight(ballSettings.GoodLengthArcHeight);
                    targetBallSettings.SetBounceForce(ballSettings.GoodLengthBounceForce);
                    targetBallSettings.SetBounceFriction(ballSettings.GoodLengthBounceFriction);
                    break;
                    
                case BowlingLength.ShortLength:
                    targetBallSettings.SetArcHeight(ballSettings.ShortLengthArcHeight);
                    targetBallSettings.SetBounceForce(ballSettings.ShortLengthBounceForce);
                    targetBallSettings.SetBounceFriction(ballSettings.ShortLengthBounceFriction);
                    break;
                    
                case BowlingLength.Bouncer:
                    targetBallSettings.SetArcHeight(ballSettings.BouncerArcHeight);
                    targetBallSettings.SetBounceForce(ballSettings.BouncerBounceForce);
                    targetBallSettings.SetBounceFriction(ballSettings.BouncerBounceFriction);
                    break;
            }
            
            // Copy common settings
            targetBallSettings.SetGravity(ballSettings.Gravity);
            targetBallSettings.SetMaxBounces(ballSettings.MaxBounces);
            targetBallSettings.SetUseRealisticPhysics(ballSettings.UseRealisticPhysics);
            
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
            if (ballSettings == null) return 0f;
            
            switch (length)
            {
                case BowlingLength.Yorker:
                    return ballSettings.YorkerRotationX; // X rotation for downward angle
                case BowlingLength.FullLength:
                    return ballSettings.FullLengthRotationX; // X rotation for downward angle
                case BowlingLength.GoodLength:
                    return ballSettings.GoodLengthRotationX; // X rotation for downward angle
                case BowlingLength.ShortLength:
                    return ballSettings.ShortLengthRotationX; // X rotation for downward angle
                case BowlingLength.Bouncer:
                    return ballSettings.BouncerRotationX; // X rotation for downward angle
                default:
                    return 0f;
            }
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
            
            // ?? FORCE: Always update trail settings to ensure white color
            trail.time = 0.5f; // Short trail duration
            trail.startWidth = 0.08f; // Thick start width
            trail.endWidth = 0.02f; // Thick end width
            trail.minVertexDistance = 0.1f; // Minimum distance between trail points
            trail.emitting = true; // Ensure trail is emitting
            
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
            Debug.Log($"?? Trail material color: {trail.material.color}");
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
                Debug.Log("?? Bounce component added to ball instance");
            }
            else
            {
                bounceComponent.Initialize(this);
                Debug.Log("?? Bounce component initialized for ball instance");
            }
            
            // Add trail renderer for visual effect
            TrailRenderer trail = ballInstance.GetComponent<TrailRenderer>();
            if (trail == null)
            {
                trail = ballInstance.AddComponent<TrailRenderer>();
            }
            
            // ?? FORCE: Always update trail settings to ensure white color
            trail.time = 0.5f; // Short trail duration
            trail.startWidth = 0.08f; // Thick start width
            trail.endWidth = 0.02f; // Thick end width
            trail.minVertexDistance = 0.1f; // Minimum distance between trail points
            trail.emitting = true; // Ensure trail is emitting
            
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
            // ?? NEW: Instantiate new ball with S key
            if (Input.GetKeyDown(KeyCode.S))
            {
                Debug.Log("🎯 S key pressed - Creating new ball");
                InstantiateNewBall();
            }
            
            // ?? NEW: Bowl current ball with SPACE key
            if (Input.GetKeyDown(KeyCode.Space))
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
            if (currentBallInstance != null)
            {
                DestroyImmediate(currentBallInstance);
                currentBallInstance = null;
                Debug.Log("🎯 Destroyed previous ball instance");
            }
            
            // Instantiate new ball at spawn point
            if (ball != null && spawnPoint != null)
            {
                currentBallInstance = Instantiate(ball, spawnPoint.position, spawnPoint.rotation);
                Debug.Log($"🎯 New ball instantiated at: {spawnPoint.position}");
                Debug.Log($"🎯 Ball instance created: {currentBallInstance != null}");
                
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
            Debug.Log($"🎯 BowlCurrentBall called - currentBallInstance: {currentBallInstance != null}, ballIsBowled: {ballIsBowled}");
            
            if (currentBallInstance == null)
            {
                Debug.LogWarning("🎯 No ball to bowl! Press S to create a new ball first.");
                return;
            }
            
            if (ballIsBowled)
            {
                Debug.LogWarning("🎯 Ball already bowled! Wait for it to be destroyed or press S for new ball.");
                return;
            }
            
            Debug.Log("🎯 Bowling current ball...");
            ballIsBowled = true;
            
            // Start bowling process
            StartCoroutine(BowlAndDestroy());
        }
        
        /// <summary>
        /// ?? NEW: Bowl ball - ball will destroy itself after 5 seconds
        /// </summary>
        IEnumerator BowlAndDestroy()
        {
            // Bowl the ball
            yield return StartCoroutine(BowlToTarget());
            
            // Wait for ball to land and bounce
            yield return StartCoroutine(WaitForLanding());
            
            // Reset ball state so new balls can be bowled
            ballIsBowled = false;
            hasLanded = false;
            ResetBounceState();
            
            // Ball will destroy itself after 5 seconds via BallAutoDestroy script
            Debug.Log("?? Ball has landed and bounced - it will destroy itself in 5 seconds");
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
            if (Vector3.Distance(ballToBowl.transform.position, originalBallPosition) > 0.1f)
            {
                Debug.LogWarning("?? Ball not at original position! Forcing reset...");
                ballToBowl.transform.position = originalBallPosition;
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
            Vector3 startPosition = ballToBowl.transform.position;
            
            // Calculate horizontal distance to target
            Vector3 horizontalStart = new Vector3(startPosition.x, 0, startPosition.z);
            Vector3 horizontalTarget = new Vector3(targetPosition.x, 0, targetPosition.z);
            float horizontalDistance = Vector3.Distance(horizontalStart, horizontalTarget);

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
            
            // ?? NEW: Read ball speed from speed controller if available, otherwise use ball settings
            float ballSpeed;
            if (speedController != null)
            {
                ballSpeed = speedController.GetCurrentSpeed();
                Debug.Log($"🎯 SPEED FROM SLIDER: {ballSpeed} m/s");
            }
            else
            {
                ballSpeed = ballSettings.BallSpeed;
                Debug.Log($"🎯 SPEED FROM BALL SETTINGS: {ballSpeed} m/s");
            }
            float arcHeight = ballSettings.ArcHeight;
            float gravity = ballSettings.Gravity;
            
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
            
            // 🎯 SIMPLIFIED: Use spawn point's forward direction directly (includes both X and Y rotation)
            Vector3 horizontalDirection;
            if (spawnPoint != null)
            {
                // Use spawn point's forward direction which already includes both X and Y rotation
                horizontalDirection = spawnPoint.forward;
                // Remove Y component to keep it horizontal
                horizontalDirection.y = 0f;
                horizontalDirection = horizontalDirection.normalized;
                
                Debug.Log($"🎯 ROTATION APPLIED: Spawn rotation {spawnPoint.rotation.eulerAngles}, Forward direction {spawnPoint.forward}, Horizontal direction {horizontalDirection}");
            }
            else
            {
                // Fallback to direct target direction
                horizontalDirection = (horizontalTarget - horizontalStart).normalized;
            }
            
            // 🎯 SIMPLIFIED X ROTATION: Apply smaller, more controlled rotation effect (post ballistic)
            float preAdjustY = initialVelocity.y;
            float rotationXRadians = spawnPoint != null ? spawnPoint.rotation.eulerAngles.x * Mathf.Deg2Rad : 0f;
            float rotationEffect = Mathf.Sin(rotationXRadians); // This gives us the downward component
            
            // 🎯 REDUCED ROTATION EFFECT: Much smaller impact on trajectory
            float rotationMultiplier = 0.1f; // Reduced from 0.5f to 0.1f
            float adjustedYVelocity = initialVelocity.y + (rotationEffect * ballSpeed * rotationMultiplier);

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
            Debug.Log($"🎯 APPLYING VELOCITY: UseRealisticPhysics={ballSettings.UseRealisticPhysics}, useSmoothMovement={useSmoothMovement}");
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
            
             // 🎯 CURVED PATH CHECK: Force kinematic movement for curved path deliveries
            bool useKinematicForCurvedPath = false;
            if (deliverySystem != null)
            {
                DeliveryType currentDelivery = deliverySystem.GetCurrentDeliveryType();
                if (currentDelivery == DeliveryType.Inswing)
                {
                    // Get InswingDelivery component from DeliverySystem
                    InswingDelivery inswingDelivery = deliverySystem.GetComponent<InswingDelivery>();
                    if (inswingDelivery == null)
                    {
                        // Try to find it on the same GameObject as DeliverySystem
                        inswingDelivery = deliverySystem.transform.GetComponent<InswingDelivery>();
                    }
                    
                    if (inswingDelivery != null && inswingDelivery.IsCurvedPathEnabled())
                    {
                        useKinematicForCurvedPath = true;
                        Debug.Log("🎯 CURVED PATH: Forcing kinematic movement for curved path delivery");
                    }
                    else
                    {
                        Debug.LogWarning($"🎯 CURVED PATH: InswingDelivery not found or curved path disabled. InswingDelivery: {inswingDelivery != null}, CurvedPathEnabled: {inswingDelivery?.IsCurvedPathEnabled()}");
                    }
                }
            }
            
            // 🎯 USE PATH FOLLOWER FOR INSWING CURVED PATH
            if (useKinematicForCurvedPath)
            {
                Debug.Log("🎯 Using PathFollower for Inswing curved path");
                InswingDelivery inswingDelivery = deliverySystem.GetComponent<InswingDelivery>() ?? deliverySystem.transform.GetComponent<InswingDelivery>();
                if (inswingDelivery != null)
                {
                    Vector3[] path = inswingDelivery.GetCurvedPathPoints(startPosition, targetPosition, ballSpeed, 30);
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

                    var follower = currentBallInstance.AddComponent<PathFollower>();
                    // Use InswingDelivery's public pathArcHeight so you can tune elevation in the Inspector
                    float addedArc = inswingDelivery.pathArcHeight;
                    follower.Initialize(path, ballSpeed, addedArc, () =>
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
                    });
                    follower.Begin();
                }
                else
                {
                    // Fallback to simple kinematic move
                    StartCoroutine(MoveBallKinematic(startPosition, targetPosition, timeToReach));
                }
            }
            else if (!ballSettings.UseRealisticPhysics)
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
            Debug.Log($"<color=#FFD700>🎯 Using ball settings: Speed={ballSpeed}, Arc={arcHeight}, Bounce={ballSettings.BounceForce}, Gravity={gravity}</color>");
            
            // Wait for ball to reach target area
            float waitTime = Mathf.Clamp(timeToReach, 0.1f, 5f);
            yield return new WaitForSeconds(waitTime);
            
            // Mark as landed
            hasLanded = true;
            Debug.Log("?? Ball has landed on target!");
            
            // 🎯 SPEED BOOST: Trigger speed boost when ball hits target
            BallSpeedBoost targetSpeedBoost = currentBallInstance?.GetComponent<BallSpeedBoost>();
            if (targetSpeedBoost != null)
            {
                targetSpeedBoost.OnTargetHit();
                DeliveryType currentDelivery = deliverySystem?.GetCurrentDeliveryType() ?? DeliveryType.Flat;
                Debug.Log($"🎯 SPEED BOOST: Triggered for {currentDelivery} delivery with initial speed {ballSpeed:F1} m/s");
                Debug.Log($"🎯 DELIVERY CONFIRMATION: Speed boost applied to {currentDelivery} delivery");
            }
            else
            {
                Debug.LogWarning($"🎯 SPEED BOOST: BallSpeedBoost component not found when ball hit target!");
                Debug.LogWarning($"🎯 SPEED BOOST: This should not happen if speed boost was set up correctly during ball launch.");
            }
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
            float arcHeight = ballSettings != null ? ballSettings.ArcHeight : 1f;
            float gravity = ballSettings != null ? ballSettings.Gravity : 9.81f;
            
            // Use ball speed for kinematic movement (simpler and more predictable)
            float ballSpeed = ballSettings != null ? ballSettings.BallSpeed : 12f;
            
            // ?? FIXED: Use realistic cricket bowling arc (much lower)
            float realisticArcHeight = arcHeight * 0.2f; // Same reduction as physics version
            
            // Speed-based arc reduction: slower balls get lower arc to avoid going too high
            float speedFactor = Mathf.Clamp(ballSpeed / 12f, 0.5f, 1.2f); // Normalize around 12 m/s
            realisticArcHeight *= speedFactor;
            
            // 🎯 SIMPLIFIED KINEMATIC: Use simpler duration calculation
            Vector3 horizontalStart = new Vector3(startPos.x, 0, startPos.z);
            Vector3 horizontalEnd = new Vector3(endPos.x, 0, endPos.z);
            float horizontalDistance = Vector3.Distance(horizontalStart, horizontalEnd);
            float baseDuration = horizontalDistance / ballSpeed;
            
            // Apply small correction for arc height
            float arcTimeFactor = 1f + (realisticArcHeight * 0.1f);
            duration = baseDuration * arcTimeFactor;
            
            // 🎯 SIMPLIFIED: Use spawn point's forward direction directly (includes both X and Y rotation)
            Vector3 trajectoryDirection;
            if (spawnPoint != null)
            {
                // Use spawn point's forward direction which already includes both X and Y rotation
                trajectoryDirection = spawnPoint.forward;
                // Remove Y component to keep it horizontal
                trajectoryDirection.y = 0f;
                trajectoryDirection = trajectoryDirection.normalized;
                
                Debug.Log($"🎯 KINEMATIC ROTATION: Applied spawn rotation {spawnPoint.rotation.eulerAngles} to trajectory direction {trajectoryDirection}");
            }
            else
            {
                // Fallback to direct target direction
                trajectoryDirection = (endPos - startPos).normalized;
            }
            
            // 🎯 CRITICAL FIX: Apply X rotation effect to arc height for kinematic movement
            float rotationXRadians = spawnPoint != null ? spawnPoint.rotation.eulerAngles.x * Mathf.Deg2Rad : 0f;
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
            }
            
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
                
                ball.transform.position = currentPos;
                
                yield return null;
            }
            
            // Ensure ball is exactly at target
            ball.transform.position = endPos;
        }
        
        /// <summary>
        /// ?? WORKING: Wait for ball to land on target and finish bouncing
        /// </summary>
        IEnumerator WaitForLanding()
        {
            Debug.Log("?? Waiting for ball to finish bouncing and settle...");
            
            // Wait for initial landing
            yield return new WaitForSeconds(0.3f);
            
            // Wait for bounces to complete
            while (isBouncing && currentBounces < 3) // Default max bounces
            {
                yield return new WaitForSeconds(0.1f);
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
            
            if (ballSettings != null && currentBounces <= ballSettings.MaxBounces)
            {
                // Calculate bounce velocity with friction
                Vector3 newVelocity = bounceVelocity * ballSettings.BounceFriction;
                
                // Bounce force tuning
                // Identify bouncer lengths (short/mid) and reduce bounce to avoid too-high hop
                bool isBouncerLength = currentLength01 <= 0.35f; // shorter distance → bouncer
                float enhancedBounceForce;
                if (isBouncerLength)
                {
                    // Reduce bounce to ~60% of base for bouncers (user request)
                    enhancedBounceForce = ballSettings.BounceForce * 0.6f;
                }
                else
                {
                    // Gentle scaling for other lengths; avoid exceeding base by much
                    enhancedBounceForce = ballSettings.BounceForce;
                    if (currentBounces == 1)
                        enhancedBounceForce *= 1.05f; // very slight first-bounce lift
                    else if (currentBounces == 2)
                        enhancedBounceForce *= 0.9f;
                    else
                        enhancedBounceForce *= 0.75f;

                    // Length-based scaling with max 1.0 to avoid extra boost
                    float lengthBounceScale = Mathf.Lerp(1.0f, 0.65f, currentLength01);
                    lengthBounceScale = Mathf.Clamp(lengthBounceScale, 0.65f, 1.0f);
                enhancedBounceForce *= lengthBounceScale;
                }
                
                // Apply enhanced bounce force to Y velocity
                newVelocity.y = Mathf.Abs(bounceVelocity.y) * enhancedBounceForce;
                
                // ?? ADDITIONAL: Preserve some horizontal momentum for realistic cricket bounce
                newVelocity.x *= 0.9f; // Keep 90% of horizontal velocity
                newVelocity.z *= 0.9f; // Keep 90% of horizontal velocity
                
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
            if (ballSettings != null && currentBounces >= ballSettings.MaxBounces)
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
            Vector3 targetPos = originalBallPosition;
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
                ball.transform.position = originalBallPosition;
                
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
                    Gizmos.color = Color.blue;
                    Gizmos.DrawWireSphere(originalBallPosition, 0.15f);
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
}
