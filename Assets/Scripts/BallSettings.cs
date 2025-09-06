using UnityEngine;

namespace CricketGame
{
    /// <summary>
    /// Centralized ball settings - attach this to your BALL prefab
    /// All ball-related settings controlled from one place
    /// </summary>
    public class BallSettings : MonoBehaviour
    {
        [Header("Ball Physics")]
        [SerializeField] private float globalBallSpeed = 12f; // Single global speed for all bowling lengths
        [SerializeField] private float arcHeight = 1f;
        [SerializeField] private float gravity = 9.81f;
        [SerializeField] private float mass = 0.16f;
        [SerializeField] private float drag = 0.02f;
        [SerializeField] private float angularDrag = 0.02f;
        
        [Header("Bounce Physics")]
        [SerializeField] private float bounceForce = 0.8f;
        [SerializeField] private float bounceFriction = 0.85f;
        [SerializeField] private int maxBounces = 3;
        
        [Header("Ball Properties")]
        [SerializeField] private float ballRadius = 0.036f;
        [SerializeField] private bool useRealisticPhysics = true;
        
        [Header("Auto Destroy")]
        [SerializeField] private float destroyDelay = 5f;
        [SerializeField] private bool startTimerOnStart = true;
        
        [Header("🔴 Yorker Settings")]
        [SerializeField] private float yorkerArcHeight = 1.5f;
        [SerializeField] private float yorkerBounceForce = 1.2f;
        [SerializeField] private float yorkerBounceFriction = 0.9f;
        [SerializeField] private float yorkerRotationX = 0f; // X rotation for downward angle (pitch)
        
        [Header("🟡 Full Length Settings")]
        [SerializeField] private float fullLengthArcHeight = 1.2f;
        [SerializeField] private float fullLengthBounceForce = 0.9f;
        [SerializeField] private float fullLengthBounceFriction = 0.8f;
        [SerializeField] private float fullLengthRotationX = 1f; // X rotation for downward angle (pitch)
        
        [Header("🟢 Good Length Settings")]
        [SerializeField] private float goodLengthArcHeight = 1.5f;
        [SerializeField] private float goodLengthBounceForce = 0.7f;
        [SerializeField] private float goodLengthBounceFriction = 0.7f;
        [SerializeField] private float goodLengthRotationX = 5f; // X rotation for downward angle (pitch)
        
        [Header("🔵 Short Length Settings")]
        [SerializeField] private float shortLengthArcHeight = 2.0f;
        [SerializeField] private float shortLengthBounceForce = 0.5f;
        [SerializeField] private float shortLengthBounceFriction = 0.6f;
        [SerializeField] private float shortLengthRotationX = 10f; // X rotation for downward angle (pitch)
        
        [Header("🟣 Bouncer Settings")]
        [SerializeField] private float bouncerArcHeight = 1.0f;
        [SerializeField] private float bouncerBounceForce = 0.3f;
        [SerializeField] private float bouncerBounceFriction = 0.5f;
        [SerializeField] private float bouncerRotationX = 15f; // Reduced from 25f to 15f for less extreme angle
        
        [Header("🎯 Realistic Physics Bounce System")]
        [Header("Physics Parameters")]
        [SerializeField] private float baseRestitutionCoefficient = 0.6f; // Base bounce energy retention
        [SerializeField] private float randomVariation = 0.05f; // Random variation for realism
        
        [Header("Length-Specific Physics Multipliers")]
        [SerializeField] private float yorkerPhysicsMultiplier = 0.8f; // Yorker bounces less
        [SerializeField] private float fullLengthPhysicsMultiplier = 0.6f; // Full length bounces least
        [SerializeField] private float goodLengthPhysicsMultiplier = 1.0f; // Good length standard bounce
        [SerializeField] private float shortLengthPhysicsMultiplier = 1.2f; // Short length bounces more
        [SerializeField] private float bouncerPhysicsMultiplier = 1.5f; // Bouncer bounces most
        
        // Public properties for other scripts to access
        public float BallSpeed => globalBallSpeed; // Now uses global speed for all lengths
        public float ArcHeight => arcHeight;
        public float Gravity => gravity;
        public float Mass => mass;
        public float Drag => drag;
        public float AngularDrag => angularDrag;
        public float BounceForce => bounceForce;
        public float BounceFriction => bounceFriction;
        public int MaxBounces => maxBounces;
        public float BallRadius => ballRadius;
        public bool UseRealisticPhysics => useRealisticPhysics;
        public float DestroyDelay => destroyDelay;
        
        // Global speed control
        public float GlobalBallSpeed => globalBallSpeed;
        
        // Bowling length settings getters (speed removed, only arc, bounce, and rotation remain)
        public float YorkerArcHeight => yorkerArcHeight;
        public float YorkerBounceForce => yorkerBounceForce;
        public float YorkerBounceFriction => yorkerBounceFriction;
        public float YorkerRotationX => yorkerRotationX; // X rotation for downward angle (pitch)
        
        public float FullLengthArcHeight => fullLengthArcHeight;
        public float FullLengthBounceForce => fullLengthBounceForce;
        public float FullLengthBounceFriction => fullLengthBounceFriction;
        public float FullLengthRotationX => fullLengthRotationX; // X rotation for downward angle (pitch)
        
        public float GoodLengthArcHeight => goodLengthArcHeight;
        public float GoodLengthBounceForce => goodLengthBounceForce;
        public float GoodLengthBounceFriction => goodLengthBounceFriction;
        public float GoodLengthRotationX => goodLengthRotationX; // X rotation for downward angle (pitch)
        
        public float ShortLengthArcHeight => shortLengthArcHeight;
        public float ShortLengthBounceForce => shortLengthBounceForce;
        public float ShortLengthBounceFriction => shortLengthBounceFriction;
        public float ShortLengthRotationX => shortLengthRotationX; // X rotation for downward angle (pitch)
        
        public float BouncerArcHeight => bouncerArcHeight;
        public float BouncerBounceForce => bouncerBounceForce;
        public float BouncerBounceFriction => bouncerBounceFriction;
        public float BouncerRotationX => bouncerRotationX; // X rotation for downward angle (pitch)
        
        // Setter methods for dynamic adjustment
        public void SetBallSpeed(float newSpeed)
        {
            globalBallSpeed = Mathf.Clamp(newSpeed, 1f, 30f); // Increased max speed for more realistic cricket bowling
        }
        
        public void SetGlobalBallSpeed(float newSpeed)
        {
            globalBallSpeed = Mathf.Clamp(newSpeed, 1f, 30f);
        }
        
        /// <summary>
        /// 🎯 REALISTIC PHYSICS BOUNCE CALCULATION: Calculate bounce using real physics equations
        /// Based on kinetic energy, restitution coefficient, and bowling length characteristics
        /// </summary>
        public void CalculatePhysicsBounce(BowlingLength length)
        {
            // Get length-specific physics multiplier
            float lengthMultiplier = GetLengthPhysicsMultiplier(length);
            
            // Calculate kinetic energy (E = 0.5 * m * v²)
            float kineticEnergy = 0.5f * this.mass * globalBallSpeed * globalBallSpeed;
            
            // Calculate speed factor using square root for realistic physics (non-linear)
            float speedFactor = Mathf.Sqrt(globalBallSpeed / 20f); // Normalize to 0-1 range
            
            // Calculate restitution coefficient with length and speed factors
            float restitution = baseRestitutionCoefficient * lengthMultiplier * speedFactor;
            
            // Add random variation for realism
            float randomFactor = Random.Range(1f - randomVariation, 1f + randomVariation);
            restitution *= randomFactor;
            
            // Calculate bounce force based on energy conservation
            float bounceForce = restitution * (kineticEnergy / 10f); // Scale down for Unity physics
            
            // Calculate friction (higher speed = less friction due to reduced contact time)
            float friction = Mathf.Clamp(0.9f - (globalBallSpeed / 30f) * 0.4f, 0.3f, 0.9f);
            friction *= lengthMultiplier; // Apply length multiplier to friction too
            
            // Apply calculated values to appropriate length
            ApplyPhysicsValues(length, bounceForce, friction);
            
            Debug.Log($"🎯 PHYSICS BOUNCE {length}: Speed={globalBallSpeed:F1}m/s, Energy={kineticEnergy:F1}J, Restitution={restitution:F2}, Bounce={bounceForce:F2}, Friction={friction:F2}");
        }
        
        /// <summary>
        /// Get physics multiplier for specific bowling length
        /// </summary>
        private float GetLengthPhysicsMultiplier(BowlingLength length)
        {
            switch (length)
            {
                case BowlingLength.Yorker: return yorkerPhysicsMultiplier;
                case BowlingLength.FullLength: return fullLengthPhysicsMultiplier;
                case BowlingLength.GoodLength: return goodLengthPhysicsMultiplier;
                case BowlingLength.ShortLength: return shortLengthPhysicsMultiplier;
                case BowlingLength.Bouncer: return bouncerPhysicsMultiplier;
                default: return 1.0f;
            }
        }
        
        /// <summary>
        /// Apply calculated physics values to specific bowling length
        /// </summary>
        private void ApplyPhysicsValues(BowlingLength length, float bounceForce, float friction)
        {
            switch (length)
            {
                case BowlingLength.Yorker:
                    yorkerBounceForce = bounceForce;
                    yorkerBounceFriction = friction;
                    break;
                case BowlingLength.FullLength:
                    fullLengthBounceForce = bounceForce;
                    fullLengthBounceFriction = friction;
                    break;
                case BowlingLength.GoodLength:
                    goodLengthBounceForce = bounceForce;
                    goodLengthBounceFriction = friction;
                    break;
                case BowlingLength.ShortLength:
                    shortLengthBounceForce = bounceForce;
                    shortLengthBounceFriction = friction;
                    break;
                case BowlingLength.Bouncer:
                    bouncerBounceForce = bounceForce;
                    bouncerBounceFriction = friction;
                    break;
            }
        }
        
        public void SetArcHeight(float newHeight)
        {
            arcHeight = Mathf.Clamp(newHeight, 0.1f, 5f);
        }
        
        public void SetBounceForce(float newForce)
        {
            bounceForce = Mathf.Clamp(newForce, 0.1f, 2f);
        }
        
        public void SetBounceFriction(float newFriction)
        {
            bounceFriction = Mathf.Clamp(newFriction, 0.1f, 1f);
        }
        
        public void SetGravity(float newGravity)
        {
            gravity = Mathf.Clamp(newGravity, 1f, 20f);
        }
        
        public void SetMaxBounces(int newMaxBounces)
        {
            maxBounces = Mathf.Clamp(newMaxBounces, 1, 10);
        }
        
        public void SetUseRealisticPhysics(bool newUseRealisticPhysics)
        {
            useRealisticPhysics = newUseRealisticPhysics;
        }
        
        void Start()
        {
            if (startTimerOnStart)
            {
                StartDestroyTimer();
            }
        }
        
        /// <summary>
        /// Start the auto-destroy timer
        /// </summary>
        public void StartDestroyTimer()
        {
            Invoke(nameof(DestroyBall), destroyDelay);
        }
        
        /// <summary>
        /// Destroy this ball instance
        /// </summary>
        void DestroyBall()
        {
            Debug.Log($"🏏 Ball destroyed after {destroyDelay} seconds");
            Destroy(gameObject);
        }
        
        /// <summary>
        /// Reset timer (useful for manual control)
        /// </summary>
        public void ResetTimer()
        {
            CancelInvoke(nameof(DestroyBall));
            StartDestroyTimer();
        }
        
        /// <summary>
        /// Set custom destroy delay
        /// </summary>
        public void SetDestroyDelay(float newDelay)
        {
            destroyDelay = newDelay;
            ResetTimer();
        }
        
        /// <summary>
        /// Destroy ball immediately
        /// </summary>
        public void DestroyImmediately()
        {
            Destroy(gameObject);
        }
        
        // Context menu for testing
        [ContextMenu("Test Ball Settings")]
        void TestBallSettings()
        {
            Debug.Log($"🏏 Ball Settings Test:");
            Debug.Log($"   Global Speed: {globalBallSpeed} m/s");
            Debug.Log($"   Arc Height: {arcHeight} m");
            Debug.Log($"   Bounce Force: {bounceForce}");
            Debug.Log($"   Max Bounces: {maxBounces}");
            Debug.Log($"   Destroy Delay: {destroyDelay}s");
        }
        
        [ContextMenu("Destroy Ball Now")]
        void DestroyBallNow()
        {
            DestroyImmediately();
        }
    }
} 