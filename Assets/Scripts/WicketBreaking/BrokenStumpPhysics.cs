using UnityEngine;

namespace CricketGame
{
    /// <summary>
    /// Stops physics when a broken stump or bail hits the ground
    /// </summary>
    public class BrokenStumpPhysics : MonoBehaviour
    {
        private bool hasHitGround = false;
        private float checkInterval = 0.05f; // Check more frequently (20 times per second)
        private float timeSinceLastCheck = 0f;
        private float groundCheckDistance = 0.5f; // Increased detection range
        
        void Update()
        {
            if (hasHitGround) return;
            
            timeSinceLastCheck += Time.deltaTime;
            
            // Check for ground contact every interval
            if (timeSinceLastCheck >= checkInterval)
            {
                timeSinceLastCheck = 0f;
                CheckForGroundContact();
            }
        }
        
        void CheckForGroundContact()
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                Debug.Log($"🎳 {gameObject.name} has no Rigidbody, skipping ground check");
                return;
            }
            if (rb.isKinematic)
            {
                Debug.Log($"🎳 {gameObject.name} is already kinematic, skipping ground check");
                return;
            }
            
            // Raycast downward to check if we're near the ground
            Vector3 rayStart = transform.position;
            float rayLength = groundCheckDistance;
            
            // Cast multiple rays (center + corners) for better detection
            RaycastHit[] hits = Physics.RaycastAll(rayStart, Vector3.down, rayLength);
            
            bool foundGround = false;
            
            foreach (RaycastHit hit in hits)
            {
                string hitName = hit.collider.name.ToLower();
                
                // Check if this is ground, pitch, grass, etc.
                if (hitName.Contains("ground") || 
                    hitName.Contains("pitch") || 
                    hitName.Contains("plane") || 
                    hitName.Contains("grass") ||
                    hit.collider.CompareTag("Ground"))
                {
                    foundGround = true;
                    
                    Debug.Log($"🎳 ✅ {gameObject.name} hit the ground ({hit.collider.name}) at distance {hit.distance:F3} - stopping physics!");
                    
                    hasHitGround = true;
                    
                    // CRITICAL: Set velocities BEFORE making kinematic
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.useGravity = false;
                    
                    // NOW make it kinematic
                    rb.isKinematic = true;
                    
                    // Optional: Snap to ground surface slightly above
                    transform.position = hit.point + hit.normal * 0.01f; // Small offset to prevent clipping
                    
                    break;
                }
            }
            
            // Debug: Log if no ground found
            if (!foundGround && hits.Length > 0)
            {
                string hitNames = "";
                foreach (RaycastHit hit in hits)
                {
                    hitNames += hit.collider.name + " ";
                }
                Debug.Log($"🎳 {gameObject.name} cast hit {hits.Length} objects but no ground: {hitNames}");
            }
        }
        
        void OnCollisionEnter(Collision collision)
        {
            if (hasHitGround) return;
            
            // Also check collision events as backup
            GameObject ground = collision.gameObject;
            string groundName = ground.name.ToLower();
            
            bool isGround = groundName.Contains("ground") || 
                           groundName.Contains("pitch") ||
                           groundName.Contains("plane") ||
                           groundName.Contains("grass");
            
            if (isGround)
            {
                Debug.Log($"🎳 {gameObject.name} collided with ground: {ground.name}");
                hasHitGround = true;
                
                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb != null && !rb.isKinematic)
                {
                    Debug.Log($"🎳 ✅ Stopping physics on ground contact!");
                    
                    rb.isKinematic = true;
                    rb.useGravity = false;
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
        }
    }
}

