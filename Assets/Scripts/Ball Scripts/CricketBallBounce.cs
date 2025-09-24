using UnityEngine;

namespace CricketGame
{
    /// <summary>
    /// Cricket Ball Bounce Component
    /// Detects when the ball hits the target and triggers realistic bounce physics
    /// </summary>
    public class CricketBallBounce : MonoBehaviour
    {
        [Header("Bounce Detection")]
        [SerializeField] private LayerMask targetLayerMask = -1; // Default to all layers
        [SerializeField] private float bounceDetectionRadius = 0.5f; // 🎯 WORKING: Optimal detection radius for target landing
        [SerializeField] private float minBounceVelocity = 0.2f; // 🎯 WORKING: Good minimum velocity for reliable detection
        [SerializeField] private float heightTolerance = 0.2f; // 🎯 WORKING: Good height tolerance for target landing
        
        [Header("Debug")]
        [SerializeField] private bool showBounceDebug = true;
        
        // Private variables
        private BowlingController bowlingSystem;
        private Rigidbody ballRigidbody;
        private bool hasBounced = false;
        private Vector3 lastPosition;
        private float lastBounceTime;
        private float bounceCooldown = 0.05f; // 🎯 WORKING: Good cooldown for multiple bounces
        
        void Start()
        {
            ballRigidbody = GetComponent<Rigidbody>();
            lastPosition = transform.position;
        }
        
        void Update()
        {
            if (bowlingSystem == null || ballRigidbody == null) return;
            
            // 🎯 SIMPLIFIED: Single, reliable bounce detection
            CheckForBounce();
            
            // Update last position
            lastPosition = transform.position;
        }
        
        /// <summary>
        /// Initialize the bounce component with reference to the bowling system
        /// </summary>
        public void Initialize(BowlingController system)
        {
            bowlingSystem = system;
            // Cricket Ball Bounce component initialized
        }
        
        /// <summary>
        /// 🎯 WORKING: Simple and reliable bounce detection
        /// </summary>
        void CheckForBounce()
        {
            if (hasBounced) return;
            
            // 🎯 SIMPLE: Check if ball is moving downward with sufficient velocity
            if (ballRigidbody.linearVelocity.y < -minBounceVelocity || ballRigidbody.isKinematic == false && ballRigidbody.linearVelocity.magnitude > 0.1f)
            {
                // 🎯 SIMPLE: Check if ball is near the target
                if (IsNearTarget())
                {
                    // 🎯 SIMPLE: Check if ball is close to ground level (target height)
                    if (IsNearGroundLevel())
                    {
                        // 🎯 SIMPLE: Check if ball has enough total velocity for visible bounce
                        float totalVelocity = ballRigidbody.linearVelocity.magnitude;
                        if (totalVelocity > 0.5f) // Good minimum velocity for bounce
                        {
                            TriggerBounce();
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 🎯 WORKING: Check if ball is near the target
        /// </summary>
        bool IsNearTarget()
        {
            if (bowlingSystem == null) return false;
            
            Transform target = bowlingSystem.GetTarget();
            if (target == null) return false;
            
            // Expand target radius slightly so path-follow completion still triggers a bounce
            float distanceToTarget = Vector3.Distance(transform.position, target.position);
            float effectiveRadius = Mathf.Max(0.5f, bounceDetectionRadius);
            return distanceToTarget <= effectiveRadius;
        }
        
        /// <summary>
        /// 🎯 WORKING: Check if ball is near ground level (target height)
        /// </summary>
        bool IsNearGroundLevel()
        {
            if (bowlingSystem == null) return false;
            
            Transform target = bowlingSystem.GetTarget();
            if (target == null) return false;
            
            float heightDifference = Mathf.Abs(transform.position.y - target.position.y);
            // Be a bit more forgiving right after path-follow completes
            float effectiveTolerance = Mathf.Max(0.15f, heightTolerance);
            return heightDifference <= effectiveTolerance;
        }
        

        
        /// <summary>
        /// Trigger the bounce event
        /// </summary>
        void TriggerBounce()
        {
            if (Time.time - lastBounceTime < bounceCooldown) return;
            
            hasBounced = true;
            lastBounceTime = Time.time;
            
            // Get current position and velocity
            Vector3 bouncePosition = transform.position;
            Vector3 bounceVelocity = ballRigidbody.linearVelocity;
            
            if (showBounceDebug)
            {
                // Bounce detected
            }
            
            // Notify the bowling system
            if (bowlingSystem != null)
            {
                bowlingSystem.OnBallBounce(bouncePosition, bounceVelocity);
            }
            
            // 🎯 FIXED: Reset bounce flag after a shorter delay for better multiple bounce detection
            Invoke(nameof(ResetBounceFlag), 0.03f);
        }
        
        /// <summary>
        /// Reset the bounce flag to allow for multiple bounces
        /// </summary>
        void ResetBounceFlag()
        {
            hasBounced = false;
        }
        
        /// <summary>
        /// Reset bounce state (called when starting new bowl)
        /// </summary>
        public void ResetBounceState()
        {
            hasBounced = false;
            lastBounceTime = 0f;
        }
        
        /// <summary>
        /// Draw debug gizmos
        /// </summary>
        void OnDrawGizmos()
        {
            if (!showBounceDebug) return;
            
            // Draw bounce detection radius
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, bounceDetectionRadius);
            
            // Draw velocity vector
            if (ballRigidbody != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawRay(transform.position, ballRigidbody.linearVelocity.normalized * 0.5f);
            }
        }
        
        /// <summary>
        /// Get the target transform from the bowling system
        /// </summary>
        public Transform GetTarget()
        {
            if (bowlingSystem == null) return null;
            
            // Use reflection to access the private target field
            var targetField = typeof(BowlingController).GetField("target", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (targetField != null)
            {
                return targetField.GetValue(bowlingSystem) as Transform;
            }
            
            return null;
        }
    }
}
