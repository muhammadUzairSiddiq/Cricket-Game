using UnityEngine;

namespace CricketGame
{
    /// <summary>
    /// Centralized ball settings - attach this to your BALL prefab
    /// All ball-related settings controlled from one place
    /// </summary>
    public class BallSettings : MonoBehaviour
    {
        [Header("Ball Physics (Managed by BallSettingsSO)")]
        [SerializeField, HideInInspector] private float globalBallSpeed = 12f; // Managed via ScriptableObject/SpeedController
        [SerializeField, HideInInspector] private float arcHeight = 1f;        // Applied at runtime by controller
        [SerializeField, HideInInspector] private float gravity = 9.81f;       // Length-specific gravity now in SO
        [SerializeField, HideInInspector] private float mass = 0.16f;
        [SerializeField, HideInInspector] private float drag = 0.02f;
        [SerializeField, HideInInspector] private float angularDrag = 0.02f;
        
        [Header("Bounce Physics (Managed by BallSettingsSO)")]
        [SerializeField, HideInInspector] private float bounceForce = 0.8f;     // Applied at runtime by controller
        [SerializeField, HideInInspector] private float bounceFriction = 0.85f; // Applied at runtime by controller
        [SerializeField, HideInInspector] private int maxBounces = 3;
        
        [Header("Ball Properties (Runtime Only)")]
        [SerializeField, HideInInspector] private float ballRadius = 0.036f;
        [SerializeField, HideInInspector] private bool useRealisticPhysics = true;
        
        [Header("Auto Destroy")]
        [SerializeField, HideInInspector] private float destroyDelay = 5f;
        [SerializeField, HideInInspector] private bool startTimerOnStart = true;
        
        
        
        // Public properties for other scripts to access
        public float BallSpeed => Mathf.Round(globalBallSpeed); // Always return rounded integer
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
        public float GlobalBallSpeed => Mathf.Round(globalBallSpeed); // Always return rounded integer
        
        
        // Setter methods for dynamic adjustment
        public void SetBallSpeed(float newSpeed)
        {
            // Round to nearest integer and clamp
            float roundedSpeed = Mathf.Round(newSpeed);
            globalBallSpeed = Mathf.Clamp(roundedSpeed, 12f, 16f); // Clamp to valid speed range
        }
        
        public void SetGlobalBallSpeed(float newSpeed)
        {
            // Round to nearest integer and clamp
            float roundedSpeed = Mathf.Round(newSpeed);
            globalBallSpeed = Mathf.Clamp(roundedSpeed, 12f, 16f); // Clamp to valid speed range
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