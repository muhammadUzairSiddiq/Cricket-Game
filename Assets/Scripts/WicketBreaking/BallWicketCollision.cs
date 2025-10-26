using UnityEngine;

namespace CricketGame
{
    /// <summary>
    /// Ball Collision Detection for Wicket Breaking
    /// Detects when the ball hits the wicket and triggers breaking
    /// </summary>
    public class BallWicketCollision : MonoBehaviour
    {
        [Header("Collision Settings")]
        [SerializeField] private LayerMask wicketLayerMask = -1; // Which layers are wickets
        [SerializeField] private float minHitVelocity = 0.1f; // Minimum velocity to break wicket (lowered for yorkers)
        [SerializeField] private float collisionRadius = 0.3f; // Ball collision radius
        [SerializeField] private bool checkAnyCollision = true; // Check any collision, not just wickets
        [SerializeField] private bool forceBreakOnLowSpeed = true; // Force break even on low-speed yorkers
        
        [Header("Debug")]
        [SerializeField] private bool showDebugGizmos = true;
        
        private Rigidbody ballRigidbody;
        private bool hasHitWicket = false;
        private float peakVelocity = 0f; // Track peak velocity for yorkers
        
        void Start()
        {
            ballRigidbody = GetComponent<Rigidbody>();
            
            if (showDebugGizmos)
            {
                Debug.Log($"🎯 BallWicketCollision initialized on: {gameObject.name}");
                Debug.Log($"🎯 Wicket Layer Mask: {wicketLayerMask}");
                Debug.Log($"🎯 Min Hit Velocity: {minHitVelocity}");
                Debug.Log($"🎯 Collision Radius: {collisionRadius}");
                Debug.Log($"🎯 Force Break On Low Speed: {forceBreakOnLowSpeed}");
            }
        }
        
        void Update()
        {
            // Track peak velocity for detection during yorkers
            if (ballRigidbody != null)
            {
                float currentSpeed = ballRigidbody.linearVelocity.magnitude;
                if (currentSpeed > peakVelocity)
                {
                    peakVelocity = currentSpeed;
                }
            }
        }
        
        void OnEnable()
        {
            if (showDebugGizmos)
            {
                Debug.Log($"🎯 BallWicketCollision enabled on: {gameObject.name}");
            }
        }
        
        void OnTriggerEnter(Collider other)
        {
            Debug.Log($"🎯 OnTriggerEnter: {other.name}");
            CheckWicketCollision(other);
        }
        
        void OnTriggerStay(Collider other)
        {
            // Check continuously while inside trigger (catches kinematic collisions)
            if (hasHitWicket) return;
            CheckWicketCollision(other);
        }
        
        void OnCollisionEnter(Collision collision)
        {
            Debug.Log($"🎯 OnCollisionEnter: {collision.gameObject.name}, Contacts: {collision.contactCount}");
            // Check collision with contact points
            CheckCollisionWithContact(collision);
        }
        
        void CheckCollisionWithContact(Collision collision)
        {
            foreach (ContactPoint contact in collision.contacts)
            {
                // Only check collisions with actual wicket parts (stumps/bails)
                string colliderName = contact.otherCollider.name.ToLower();
                if (colliderName.Contains("stump") || 
                    colliderName.Contains("bail") ||
                    colliderName.Contains("wicket"))
                {
                    CheckWicketCollision(contact.otherCollider);
                }
                else
                {
                    // Log ignored collisions for debugging
                    if (showDebugGizmos)
                        Debug.Log($"🎯 Ignoring collision with: {contact.otherCollider.name}");
                }
            }
        }
        
        /// <summary>
        /// Check if collision is with a wicket
        /// </summary>
        void CheckWicketCollision(Collider other)
        {
            // DEBUG: Log all collisions
            if (showDebugGizmos)
            {
                Debug.Log($"🎯 Ball collision detected with: {other.gameObject.name}");
            }
            
            // Check if we've already hit a wicket
            if (hasHitWicket)
            {
                if (showDebugGizmos)
                    Debug.Log("🎯 Already hit wicket, ignoring collision");
                return;
            }
            
            // Check if collision is with wicket layer (only if not checking any collision)
            if (!checkAnyCollision && ((1 << other.gameObject.layer) & wicketLayerMask) == 0)
            {
                if (showDebugGizmos)
                    Debug.Log($"🎯 Collision not on wicket layer: {other.gameObject.layer}");
                return;
            }
            
            // CRITICAL FIX FOR YORKERS: Check previous velocity or current velocity
            if (ballRigidbody == null)
            {
                if (showDebugGizmos)
                    Debug.Log("🎯 Ball has no Rigidbody!");
                return;
            }
            
            float ballSpeed = ballRigidbody.linearVelocity.magnitude;
            
            // For yorkers/full length, use peak velocity if current is too low
            float effectiveSpeed = Mathf.Max(ballSpeed, peakVelocity * 0.3f); // Use 30% of peak as fallback
            
            // For yorkers/full length, ball might have stopped or slowed down
            // Check if we have any velocity or if it's touching a stump
            bool shouldBreak = forceBreakOnLowSpeed || effectiveSpeed >= minHitVelocity || ballSpeed > 0.01f;
            
            if (showDebugGizmos)
                Debug.Log($"🎯 Ball speed: {ballSpeed:F3}, Peak: {peakVelocity:F3}, Effective: {effectiveSpeed:F3}, Should break: {shouldBreak}");
            
            // Even at low speed, if ball is touching stump, it should break
            if (!shouldBreak)
            {
                if (showDebugGizmos)
                    Debug.Log($"🎯 Skipping - ball speed too low: {ballSpeed:F3}");
                return;
            }
            
            // Log low-speed yorker scenario
            if (ballSpeed < minHitVelocity && ballSpeed > 0)
            {
                if (showDebugGizmos)
                    Debug.Log($"🎳 YORKER/FULL LENGTH detected! Low speed ({ballSpeed:F3}), but forcing break!");
            }
            
            // Find WicketBreakingSystem component
            WicketBreakingSystem wicketSystem = other.GetComponentInParent<WicketBreakingSystem>();
            if (wicketSystem == null)
            {
                // Try to find it on any parent
                Transform current = other.transform;
                while (current != null && wicketSystem == null)
                {
                    wicketSystem = current.GetComponent<WicketBreakingSystem>();
                    current = current.parent;
                }
            }
            
            if (wicketSystem == null)
            {
                // Don't log error for non-wicket collisions - just ignore them silently
                if (showDebugGizmos)
                    Debug.Log($"🎯 No WicketBreakingSystem found on: {other.gameObject.name} (ignoring)");
                return;
            }
            
            if (wicketSystem.IsBroken())
            {
                if (showDebugGizmos)
                    Debug.Log("🎯 Wicket already broken");
                return;
            }
            
            // Calculate hit point
            Vector3 hitPoint = other.ClosestPoint(transform.position);
            
            if (showDebugGizmos)
            {
                Debug.Log($"🎯 Breaking wicket! Hit point: {hitPoint}, Speed: {ballSpeed}");
            }
            
            // Break the wicket
            wicketSystem.BreakWicket(ballRigidbody.linearVelocity, hitPoint);
            
            // Mark as hit to prevent multiple breaks
            hasHitWicket = true;
            
            // Reset flag after a delay
            Invoke(nameof(ResetHitFlag), 1f);
        }
        
        /// <summary>
        /// Reset the hit flag
        /// </summary>
        void ResetHitFlag()
        {
            hasHitWicket = false;
        }
        
        /// <summary>
        /// Draw debug gizmos
        /// </summary>
        void OnDrawGizmos()
        {
            if (!showDebugGizmos) return;
            
            // Draw collision radius
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, collisionRadius);
            
            // Draw velocity vector
            if (ballRigidbody != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(transform.position, ballRigidbody.linearVelocity.normalized * 0.5f);
            }
        }
    }
}
