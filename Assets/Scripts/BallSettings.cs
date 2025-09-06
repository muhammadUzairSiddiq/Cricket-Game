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
        
        [Header("🎯 Dynamic Bounce System - Starting/Ending Ranges")]
        [Header("🔴 Yorker Dynamic Bounce")]
        [SerializeField] public float yorkerBounceForceStart = 0.4f;
        [SerializeField] public float yorkerBounceForceEnd = 0.5f;
        [SerializeField] public float yorkerBounceFrictionStart = 0.9f;
        [SerializeField] public float yorkerBounceFrictionEnd = 0.95f;
        
        [Header("🟡 Full Length Dynamic Bounce")]
        [SerializeField] public float fullLengthBounceForceStart = 0.25f;
        [SerializeField] public float fullLengthBounceForceEnd = 0.35f;
        [SerializeField] public float fullLengthBounceFrictionStart = 0.75f;
        [SerializeField] public float fullLengthBounceFrictionEnd = 0.85f;
        
        [Header("🟢 Good Length Dynamic Bounce")]
        [SerializeField] public float goodLengthBounceForceStart = 0.7f;
        [SerializeField] public float goodLengthBounceForceEnd = 0.8f;
        [SerializeField] public float goodLengthBounceFrictionStart = 0.8f;
        [SerializeField] public float goodLengthBounceFrictionEnd = 0.9f;
        
        [Header("🔵 Short Length Dynamic Bounce")]
        [SerializeField] public float shortLengthBounceForceStart = 0.6f;
        [SerializeField] public float shortLengthBounceForceEnd = 0.7f;
        [SerializeField] public float shortLengthBounceFrictionStart = 0.7f;
        [SerializeField] public float shortLengthBounceFrictionEnd = 0.8f;
        
        [Header("🟣 Bouncer Dynamic Bounce")]
        [SerializeField] public float bouncerBounceForceStart = 0.2f;
        [SerializeField] public float bouncerBounceForceEnd = 0.4f;
        [SerializeField] public float bouncerBounceFrictionStart = 0.4f;
        [SerializeField] public float bouncerBounceFrictionEnd = 0.6f;
        
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
        /// 🎯 INTELLIGENT BOUNCE CALCULATION: Calculate dynamic bounce values based on speed and bowling length
        /// Higher speed = higher bounce, Lower speed = lower bounce
        /// </summary>
        public void CalculateDynamicBounce(BowlingLength length)
        {
            float speedFactor = Mathf.Clamp01((globalBallSpeed - 5f) / 15f); // Normalize speed (5-20 m/s) to 0-1
            
            switch (length)
            {
                case BowlingLength.Yorker:
                    yorkerBounceForce = Mathf.Lerp(yorkerBounceForceStart, yorkerBounceForceEnd, speedFactor);
                    yorkerBounceFriction = Mathf.Lerp(yorkerBounceFrictionStart, yorkerBounceFrictionEnd, speedFactor);
                    Debug.Log($"🎯 YORKER Dynamic Bounce: Speed={globalBallSpeed:F1}m/s, Factor={speedFactor:F2}, Bounce={yorkerBounceForce:F2}, Friction={yorkerBounceFriction:F2}");
                    break;
                    
                case BowlingLength.FullLength:
                    fullLengthBounceForce = Mathf.Lerp(fullLengthBounceForceStart, fullLengthBounceForceEnd, speedFactor);
                    fullLengthBounceFriction = Mathf.Lerp(fullLengthBounceFrictionStart, fullLengthBounceFrictionEnd, speedFactor);
                    Debug.Log($"🎯 FULL LENGTH Dynamic Bounce: Speed={globalBallSpeed:F1}m/s, Factor={speedFactor:F2}, Bounce={fullLengthBounceForce:F2}, Friction={fullLengthBounceFriction:F2}");
                    break;
                    
                case BowlingLength.GoodLength:
                    goodLengthBounceForce = Mathf.Lerp(goodLengthBounceForceStart, goodLengthBounceForceEnd, speedFactor);
                    goodLengthBounceFriction = Mathf.Lerp(goodLengthBounceFrictionStart, goodLengthBounceFrictionEnd, speedFactor);
                    Debug.Log($"🎯 GOOD LENGTH Dynamic Bounce: Speed={globalBallSpeed:F1}m/s, Factor={speedFactor:F2}, Bounce={goodLengthBounceForce:F2}, Friction={goodLengthBounceFriction:F2}");
                    break;
                    
                case BowlingLength.ShortLength:
                    shortLengthBounceForce = Mathf.Lerp(shortLengthBounceForceStart, shortLengthBounceForceEnd, speedFactor);
                    shortLengthBounceFriction = Mathf.Lerp(shortLengthBounceFrictionStart, shortLengthBounceFrictionEnd, speedFactor);
                    Debug.Log($"🎯 SHORT LENGTH Dynamic Bounce: Speed={globalBallSpeed:F1}m/s, Factor={speedFactor:F2}, Bounce={shortLengthBounceForce:F2}, Friction={shortLengthBounceFriction:F2}");
                    break;
                    
                case BowlingLength.Bouncer:
                    bouncerBounceForce = Mathf.Lerp(bouncerBounceForceStart, bouncerBounceForceEnd, speedFactor);
                    bouncerBounceFriction = Mathf.Lerp(bouncerBounceFrictionStart, bouncerBounceFrictionEnd, speedFactor);
                    Debug.Log($"🎯 BOUNCER Dynamic Bounce: Speed={globalBallSpeed:F1}m/s, Factor={speedFactor:F2}, Bounce={bouncerBounceForce:F2}, Friction={bouncerBounceFriction:F2}");
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