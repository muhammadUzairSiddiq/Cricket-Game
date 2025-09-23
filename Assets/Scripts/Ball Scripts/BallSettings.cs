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
        [SerializeField] public float baseRestitutionCoefficient = 0.6f; // Base bounce energy retention
        [SerializeField] public float randomVariation = 0.05f; // Random variation for realism
        
        [Header("Speed Response Values (Simple Numbers)")]
        [Tooltip("Bounce multiplier for different speeds - higher = more bounce")]
        [SerializeField] public float speed9Bounce = 1.08f;
        [SerializeField] public float speed10Bounce = 1.10f;
        [SerializeField] public float speed11Bounce = 1.08f;
        [SerializeField] public float speed12Bounce = 1.06f;
        [SerializeField] public float speed13Bounce = 1.03f;
        [SerializeField] public float speed14Bounce = 1.00f;
        [SerializeField] public float speed15Bounce = 0.85f;
        [SerializeField] public float speed16Bounce = 0.80f;
        [SerializeField] public float speed18Bounce = 0.75f;
        [SerializeField] public float speed20Bounce = 0.72f;
        
        [Tooltip("Friction multiplier for different speeds - higher = more friction/damping")]
        [SerializeField] public float speed9Friction = 0.80f;
        [SerializeField] public float speed10Friction = 0.82f;
        [SerializeField] public float speed12Friction = 0.85f;
        [SerializeField] public float speed15Friction = 0.95f;
        [SerializeField] public float speed16Friction = 1.02f;
        [SerializeField] public float speed18Friction = 1.08f;
        [SerializeField] public float speed20Friction = 1.10f;
        
        [Header("Length-Specific Physics Multipliers")]
        [SerializeField] public float yorkerPhysicsMultiplier = 0.8f; // Yorker bounces less
        [SerializeField] public float fullLengthPhysicsMultiplier = 0.6f; // Full length bounces least
        [SerializeField] public float goodLengthPhysicsMultiplier = 1.0f; // Good length standard bounce
        [SerializeField] public float shortLengthPhysicsMultiplier = 1.2f; // Short length bounces more
        [SerializeField] public float bouncerPhysicsMultiplier = 1.5f; // Bouncer bounces most
        
        [Header("Length-Specific Physics Friction")]
        [SerializeField] public float yorkerPhysicsFriction = 0.9f; // Yorker friction
        [SerializeField] public float fullLengthPhysicsFriction = 0.8f; // Full length friction
        [SerializeField] public float goodLengthPhysicsFriction = 1.0f; // Good length friction
        [SerializeField] public float shortLengthPhysicsFriction = 1.1f; // Short length friction
        [SerializeField] public float bouncerPhysicsFriction = 1.2f; // Bouncer friction
        
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
            
            // Compute a normalized restitution coefficient (0.0 - 1.2) independent of v² to avoid extreme scaling
            float restitution = baseRestitutionCoefficient * lengthMultiplier;
            
            // Get bounce multiplier based on speed
            float bounceMultiplier = GetBounceMultiplier(globalBallSpeed);
            restitution *= bounceMultiplier;
            
            // Add subtle random variation
            restitution *= Random.Range(1f - randomVariation, 1f + randomVariation);
            restitution = Mathf.Clamp(restitution, 0.4f, 1.2f);
            
            // Use restitution directly as bounceForce (coefficient style), not scaled by kinetic energy
            float bounceForceCoef = restitution;
            
            // Friction coefficient shaped by speed and length (higher at high speeds to damp)
            float friction = GetFrictionMultiplier(globalBallSpeed);
            float lengthFriction = GetLengthPhysicsFriction(length);
            friction = Mathf.Clamp(friction * lengthFriction, 0.5f, 1.2f);
            
            // Apply calculated values to appropriate length
            ApplyPhysicsValues(length, bounceForceCoef, friction);
            
            Debug.Log($"🎯 PHYSICS BOUNCE {length}: Speed={globalBallSpeed:F1}m/s, Restitution={restitution:F2}, BounceCoef={bounceForceCoef:F2}, Friction={friction:F2}");
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
        /// Get physics friction for specific bowling length
        /// </summary>
        private float GetLengthPhysicsFriction(BowlingLength length)
        {
            switch (length)
            {
                case BowlingLength.Yorker: return yorkerPhysicsFriction;
                case BowlingLength.FullLength: return fullLengthPhysicsFriction;
                case BowlingLength.GoodLength: return goodLengthPhysicsFriction;
                case BowlingLength.ShortLength: return shortLengthPhysicsFriction;
                case BowlingLength.Bouncer: return bouncerPhysicsFriction;
                default: return 1.0f;
            }
        }
        
        /// <summary>
        /// Get bounce multiplier based on speed (simple linear interpolation)
        /// </summary>
        private float GetBounceMultiplier(float speed)
        {
            if (speed <= 9f) return speed9Bounce;
            if (speed <= 10f) return Mathf.Lerp(speed9Bounce, speed10Bounce, (speed - 9f) / 1f);
            if (speed <= 11f) return Mathf.Lerp(speed10Bounce, speed11Bounce, (speed - 10f) / 1f);
            if (speed <= 12f) return Mathf.Lerp(speed11Bounce, speed12Bounce, (speed - 11f) / 1f);
            if (speed <= 13f) return Mathf.Lerp(speed12Bounce, speed13Bounce, (speed - 12f) / 1f);
            if (speed <= 14f) return Mathf.Lerp(speed13Bounce, speed14Bounce, (speed - 13f) / 1f);
            if (speed <= 15f) return Mathf.Lerp(speed14Bounce, speed15Bounce, (speed - 14f) / 1f);
            if (speed <= 16f) return Mathf.Lerp(speed15Bounce, speed16Bounce, (speed - 15f) / 1f);
            if (speed <= 18f) return Mathf.Lerp(speed16Bounce, speed18Bounce, (speed - 16f) / 2f);
            if (speed <= 20f) return Mathf.Lerp(speed18Bounce, speed20Bounce, (speed - 18f) / 2f);
            return speed20Bounce;
        }
        
        /// <summary>
        /// Get friction multiplier based on speed (simple linear interpolation)
        /// </summary>
        private float GetFrictionMultiplier(float speed)
        {
            if (speed <= 9f) return speed9Friction;
            if (speed <= 10f) return Mathf.Lerp(speed9Friction, speed10Friction, (speed - 9f) / 1f);
            if (speed <= 12f) return Mathf.Lerp(speed10Friction, speed12Friction, (speed - 10f) / 2f);
            if (speed <= 15f) return Mathf.Lerp(speed12Friction, speed15Friction, (speed - 12f) / 3f);
            if (speed <= 16f) return Mathf.Lerp(speed15Friction, speed16Friction, (speed - 15f) / 1f);
            if (speed <= 18f) return Mathf.Lerp(speed16Friction, speed18Friction, (speed - 16f) / 2f);
            if (speed <= 20f) return Mathf.Lerp(speed18Friction, speed20Friction, (speed - 18f) / 2f);
            return speed20Friction;
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