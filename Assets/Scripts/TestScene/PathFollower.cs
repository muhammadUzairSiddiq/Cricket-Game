using UnityEngine;
using System.Collections;

namespace CricketGame
{
    /// <summary>
    /// Moves an object along a provided path of positions at a given speed.
    /// Designed to be attached to the instantiated ball at runtime.
    /// </summary>
    public class PathFollower : MonoBehaviour
    {
        [SerializeField] private float speed = 12f; // m/s along the path
        [SerializeField] private float arcHeight = 0.2f; // subtle cricket arc added on top of path
        [SerializeField] private bool faceVelocity = true;
        [SerializeField] private float obstacleCheckRadius = 0.1f; // Ball radius for obstacle detection
        [SerializeField] private bool enableObstacleDetection = true;
        [SerializeField] private LayerMask obstacleMask = ~0; // Which layers to detect during path following
        
        // Public getters for debugging
        public bool IsObstacleDetectionEnabled => enableObstacleDetection;
        public float ObstacleCheckRadius => obstacleCheckRadius;

        private Vector3[] path;
        private System.Action onComplete;
        private Vector3 previousPosition;

        public void Initialize(Vector3[] worldPath, float pathSpeed, float addedArcHeight, System.Action onDone, bool disableObstacles = false)
        {
            path = worldPath;
            speed = pathSpeed;
            arcHeight = addedArcHeight;
            onComplete = onDone;
            
            // 🎯 CRITICAL: Allow disabling obstacle detection to prevent unwanted collisions
            if (disableObstacles)
            {
                enableObstacleDetection = false;
                Debug.Log($"🎯 PATHFOLLOWER: Obstacle detection DISABLED (prevents ground/plane collisions)");
            }
            
            // 🎯 DEBUG: Verify path initialization
            Debug.Log($"🎯 PATHFOLLOWER INIT:");
            Debug.Log($"   Path Points: {path.Length}");
            Debug.Log($"   Start Point: {path[0]}");
            Debug.Log($"   End Point: {path[path.Length - 1]}");
            Debug.Log($"   Current Ball Position: {transform.position}");
            Debug.Log($"   Speed: {pathSpeed} m/s");
            Debug.Log($"   Arc Height: {addedArcHeight}");
        }

        public void Begin()
        {
            if (path == null || path.Length < 2)
            {
                Debug.LogError("🎯 PATHFOLLOWER: Invalid path!");
                onComplete?.Invoke();
                // Only destroy PathFollower if auto-destroy is enabled
            if (ShouldDestroyPathFollower()) Destroy(this);
                return;
            }
            
            Debug.Log($"🎯 PATHFOLLOWER BEGIN: Starting to follow path from {transform.position}");
            StopAllCoroutines();
            StartCoroutine(FollowPath());
        }

        IEnumerator FollowPath()
        {
            // 🎯 DEBUG: Verify path direction
            Vector3 pathDirection = (path[path.Length - 1] - path[0]).normalized;
            Vector3 ballToTarget = (path[path.Length - 1] - transform.position).normalized;
            float directionDot = Vector3.Dot(pathDirection, ballToTarget);
            
            Debug.Log($"🎯 PATHFOLLOWER DIRECTION CHECK:");
            Debug.Log($"   Ball Position: {transform.position}");
            Debug.Log($"   Path Start: {path[0]}");
            Debug.Log($"   Path End: {path[path.Length - 1]}");
            Debug.Log($"   Path Direction: {pathDirection}");
            Debug.Log($"   Ball-to-Target: {ballToTarget}");
            Debug.Log($"   Direction Match: {directionDot:F2} (1.0 = same direction, -1.0 = opposite)");
            
            if (directionDot < 0)
            {
                Debug.LogError("🚨 PATHFOLLOWER ERROR: Path is REVERSED! Ball will go backwards!");
            }
            
            // Precompute cumulative distances for smooth, non-zigzag motion
            float totalLen = 0f;
            float[] cum = new float[path.Length];
            cum[0] = 0f;
            for (int i = 1; i < path.Length; i++)
            {
                totalLen += Vector3.Distance(path[i - 1], path[i]);
                cum[i] = totalLen;
            }
            if (totalLen < 0.0001f) { 
                onComplete?.Invoke(); 
                // Only destroy PathFollower if auto-destroy is enabled
                if (ShouldDestroyPathFollower()) Destroy(this); 
                yield break; 
            }

            Debug.Log($"🎯 PATHFOLLOWER: Total path length: {totalLen:F2}m");

            float traveled = 0f;
            previousPosition = transform.position; // Initialize previous position

            while (traveled < totalLen)
            {
                traveled += speed * Time.deltaTime;
                float targetDist = Mathf.Clamp(traveled, 0f, totalLen);
                // find segment
                int seg = 0;
                while (seg < path.Length - 1 && cum[seg + 1] < targetDist) seg++;
                float segStart = cum[seg];
                float segEnd = cum[seg + 1];
                float segT = Mathf.InverseLerp(segStart, segEnd, targetDist);
                Vector3 a = path[seg];
                Vector3 b = path[seg + 1];
                Vector3 dir = (b - a).normalized;
                Vector3 pos = Vector3.Lerp(a, b, segT);
                pos.y += Mathf.Sin((targetDist / totalLen) * Mathf.PI) * arcHeight;
                
                // 🎯 OBSTACLE DETECTION: Check for obstacles during path following
                if (enableObstacleDetection)
                {
                    Vector3 movementDirection = (pos - previousPosition).normalized;
                    float movementDistance = Vector3.Distance(previousPosition, pos);
                    
                    if (movementDistance > 0.001f) // Only check if there's actual movement
                    {
                        // Cast a sphere along the movement path to detect obstacles
                        RaycastHit[] hits = Physics.SphereCastAll(previousPosition, obstacleCheckRadius, movementDirection, movementDistance, obstacleMask, QueryTriggerInteraction.Ignore);
                        
                        // 🎯 DEBUG: Log all hits for debugging
                        if (hits.Length > 0)
                        {
                            Debug.Log($"🎯 PATHFOLLOWER CAST: Found {hits.Length} hits at distance {movementDistance:F3}");
                            foreach (var hit in hits)
                            {
                                Debug.Log($"🎯 PATHFOLLOWER HIT: {hit.collider.name} (Layer: {hit.collider.gameObject.layer}, Tag: {hit.collider.tag}, HasRB: {hit.collider.attachedRigidbody != null})");
                            }
                        }
                        
                        foreach (RaycastHit hit in hits)
                        {
                            // Skip self-collision
                            if (hit.collider.gameObject == gameObject)
                                continue;
                            
                            // 🎯 CRITICAL FIX: Ignore ground, plane, pitching area during path following
                            // Only detect REAL obstacles (bat, stumps, fielders, etc.)
                            string objName = hit.collider.gameObject.name.ToLower();
                            bool isGroundObject = hit.collider.CompareTag("Ground") || 
                                                 objName.Contains("plane") || 
                                                 objName.Contains("ground") || 
                                                 objName.Contains("pitch") ||
                                                 objName.Contains("field");
                            
                            // Skip if it's a ground/plane object
                            if (isGroundObject)
                            {
                                Debug.Log($"🎯 PATHFOLLOWER: Ignoring ground object: {hit.collider.name}");
                                continue;
                            }
                                
                            // Treat any solid (non-trigger) collider that isn't the ball/ground as an obstacle
                            bool isSolid = hit.collider != null && hit.collider.enabled && !hit.collider.isTrigger;
                            bool isBall = hit.collider.CompareTag("Ball");
                            if (isSolid && !isBall)
                            {
                                Debug.Log($"🎯 PATHFOLLOWER OBSTACLE HIT: Ball hit obstacle {hit.collider.name} during curved path movement");
                                
                                // Check if obstacle is a wicket and trigger breaking
                                CheckForWicketHit(hit);
                                
                                // Apply physics response to the obstacle
                                ApplyObstaclePhysicsResponse(hit, movementDirection, speed);
                                
                                // Hand control to physics immediately to respect colliders
                                Rigidbody rb = GetComponent<Rigidbody>();
                                if (rb != null)
                                {
                                    // Place ball at safe contact point
                                    Vector3 contactPos = hit.point + hit.normal * obstacleCheckRadius;
                                    transform.position = contactPos;
                                    
                                    // Enable physics and apply a reflected velocity
                                    rb.isKinematic = false;
                                    rb.useGravity = true;
                                    Vector3 reflected = Vector3.Reflect(movementDirection, hit.normal).normalized;
                                    float resumeSpeed = Mathf.Max(8f, speed);
                                    Vector3 resumeVelocity = reflected * resumeSpeed + Vector3.down * 2f;
                                    rb.linearVelocity = resumeVelocity;
                                    
                                    Debug.Log($"🎯 PATHFOLLOWER HANDOFF: Physics resumed with velocity {resumeVelocity}");
                                }
                                // Stop following the scripted path; physics now takes over
                                onComplete = null; // prevent delivery callback that repositions to target
                                // Only destroy PathFollower if auto-destroy is enabled
                                if (ShouldDestroyPathFollower()) Destroy(this);
                                yield break;
                            }
                        }
                    }
                }
                
                transform.position = pos;
                previousPosition = pos;
                
                if (faceVelocity && dir.sqrMagnitude > 0.0001f) transform.forward = Vector3.Lerp(transform.forward, dir, 0.5f);

                yield return null;
            }

            onComplete?.Invoke();
            // Only destroy PathFollower if auto-destroy is enabled
            if (ShouldDestroyPathFollower()) Destroy(this);
        }
        
        /// <summary>
        /// Check if the obstacle is a wicket and trigger breaking
        /// </summary>
        private void CheckForWicketHit(RaycastHit hit)
        {
            // Check if this is a wicket component
            string objName = hit.collider.name.ToLower();
            if (objName.Contains("stump") || objName.Contains("bail") || objName.Contains("wicket"))
            {
                Debug.Log($"🎳 PATHFOLLOWER WICKET HIT: {hit.collider.name}");
                
                // Find WicketBreakingSystem
                WicketBreakingSystem wicketSystem = hit.collider.GetComponentInParent<WicketBreakingSystem>();
                if (wicketSystem == null)
                {
                    Transform current = hit.collider.transform;
                    while (current != null && wicketSystem == null)
                    {
                        wicketSystem = current.GetComponent<WicketBreakingSystem>();
                        current = current.parent;
                    }
                }
                
                if (wicketSystem != null && !wicketSystem.IsBroken())
                {
                    // Calculate ball velocity from PathFollower's current movement
                    Vector3 ballVelocity = speed * (hit.collider.transform.position - transform.position).normalized;
                    Vector3 hitPoint = hit.point;
                    
                    Debug.Log($"🎳 PATHFOLLOWER BREAKING WICKET: Speed={speed:F1}, Velocity={ballVelocity}, Hit={hitPoint}");
                    wicketSystem.BreakWicket(ballVelocity, hitPoint);
                }
                else if (wicketSystem == null)
                {
                    Debug.LogWarning($"🎳 PATHFOLLOWER: No WicketBreakingSystem found on {hit.collider.name}");
                }
                else
                {
                    Debug.Log($"🎳 PATHFOLLOWER: Wicket already broken");
                }
            }
        }
        
        /// <summary>
        /// Apply physics response to obstacles hit during path following
        /// </summary>
        private void ApplyObstaclePhysicsResponse(RaycastHit hit, Vector3 ballDirection, float ballSpeed)
        {
            Rigidbody obstacleRb = hit.collider.attachedRigidbody;
            if (obstacleRb != null)
            {
                // Apply force to the obstacle
                Vector3 forceDirection = hit.normal;
                float forceMagnitude = ballSpeed * 0.5f; // Configurable force multiplier
                Vector3 force = forceDirection * forceMagnitude;
                
                obstacleRb.AddForceAtPosition(force, hit.point, ForceMode.Impulse);
                
                Debug.Log($"🎯 PATHFOLLOWER OBSTACLE FORCE: Applied {force.magnitude:F1}N force to {hit.collider.name}");
            }
            
            // Optional: Add visual/audio effects here
            // Example: Particle effects, sound effects, etc.
        }
        
        /// <summary>
        /// Check if path follower should destroy itself based on auto-destroy setting
        /// </summary>
        private bool ShouldDestroyPathFollower()
        {
            // Find BallSettingsSO to check auto-destroy setting
            BallSettingsSO ballSettingsSO = FindObjectOfType<BallSettingsSO>();
            if (ballSettingsSO != null && !ballSettingsSO.EnableAutoDestroy)
            {
                Debug.Log("🏏 PathFollower: Auto-destroy disabled - PathFollower will not be destroyed");
                return false;
            }
            
            return true; // Default behavior - destroy PathFollower
        }
    }
}


