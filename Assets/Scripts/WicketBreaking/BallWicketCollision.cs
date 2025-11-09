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
        
        private Rigidbody ballRigidbody;
        private bool hasHitWicket = false;
        private float peakVelocity = 0f; // Track peak velocity for yorkers
        
        void Start()
        {
            ballRigidbody = GetComponent<Rigidbody>();
            
            {

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
            {
            }
        }
        
        void OnTriggerEnter(Collider other)
        {
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
			}
		}
        
        /// <summary>
        /// Check if collision is with a wicket
        /// </summary>
		void CheckWicketCollision(Collider other)
		{
            // Check if we've already hit a wicket
            if (hasHitWicket)
            {
                return;
            }
            
            // Check if collision is with wicket layer (only if not checking any collision)
            if (!checkAnyCollision && ((1 << other.gameObject.layer) & wicketLayerMask) == 0)
            {
                return;
            }
            
            // CRITICAL FIX FOR YORKERS: Check previous velocity or current velocity
            if (ballRigidbody == null)
            {
                return;
            }
            
            float ballSpeed = ballRigidbody.linearVelocity.magnitude;
            
            // For yorkers/full length, use peak velocity if current is too low
            float effectiveSpeed = Mathf.Max(ballSpeed, peakVelocity * 0.3f); // Use 30% of peak as fallback
            
			// For yorkers/full length, ball might have stopped or slowed down
			// Check if we have any velocity or if it's touching a stump
			bool shouldBreak = forceBreakOnLowSpeed || effectiveSpeed >= minHitVelocity || ballSpeed > 0.01f;

            // Even at low speed, if ball is touching stump, it should break
            if (!shouldBreak)
            {
                return;
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
                return;
            }
            
            if (wicketSystem.IsBroken())
            {
                return;
            }
            
            // Calculate hit point
            Vector3 hitPoint = other.ClosestPoint(transform.position);
            
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
