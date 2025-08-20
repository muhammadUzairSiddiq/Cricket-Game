using System.Collections;
using UnityEngine;

namespace CricketGame
{
    /// <summary>
    /// Specialized Cricket Ball with realistic physics and behavior
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public class CricketBall : MonoBehaviour
    {
        [Header("Ball Properties")]
        [SerializeField] private float ballRadius = 0.036f; // Standard cricket ball radius
        [SerializeField] private float ballMass = 0.16f; // Standard cricket ball mass (kg)
        [SerializeField] private float ballBounciness = 0.8f; // How bouncy the ball is
        [SerializeField] private float ballFriction = 0.8f; // Ground friction
        
        [Header("PHYSICS SETTINGS - ADJUST THESE IN INSPECTOR")]
        [SerializeField] public float airResistance = 0.01f; // Air resistance (higher = more drag)
        [SerializeField] public float spinDecay = 0.95f; // How quickly spin decreases (0.95 = 5% loss per frame)
        [SerializeField] public float velocityDecay = 0.999f; // Velocity decay per frame (0.999 = 0.1% loss per frame)
        [SerializeField] public float maxBounceHeight = 8f; // Maximum bounce height allowed
        
        [Header("BOUNCE HEIGHT SETTINGS - ADJUST THESE IN INSPECTOR")]
        [SerializeField] public float pitchingAreaBounceHeight = 0.3f; // Pitching area bounce height (PERFECT CRICKET)
        [SerializeField] public float pitchingAreaBounceHeightFlat = 0.4f; // Bounce height for flat deliveries (PERFECT CRICKET)
        [SerializeField] public float groundBounceHeight = 0.15f; // Ground bounce height (PERFECT CRICKET)
        [SerializeField] public float groundBounceHeightFast = 0.25f; // Ground bounce height for fast balls (PERFECT CRICKET)
        
        [Header("BOUNCE FORCE SETTINGS - ADJUST THESE IN INSPECTOR")]
        [SerializeField] public float pitchingAreaImpulse = 0.2f; // Upward impulse for pitching area (PERFECT CRICKET)
        [SerializeField] public float groundImpulse = 0.1f; // Upward impulse for ground (PERFECT CRICKET)
        
        [Header("BOUNCE PHYSICS SETTINGS - ADJUST THESE IN INSPECTOR")]
        [SerializeField] public float bounceFactor = 0.3f; // Bounce factor for pitching area (0.3 = 30% bounce)
        [SerializeField] public float energyLoss = 0.2f; // Energy loss on bounce (0.2 = 20% loss)
        [SerializeField] public float momentumBoost = 1.0f; // Momentum boost (1.0 = no change)
        [SerializeField] public float groundBounceFactor = 0.2f; // Ground bounce factor (0.2 = 20% bounce)
        [SerializeField] public float groundEnergyLoss = 0.3f; // Ground energy loss (0.3 = 30% loss)
        
        [Header("BALL SPEED SETTINGS - ADJUST THESE IN INSPECTOR")]
        [SerializeField] public float minForwardSpeed = 35f; // Minimum forward speed after bounce (m/s)
        [SerializeField] public float speedBoostAfterPitch = 1.0f; // Speed multiplier after pitching (1.0 = no change)
        [SerializeField] public float initialBallSpeed = 40f; // Initial ball speed when bowled (m/s)
        
        [Header("ACCURACY DEBUG SETTINGS - ADJUST THESE IN INSPECTOR")]
        [SerializeField] public bool showAccuracyDebug = true; // Show accuracy debug info
        [SerializeField] public bool showTrajectoryDebug = false; // 🎯 DISABLED by default - no more yellow line!
        [SerializeField] public Color debugLineColor = new Color(1f, 1f, 1f, 0.2f); // 🎯 WHITE with 20% transparency (very subtle)
        [SerializeField] public float debugLineWidth = 0.01f; // 🎯 Thinner line for subtlety
        
        [Header("Visual Effects")]
        [SerializeField] private TrailRenderer ballTrail;
        [SerializeField] private ParticleSystem bounceEffect;
        [SerializeField] private Material newBallMaterial;
        [SerializeField] private Material oldBallMaterial;
        
        [Header("Ball Condition")]
        [SerializeField] private float ballAge = 0f; // Ball age in overs
        [SerializeField] private bool isNewBall = true;
        [SerializeField] private float roughness = 0f; // Ball roughness (0 = smooth, 1 = rough)
        
        // Private variables
        private Rigidbody rb;
        private Collider ballCollider;
        private Vector3 initialPosition;
        private bool hasBounced = false;
        private int bounceCount = 0;
        private float lastBounceTime = 0f;
        private float minBounceInterval = 0.1f; // Minimum time between bounces
        
        // ACCURACY DEBUG VARIABLES
        private Vector3 targetLandingPosition = Vector3.zero; // Where the ball SHOULD land
        private Vector3 actualLandingPosition = Vector3.zero; // Where the ball ACTUALLY landed
        private float landingAccuracy = 0f; // Distance between target and actual landing
        private bool hasLanded = false; // Has the ball landed on pitching area?
        private LineRenderer debugTrajectory; // Debug trajectory line
        
        // Events
        public System.Action<CricketBall> OnBallBounced;
        public System.Action<CricketBall> OnBallStopped;
        public System.Action<CricketBall> OnBallHitGround;
        
        void Awake()
        {
            InitializeBall();
        }
        
        void Start()
        {
            initialPosition = transform.position;
            GetTargetLandingPosition();
            SetupDebugTrajectory();
        }
        
        void Update()
        {
            UpdateBallPhysics();
            UpdateBallCondition();
            UpdateDebugTrajectory();
        }
        
        /// <summary>
        /// Initialize the cricket ball
        /// </summary>
        void InitializeBall()
        {
            // Get or add required components
            rb = GetComponent<Rigidbody>();
            ballCollider = GetComponent<Collider>();
            
            // Configure rigidbody
            rb.mass = ballMass;
            rb.linearDamping = airResistance;
            rb.angularDamping = 0.05f;
            rb.useGravity = true;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            
            // Configure collider
            if (ballCollider is SphereCollider sphereCollider)
            {
                sphereCollider.radius = ballRadius;
            }
            
            // Setup trail renderer
            SetupTrailRenderer();
            
            // Setup bounce effect
            SetupBounceEffect();
            
            // Setup materials
            SetupBallMaterials();
        }
        
        /// <summary>
        /// Setup ball trail renderer
        /// </summary>
        void SetupTrailRenderer()
        {
            if (ballTrail == null)
            {
                ballTrail = GetComponent<TrailRenderer>();
            }
            
            if (ballTrail != null)
            {
                ballTrail.time = 0.8f;
                ballTrail.startWidth = 0.08f;
                ballTrail.endWidth = 0.02f;
                ballTrail.material = new Material(Shader.Find("Sprites/Default"));
                ballTrail.startColor = new Color(1f, 1f, 1f, 0.3f); // 🎯 WHITE with 30% transparency
                ballTrail.endColor = new Color(1f, 1f, 1f, 0f); // 🎯 WHITE with 0% transparency (fade out)
                ballTrail.emitting = false;
            }
        }
        
        /// <summary>
        /// Setup bounce particle effect
        /// </summary>
        void SetupBounceEffect()
        {
            if (bounceEffect == null)
            {
                bounceEffect = GetComponent<ParticleSystem>();
            }
            
            if (bounceEffect != null)
            {
                var main = bounceEffect.main;
                main.startLifetime = 0.5f;
                main.startSpeed = 2f;
                main.startSize = 0.1f;
                main.maxParticles = 20;
                
                var emission = bounceEffect.emission;
                emission.rateOverTime = 0;
                emission.SetBursts(new ParticleSystem.Burst[]
                {
                    new ParticleSystem.Burst(0f, 15)
                });
            }
        }
        
        /// <summary>
        /// Setup debug trajectory line
        /// </summary>
        void SetupDebugTrajectory()
        {
            if (showTrajectoryDebug)
            {
                debugTrajectory = gameObject.AddComponent<LineRenderer>();
                debugTrajectory.material = new Material(Shader.Find("Sprites/Default"));
                debugTrajectory.startColor = debugLineColor;
                debugTrajectory.endColor = debugLineColor;
                debugTrajectory.startWidth = debugLineWidth;
                debugTrajectory.endWidth = debugLineWidth;
                debugTrajectory.positionCount = 2;
                debugTrajectory.enabled = false;
            }
        }
        
        /// <summary>
        /// Setup ball materials for new/old ball
        /// </summary>
        void SetupBallMaterials()
        {
            if (newBallMaterial != null && oldBallMaterial != null)
            {
                Renderer renderer = GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = isNewBall ? newBallMaterial : oldBallMaterial;
                }
            }
        }
        
        /// <summary>
        /// Update ball physics
        /// </summary>
        void UpdateBallPhysics()
        {
            if (rb == null) return;
            
            // Apply air resistance
            if (rb.linearVelocity.magnitude > 0.1f)
            {
                rb.AddForce(-rb.linearVelocity.normalized * airResistance * rb.linearVelocity.sqrMagnitude, ForceMode.Force);
            }
            
            // Decay spin over time
            if (rb.angularVelocity.magnitude > 0.01f)
            {
                rb.angularVelocity *= spinDecay;
            }
            
            // Decay velocity over time
            if (rb.linearVelocity.magnitude > 0.01f)
            {
                rb.linearVelocity *= velocityDecay;
            }
            
            // Check if ball has stopped
            if (rb.linearVelocity.magnitude < 0.1f && rb.angularVelocity.magnitude < 0.01f)
            {
                if (!hasBounced || bounceCount > 0)
                {
                    OnBallStopped?.Invoke(this);
                }
            }
            
            // Update trail emission
            if (ballTrail != null)
            {
                ballTrail.emitting = rb.linearVelocity.magnitude > 0.5f;
            }
        }
        
        /// <summary>
        /// Update debug trajectory visualization
        /// </summary>
        void UpdateDebugTrajectory()
        {
            if (showTrajectoryDebug && debugTrajectory != null && rb != null && rb.linearVelocity.magnitude > 0.1f)
            {
                // Show trajectory from current position to where ball will land
                Vector3 currentPos = transform.position;
                Vector3 velocity = rb.linearVelocity;
                
                // Calculate landing position using projectile motion
                float timeToLand = Mathf.Abs(velocity.y) / 9.81f; // Time to reach ground
                Vector3 landingPos = currentPos + velocity * timeToLand;
                landingPos.y = currentPos.y + velocity.y * timeToLand - 0.5f * 9.81f * timeToLand * timeToLand;
                
                // Update debug line
                debugTrajectory.enabled = true;
                debugTrajectory.SetPosition(0, currentPos);
                debugTrajectory.SetPosition(1, landingPos);
                
                // Show accuracy if we have a target
                if (targetLandingPosition != Vector3.zero)
                {
                    float accuracy = Vector3.Distance(landingPos, targetLandingPosition);
                    if (showAccuracyDebug)
                    {
                        Debug.Log($"🎯 TRAJECTORY ACCURACY: {accuracy:F2}m (Target: {targetLandingPosition}, Predicted: {landingPos})");
                    }
                }
            }
            else if (debugTrajectory != null)
            {
                debugTrajectory.enabled = false;
            }
        }
        
        /// <summary>
        /// Update ball condition based on age
        /// </summary>
        void UpdateBallCondition()
        {
            // Ball gets rougher over time
            if (isNewBall && ballAge > 5f) // After 5 overs
            {
                isNewBall = false;
                roughness = Mathf.Min(roughness + 0.1f, 1f);
                SetupBallMaterials();
            }
            
            // Ball condition affects physics
            if (roughness > 0.5f)
            {
                // Rough ball has more unpredictable movement
                if (Random.Range(0f, 1f) < 0.01f) // 1% chance per frame
                {
                    ApplyRandomMovement();
                }
            }
        }
        
        /// <summary>
        /// Apply random movement for rough ball
        /// </summary>
        void ApplyRandomMovement()
        {
            if (rb != null && rb.linearVelocity.magnitude > 1f)
            {
                Vector3 randomForce = Random.insideUnitSphere * 0.5f;
                rb.AddForce(randomForce, ForceMode.Impulse);
            }
        }
        
        /// <summary>
        /// Get the target landing position from CricketBowlingSystem
        /// </summary>
        void GetTargetLandingPosition()
        {
            CricketBowlingSystem bowlingSystem = FindObjectOfType<CricketBowlingSystem>();
            if (bowlingSystem != null)
            {
                targetLandingPosition = bowlingSystem.GetCurrentTargetPosition();
                if (showAccuracyDebug)
                {
                    Debug.Log($"🎯 BALL TARGET: {targetLandingPosition}");
                }
            }
        }
        
        /// <summary>
        /// Hide the pitch prediction when ball hits pitching area
        /// </summary>
        void HidePitchPrediction()
        {
            // Find the CricketBowlingSystem and hide prediction
            CricketBowlingSystem bowlingSystem = FindObjectOfType<CricketBowlingSystem>();
            if (bowlingSystem != null)
            {
                bowlingSystem.HidePrediction();
            }
        }
        
        /// <summary>
        /// Handle collision with different surfaces
        /// </summary>
        void OnCollisionEnter(Collision collision)
        {
            // Handle collision with different surfaces
            if (collision.gameObject.CompareTag("PitchingArea"))
            {
                HandlePitchingAreaCollision(collision);
            }
            else if (collision.gameObject.CompareTag("Ground"))
            {
                HandleGroundCollision(collision);
            }
            else if (collision.gameObject.CompareTag("Wicket"))
            {
                HandleWicketCollision(collision);
            }
        }

        /// <summary>
        /// Handle collision with pitching area - MAINTAIN CALCULATED TRAJECTORY
        /// </summary>
        void HandlePitchingAreaCollision(Collision collision)
        {
            // Get the contact point
            ContactPoint contact = collision.contacts[0];
            
            // 🎯 RECORD ACTUAL LANDING POSITION FOR ACCURACY CHECK
            actualLandingPosition = contact.point;
            hasLanded = true;
            
            // Calculate accuracy if we have a target
            if (targetLandingPosition != Vector3.zero)
            {
                landingAccuracy = Vector3.Distance(actualLandingPosition, targetLandingPosition);
                if (showAccuracyDebug)
                {
                    Debug.Log($"🎯 LANDING ACCURACY: {landingAccuracy:F2}m");
                    Debug.Log($"   Target: {targetLandingPosition}");
                    Debug.Log($"   Actual: {actualLandingPosition}");
                    
                    if (landingAccuracy < 0.5f)
                        Debug.Log("   ✅ EXCELLENT ACCURACY!");
                    else if (landingAccuracy < 1.0f)
                        Debug.Log("   ✅ GOOD ACCURACY");
                    else if (landingAccuracy < 2.0f)
                        Debug.Log("   ⚠️ ACCEPTABLE ACCURACY");
                    else
                        Debug.Log("   ❌ POOR ACCURACY - NEEDS IMPROVEMENT");
                }
            }
            
            // 🎯 MAINTAIN CALCULATED TRAJECTORY - Don't let bounce physics interfere!
            
            // Get current velocity and direction
            Vector3 incomingVelocity = rb.linearVelocity;
            Vector3 normal = contact.normal;
            
            // Calculate current speed and direction
            float incomingSpeed = incomingVelocity.magnitude;
            Vector3 forwardDirection = new Vector3(incomingVelocity.x, 0, incomingVelocity.z).normalized;
            
            // 🎯 CRITICAL: Use MINIMAL bounce to maintain trajectory accuracy
            // The ball should bounce but not deviate significantly from its calculated path
            
            // Calculate new velocity with MINIMAL energy loss to maintain accuracy
            float baseSpeed = incomingSpeed * (1f - this.energyLoss) * this.momentumBoost;
            
            // Ensure minimum forward speed to reach wickets
            if (baseSpeed < this.minForwardSpeed)
            {
                baseSpeed = this.minForwardSpeed;
                Debug.Log($"🎯 BOOSTED ball speed to {this.minForwardSpeed} m/s to maintain trajectory accuracy!");
            }
            
            Vector3 newVelocity = forwardDirection * baseSpeed;
            
            // 🎯 PERFECT CRICKET BOUNCE - Low and controlled
            // Use inspector values for easy adjustment
            newVelocity.y = pitchingAreaBounceHeight; // Use inspector value
            
            // If ball is coming in too flat, use flat delivery bounce height
            if (Mathf.Abs(incomingVelocity.y) < 3.0f)
            {
                newVelocity.y = pitchingAreaBounceHeightFlat; // Use inspector value
            }
            
            Debug.Log($"🎯 TRAJECTORY MAINTENANCE: Speed after pitch: {baseSpeed:F1} → {newVelocity.magnitude:F1} m/s");
            Debug.Log($"🎯 Bounce height: {newVelocity.y:F2}m (incoming Y: {incomingVelocity.y:F2}m)");
            
            // 🎯 APPLY NEW VELOCITY IMMEDIATELY to maintain trajectory
            rb.linearVelocity = newVelocity;
            
            // Add minimal upward impulse for realistic bounce (use inspector value)
            rb.AddForce(Vector3.up * pitchingAreaImpulse, ForceMode.Impulse);
            
            // Update ball condition (gets rougher with bounces)
            UpdateBallCondition();
            
            // Hide the pitch prediction when ball hits pitching area
            HidePitchPrediction();
            
            // 🎯 Notify CricketBowlingSystem that ball has landed
            CricketBowlingSystem bowlingSystem = FindObjectOfType<CricketBowlingSystem>();
            if (bowlingSystem != null)
            {
                bowlingSystem.SetBallLanded();
            }
            
            // Play bounce effect
            if (bounceEffect != null)
            {
                bounceEffect.transform.position = contact.point;
                bounceEffect.Play();
            }
            
            Debug.Log($"🎯 Ball hit PITCHING AREA! Trajectory maintained with bounce height: {newVelocity.y:F2}m");
        }
        
        /// <summary>
        /// Handle collision with ground - MINIMAL TRAJECTORY INTERFERENCE
        /// </summary>
        void HandleGroundCollision(Collision collision)
        {
            if (Time.time - lastBounceTime < minBounceInterval) return;
            
            lastBounceTime = Time.time;
            bounceCount++;
            
            // 🎯 MINIMAL GROUND BOUNCE - Don't interfere with trajectory accuracy
            
            // Get current velocity and direction
            Vector3 velocity = rb.linearVelocity;
            Vector3 forwardDirection = new Vector3(velocity.x, 0, velocity.z).normalized;
            float forwardSpeed = new Vector3(velocity.x, 0, velocity.z).magnitude;
            
            // 🎯 CRITICAL: Use MINIMAL bounce to maintain trajectory accuracy
            // The ball should bounce but not deviate significantly from its calculated path
            
            // Calculate new velocity with MINIMAL energy loss to maintain accuracy
            float newForwardSpeed = forwardSpeed * (1f - this.groundEnergyLoss);
            
            // Ensure minimum forward speed to reach wickets
            if (newForwardSpeed < this.minForwardSpeed * 0.8f) // Allow some reduction but not too much
            {
                newForwardSpeed = this.minForwardSpeed * 0.8f;
                Debug.Log($"🎯 GROUND BOUNCE: Maintained forward speed at {newForwardSpeed:F1} m/s for trajectory accuracy!");
            }
            
            // Create new velocity with perfect bounce
            Vector3 newVelocity = forwardDirection * newForwardSpeed;
            
            // 🎯 PERFECT GROUND BOUNCE - Low and controlled
            // Use inspector values for easy adjustment
            newVelocity.y = groundBounceHeight; // Use inspector value
            
            // If ball is moving fast, use fast ball bounce height
            if (newForwardSpeed > 15f)
            {
                newVelocity.y = groundBounceHeightFast; // Use inspector value
            }
            
            Debug.Log($"🎯 GROUND BOUNCE: Speed maintained at {newForwardSpeed:F1} m/s, bounce height: {newVelocity.y:F2}m");
            
            // 🎯 APPLY NEW VELOCITY IMMEDIATELY to maintain trajectory
            rb.linearVelocity = newVelocity;
            
            // Add minimal upward impulse for realistic bounce (use inspector value)
            rb.AddForce(Vector3.up * groundImpulse, ForceMode.Impulse);
            
            // Reduce angular velocity on bounce to maintain stability
            rb.angularVelocity *= 0.8f;
            
            // Trigger bounce effect
            if (bounceEffect != null)
            {
                bounceEffect.Play();
            }
            
            // Trigger events
            OnBallBounced?.Invoke(this);
            OnBallHitGround?.Invoke(this);
            
            hasBounced = true;
            
            // Ball gets rougher with each bounce
            roughness = Mathf.Min(roughness + 0.05f, 1f);
            
            Debug.Log($"🎯 Ball hit GROUND! Trajectory maintained with bounce height: {newVelocity.y:F2}m");
        }
        
        /// <summary>
        /// Handle collision with wicket
        /// </summary>
        void HandleWicketCollision(Collision collision)
        {
            Debug.Log("Ball hit the wicket!");
            // You might want to trigger a wicket event here
            // OnBallHitWicket?.Invoke(this);
        }
        
        /// <summary>
        /// Reset ball to initial state
        /// </summary>
        public void ResetBall()
        {
            transform.position = initialPosition;
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            
            hasBounced = false;
            bounceCount = 0;
            lastBounceTime = 0f;
            
            if (ballTrail != null)
            {
                ballTrail.Clear();
            }
        }
        
        /// <summary>
        /// Set ball age (affects condition)
        /// </summary>
        public void SetBallAge(float age)
        {
            ballAge = age;
            UpdateBallCondition();
        }
        
        /// <summary>
        /// Get ball condition as string
        /// </summary>
        public string GetBallCondition()
        {
            if (isNewBall)
                return "New Ball";
            else if (roughness < 0.3f)
                return "Good Condition";
            else if (roughness < 0.6f)
                return "Worn";
            else
                return "Rough";
        }
        
        /// <summary>
        /// Apply spin to the ball
        /// </summary>
        public void ApplySpin(Vector3 spinAxis, float spinRate)
        {
            if (rb != null)
            {
                rb.angularVelocity = spinAxis * spinRate;
            }
        }
        
        /// <summary>
        /// Apply force to the ball
        /// </summary>
        public void ApplyForce(Vector3 force, ForceMode forceMode = ForceMode.Impulse)
        {
            if (rb != null)
            {
                rb.AddForce(force, forceMode);
            }
        }
        
        /// <summary>
        /// Get current ball velocity
        /// </summary>
        public Vector3 GetVelocity()
        {
            return rb != null ? rb.linearVelocity : Vector3.zero;
        }
        
        /// <summary>
        /// Get current ball angular velocity
        /// </summary>
        public Vector3 GetAngularVelocity()
        {
            return rb != null ? rb.angularVelocity : Vector3.zero;
        }
        
        /// <summary>
        /// Check if ball is moving
        /// </summary>
        public bool IsMoving()
        {
            return rb != null && (rb.linearVelocity.magnitude > 0.1f || rb.angularVelocity.magnitude > 0.01f);
        }
        
        /// <summary>
        /// Get bounce count
        /// </summary>
        public int GetBounceCount()
        {
            return bounceCount;
        }
        
        /// <summary>
        /// Get ball roughness
        /// </summary>
        public float GetRoughness()
        {
            return roughness;
        }
        
        /// <summary>
        /// Context menu function to reset ball
        /// </summary>
        [ContextMenu("Reset Ball")]
        void ResetBallContext()
        {
            ResetBall();
        }
        
        /// <summary>
        /// Context menu function to apply test spin
        /// </summary>
        [ContextMenu("Apply Test Spin")]
        void ApplyTestSpinContext()
        {
            ApplySpin(Vector3.up, 10f);
        }
        
        /// <summary>
        /// Context menu function to apply test force
        /// </summary>
        [ContextMenu("Apply Test Force")]
        void ApplyTestForceContext()
        {
            ApplyForce(Vector3.forward * 10f, ForceMode.Impulse);
        }
        
        /// <summary>
        /// Context menu function to test accuracy system
        /// </summary>
        [ContextMenu("Test Accuracy System")]
        void TestAccuracySystemContext()
        {
            if (showAccuracyDebug)
            {
                Debug.Log($"🎯 BALL ACCURACY DEBUG:");
                Debug.Log($"   Target Landing Position: {targetLandingPosition}");
                Debug.Log($"   Actual Landing Position: {actualLandingPosition}");
                Debug.Log($"   Landing Accuracy: {landingAccuracy:F3}m");
                Debug.Log($"   Has Landed: {hasLanded}");
                Debug.Log($"   Bounce Count: {bounceCount}");
                
                if (rb != null)
                {
                    Debug.Log($"   Current Velocity: {rb.linearVelocity}");
                    Debug.Log($"   Current Speed: {rb.linearVelocity.magnitude:F2} m/s");
                }
                
                if (targetLandingPosition != Vector3.zero && actualLandingPosition != Vector3.zero)
                {
                    Vector3 difference = targetLandingPosition - actualLandingPosition;
                    Debug.Log($"   Position Difference: {difference}");
                    Debug.Log($"   X Difference: {difference.x:F3}m");
                    Debug.Log($"   Z Difference: {difference.z:F3}m");
                    Debug.Log($"   Y Difference: {difference.y:F3}m");
                }
            }
            else
            {
                Debug.Log("Enable showAccuracyDebug in Inspector to see accuracy information");
            }
        }
    }
}
