using System.Collections;
using UnityEngine;

namespace CricketGame
{
    /// <summary>
    /// Continuous Bowling Test System - WITH BOUNCE PHYSICS
    /// Automatically bowls ball to target with realistic cricket physics and bouncing, waits 3 seconds, returns to original position, and repeats
    /// </summary>
    public class ContinuousBowlingTest_WithBounce : MonoBehaviour
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
            float originalSpeed = ballSettings.BallSpeed;
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
            string speedColor = originalSpeed != ballSettings.BallSpeed ? "<color=#00FF00>" : "<color=#FF0000>";
            string arcColor = originalArc != ballSettings.ArcHeight ? "<color=#00FF00>" : "<color=#FF0000>";
            string bounceColor = originalBounce != ballSettings.BounceForce ? "<color=#00FF00>" : "<color=#FF0000>";
            
            Debug.Log($"{lengthColor}🎯 BOWLING LENGTH: {lengthCategory} (Zone-based detection)</color>");
            Debug.Log($"{speedColor}⚡ BALL SPEED: {originalSpeed:F1} → {ballSettings.BallSpeed:F1} m/s</color>");
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
            
            // Apply settings based on bowling length from the single BallSettings component
            switch (length)
            {
                case BowlingLength.Yorker:
                    Debug.Log($"<color=#FF0000>🔍 Yorker Inspector Values: Speed={ballSettings.YorkerSpeed}, Arc={ballSettings.YorkerArcHeight}, Bounce={ballSettings.YorkerBounceForce}</color>");
                    targetBallSettings.SetBallSpeed(ballSettings.YorkerSpeed);
                    targetBallSettings.SetArcHeight(ballSettings.YorkerArcHeight);
                    targetBallSettings.SetBounceForce(ballSettings.YorkerBounceForce);
                    targetBallSettings.SetBounceFriction(ballSettings.YorkerBounceFriction);
                    break;
                    
                case BowlingLength.FullLength:
                    targetBallSettings.SetBallSpeed(ballSettings.FullLengthSpeed);
                    targetBallSettings.SetArcHeight(ballSettings.FullLengthArcHeight);
                    targetBallSettings.SetBounceForce(ballSettings.FullLengthBounceForce);
                    targetBallSettings.SetBounceFriction(ballSettings.FullLengthBounceFriction);
                    break;
                    
                case BowlingLength.GoodLength:
                    targetBallSettings.SetBallSpeed(ballSettings.GoodLengthSpeed);
                    targetBallSettings.SetArcHeight(ballSettings.GoodLengthArcHeight);
                    targetBallSettings.SetBounceForce(ballSettings.GoodLengthBounceForce);
                    targetBallSettings.SetBounceFriction(ballSettings.GoodLengthBounceFriction);
                    break;
                    
                case BowlingLength.ShortLength:
                    targetBallSettings.SetBallSpeed(ballSettings.ShortLengthSpeed);
                    targetBallSettings.SetArcHeight(ballSettings.ShortLengthArcHeight);
                    targetBallSettings.SetBounceForce(ballSettings.ShortLengthBounceForce);
                    targetBallSettings.SetBounceFriction(ballSettings.ShortLengthBounceFriction);
                    break;
                    
                case BowlingLength.Bouncer:
                    targetBallSettings.SetBallSpeed(ballSettings.BouncerSpeed);
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
            switch (length)
            {
                case BowlingLength.Yorker:
                    targetBallSettings.SetBallSpeed(15f);
                    targetBallSettings.SetArcHeight(1.5f);
                    targetBallSettings.SetBounceForce(1.2f);
                    targetBallSettings.SetBounceFriction(0.9f);
                    break;
                    
                case BowlingLength.FullLength:
                    targetBallSettings.SetBallSpeed(12f);
                    targetBallSettings.SetArcHeight(1.2f);
                    targetBallSettings.SetBounceForce(0.9f);
                    targetBallSettings.SetBounceFriction(0.8f);
                    break;
                    
                case BowlingLength.GoodLength:
                    targetBallSettings.SetBallSpeed(13f);
                    targetBallSettings.SetArcHeight(1.3f);
                    targetBallSettings.SetBounceForce(1.0f);
                    targetBallSettings.SetBounceFriction(0.85f);
                    break;
                    
                case BowlingLength.ShortLength:
                    targetBallSettings.SetBallSpeed(11f);
                    targetBallSettings.SetArcHeight(1.1f);
                    targetBallSettings.SetBounceForce(0.8f);
                    targetBallSettings.SetBounceFriction(0.75f);
                    break;
                    
                case BowlingLength.Bouncer:
                    targetBallSettings.SetBallSpeed(10f);
                    targetBallSettings.SetArcHeight(1.0f);
                    targetBallSettings.SetBounceForce(0.7f);
                    targetBallSettings.SetBounceFriction(0.7f);
                    break;
            }
            
            Debug.Log($"✅ Applied hardcoded {length} settings: Speed={targetBallSettings.BallSpeed}, Arc={targetBallSettings.ArcHeight}, Bounce={targetBallSettings.BounceForce}");
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
            
            // Ball will destroy itself after 5 seconds via BallAutoDestroy script
            Debug.Log("?? Ball has landed and bounced - it will destroy itself in 5 seconds");
            Debug.Log("?? Press S to create a new ball when ready!");
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
            
            // Calculate trajectory to target
            Vector3 targetPosition = target.position;
            Vector3 startPosition = ballToBowl.transform.position;
            
            // Calculate horizontal distance
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
            
            // ?? NEW: Read ball settings AFTER dynamic update
            float ballSpeed = ballSettings.BallSpeed;
            float arcHeight = ballSettings.ArcHeight;
            float gravity = ballSettings.Gravity;
            
            // Calculate time to reach target
            float timeToReach = horizontalDistance / ballSpeed;
            
            // Calculate realistic cricket bowling arc
            float heightDifference = targetPosition.y - startPosition.y;
            float realisticArcHeight = arcHeight * 0.2f; // Use your working arc height
            
            // Calculate required Y velocity for realistic arc
            float requiredYVelocity = (heightDifference + realisticArcHeight + 0.5f * gravity * timeToReach * timeToReach) / timeToReach;
            
            // ?? LENGTH-BASED TRAJECTORY TUNING
            // Higher length (towards batsman) -> lower bounce/arc; shorter (bouncer) -> higher arc
            float yScaleByLength = Mathf.Lerp(1.35f, 0.75f, currentLength01); // 0 -> +35% Y, 1 -> -25% Y
            requiredYVelocity *= yScaleByLength;
            
            // Cap Y velocity for realistic cricket arc
            float maxYVelocity = ballSpeed * 0.25f;
            if (requiredYVelocity > maxYVelocity)
            {
                requiredYVelocity = maxYVelocity;
                Debug.Log($"?? Y velocity capped to {maxYVelocity:F1} m/s for realistic cricket arc");
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
            
            // 🎯 CRITICAL FIX: Apply X rotation to Y velocity as well
            float rotationXRadians = spawnPoint != null ? spawnPoint.rotation.eulerAngles.x * Mathf.Deg2Rad : 0f;
            float rotationEffect = Mathf.Sin(rotationXRadians); // This gives us the downward component
            
            // 🎯 BOUNCER DEBUG: Check for extreme rotation values
            if (spawnPoint != null && spawnPoint.rotation.eulerAngles.x > 20f)
            {
                Debug.LogWarning($"🎯 EXTREME ROTATION DETECTED: X rotation = {spawnPoint.rotation.eulerAngles.x}° - This might cause ball to go straight down!");
                Debug.LogWarning($"🎯 Rotation effect = {rotationEffect:F3}, Ball speed = {ballSpeed}");
            }
            
            // Adjust Y velocity based on X rotation
            float adjustedYVelocity = requiredYVelocity + (rotationEffect * ballSpeed * 0.5f);
            
            Vector3 horizontalVelocity = horizontalDirection * ballSpeed;
            
            // 🎯 BOUNCER FIX: Ensure minimum horizontal velocity even with extreme rotations
            float minHorizontalSpeed = 2f; // Minimum horizontal speed to prevent ball from going straight down
            if (horizontalVelocity.magnitude < minHorizontalSpeed)
            {
                Debug.LogWarning($"🎯 BOUNCER FIX: Horizontal velocity too low ({horizontalVelocity.magnitude:F2}), applying minimum speed");
                horizontalVelocity = horizontalDirection * minHorizontalSpeed;
            }
            
            // Combine velocities with rotation-adjusted Y velocity
            Vector3 initialVelocity = horizontalVelocity;
            initialVelocity.y = adjustedYVelocity;
            
            Debug.Log($"🎯 ROTATION EFFECT: X rotation {spawnPoint?.rotation.eulerAngles.x ?? 0f}°, Rotation effect {rotationEffect:F3}, Y velocity {requiredYVelocity:F2} → {adjustedYVelocity:F2}");
            
            // Apply velocity to ball with smooth movement
            Debug.Log($"🎯 APPLYING VELOCITY: UseRealisticPhysics={ballSettings.UseRealisticPhysics}, useSmoothMovement={useSmoothMovement}");
            Debug.Log($"🎯 VELOCITY VALUES: initialVelocity={initialVelocity}, magnitude={initialVelocity.magnitude:F2}");
            Debug.Log($"🎯 RIGIDBODY STATUS: ballRigidbodyToUse={ballRigidbodyToUse != null}, isKinematic={ballRigidbodyToUse?.isKinematic}");
            
            if (ballSettings.UseRealisticPhysics)
            {
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
            else
            {
                Debug.Log("🎯 Using kinematic movement");
                // Use kinematic movement for precise control
                StartCoroutine(MoveBallKinematic(startPosition, targetPosition, timeToReach));
            }
            
            Debug.Log($"?? Ball launched with velocity: {initialVelocity.magnitude:F1} m/s");
            Debug.Log($"?? Expected time to target: {timeToReach:F2} seconds");
            Debug.Log($"<color=#00FF00>✅ FINAL BALL SETTINGS: Speed={ballSpeed}, Arc={arcHeight}, Gravity={gravity}</color>");
            Debug.Log($"<color=#FFD700>🎯 Using ball settings: Speed={ballSpeed}, Arc={arcHeight}, Bounce={ballSettings.BounceForce}, Gravity={gravity}</color>");
            
            // Wait for ball to reach target area
            yield return new WaitForSeconds(timeToReach);
            
            // Mark as landed
            hasLanded = true;
            Debug.Log("?? Ball has landed on target!");
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
            
            // ?? FIXED: Use realistic cricket bowling arc (much lower)
            float realisticArcHeight = arcHeight * 0.2f; // Same reduction as physics version
            
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
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // 🎯 FIXED: Use rotated trajectory direction
                Vector3 currentPos = startPos + trajectoryDirection * Vector3.Distance(startPos, endPos) * t;
                
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
                
                // ?? ENHANCED: Apply stronger bounce force for more visible bouncing
                float enhancedBounceForce = ballSettings.BounceForce;
                if (currentBounces == 1)
                {
                    enhancedBounceForce = ballSettings.BounceForce * 1.2f; // First bounce is stronger
                }
                else if (currentBounces == 2)
                {
                    enhancedBounceForce = ballSettings.BounceForce * 0.9f; // Second bounce slightly weaker
                }
                else
                {
                    enhancedBounceForce = ballSettings.BounceForce * 0.7f; // Third bounce weaker
                }

                // ?? LENGTH-BASED BOUNCE SCALING
                // Higher length (yorker, near batsman) => lower bounce; bouncer (mid/short) => higher bounce
                float lengthBounceScale = Mathf.Lerp(1.25f, 0.65f, currentLength01);
                // Prevent too-dead bounce; keep some minimum
                lengthBounceScale = Mathf.Clamp(lengthBounceScale, 0.6f, 1.35f);
                enhancedBounceForce *= lengthBounceScale;
                
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
