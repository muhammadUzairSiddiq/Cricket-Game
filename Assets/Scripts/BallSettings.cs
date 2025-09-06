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
        [SerializeField] private float ballSpeed = 10f;
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
        [SerializeField] private float yorkerSpeed = 15f;
        [SerializeField] private float yorkerArcHeight = 1.5f;
        [SerializeField] private float yorkerBounceForce = 1.2f;
        [SerializeField] private float yorkerBounceFriction = 0.9f;
        [SerializeField] private float yorkerRotationX = 0f; // X rotation for downward angle (pitch)
        
        [Header("🟡 Full Length Settings")]
        [SerializeField] private float fullLengthSpeed = 12f;
        [SerializeField] private float fullLengthArcHeight = 1.2f;
        [SerializeField] private float fullLengthBounceForce = 0.9f;
        [SerializeField] private float fullLengthBounceFriction = 0.8f;
        [SerializeField] private float fullLengthRotationX = 1f; // X rotation for downward angle (pitch)
        
        [Header("🟢 Good Length Settings")]
        [SerializeField] private float goodLengthSpeed = 10f;
        [SerializeField] private float goodLengthArcHeight = 1.5f;
        [SerializeField] private float goodLengthBounceForce = 0.7f;
        [SerializeField] private float goodLengthBounceFriction = 0.7f;
        [SerializeField] private float goodLengthRotationX = 5f; // X rotation for downward angle (pitch)
        
        [Header("🔵 Short Length Settings")]
        [SerializeField] private float shortLengthSpeed = 8f;
        [SerializeField] private float shortLengthArcHeight = 2.0f;
        [SerializeField] private float shortLengthBounceForce = 0.5f;
        [SerializeField] private float shortLengthBounceFriction = 0.6f;
        [SerializeField] private float shortLengthRotationX = 10f; // X rotation for downward angle (pitch)
        
        [Header("🟣 Bouncer Settings")]
        [SerializeField] private float bouncerSpeed = 10f; // Increased from 6f to 10f for better movement
        [SerializeField] private float bouncerArcHeight = 1.0f;
        [SerializeField] private float bouncerBounceForce = 0.3f;
        [SerializeField] private float bouncerBounceFriction = 0.5f;
        [SerializeField] private float bouncerRotationX = 15f; // Reduced from 25f to 15f for less extreme angle
        
        // Public properties for other scripts to access
        public float BallSpeed => ballSpeed;
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
        
        // Bowling length settings getters
        public float YorkerSpeed => yorkerSpeed;
        public float YorkerArcHeight => yorkerArcHeight;
        public float YorkerBounceForce => yorkerBounceForce;
        public float YorkerBounceFriction => yorkerBounceFriction;
        public float YorkerRotationX => yorkerRotationX; // X rotation for downward angle (pitch)
        
        public float FullLengthSpeed => fullLengthSpeed;
        public float FullLengthArcHeight => fullLengthArcHeight;
        public float FullLengthBounceForce => fullLengthBounceForce;
        public float FullLengthBounceFriction => fullLengthBounceFriction;
        public float FullLengthRotationX => fullLengthRotationX; // X rotation for downward angle (pitch)
        
        public float GoodLengthSpeed => goodLengthSpeed;
        public float GoodLengthArcHeight => goodLengthArcHeight;
        public float GoodLengthBounceForce => goodLengthBounceForce;
        public float GoodLengthBounceFriction => goodLengthBounceFriction;
        public float GoodLengthRotationX => goodLengthRotationX; // X rotation for downward angle (pitch)
        
        public float ShortLengthSpeed => shortLengthSpeed;
        public float ShortLengthArcHeight => shortLengthArcHeight;
        public float ShortLengthBounceForce => shortLengthBounceForce;
        public float ShortLengthBounceFriction => shortLengthBounceFriction;
        public float ShortLengthRotationX => shortLengthRotationX; // X rotation for downward angle (pitch)
        
        public float BouncerSpeed => bouncerSpeed;
        public float BouncerArcHeight => bouncerArcHeight;
        public float BouncerBounceForce => bouncerBounceForce;
        public float BouncerBounceFriction => bouncerBounceFriction;
        public float BouncerRotationX => bouncerRotationX; // X rotation for downward angle (pitch)
        
        // Setter methods for dynamic adjustment
        public void SetBallSpeed(float newSpeed)
        {
            ballSpeed = Mathf.Clamp(newSpeed, 1f, 20f);
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
            Debug.Log($"   Speed: {ballSpeed} m/s");
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