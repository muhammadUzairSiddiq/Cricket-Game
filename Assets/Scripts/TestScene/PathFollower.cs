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
        [SerializeField] private float obstacleCheckRadius = 0.15f; // Ball radius for obstacle detection (increased for better wicket detection)
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
            }
            
            // 🎯 DEBUG: Verify path initialization

        }

        public void Begin()
        {
            if (path == null || path.Length < 2)
            {
                onComplete?.Invoke();
                // Only destroy PathFollower if auto-destroy is enabled
            if (ShouldDestroyPathFollower()) Destroy(this);
                return;
            }
            StopAllCoroutines();
            StartCoroutine(FollowPath());
        }

        IEnumerator FollowPath()
        {
            // 🎯 DEBUG: Verify path direction
            Vector3 pathDirection = (path[path.Length - 1] - path[0]).normalized;
            Vector3 ballToTarget = (path[path.Length - 1] - transform.position).normalized;
            float directionDot = Vector3.Dot(pathDirection, ballToTarget);

            
            if (directionDot < 0)
            {
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
                            foreach (var hit in hits)
                            {
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
                                continue;
                            }
                                
                            // Treat any solid (non-trigger) collider that isn't the ball/ground as an obstacle
                            bool isSolid = hit.collider != null && hit.collider.enabled && !hit.collider.isTrigger;
                            bool isBall = hit.collider.CompareTag("Ball");
                            if (isSolid && !isBall)
                            {
                                
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
                
                if (wicketSystem != null)
                {
                    // Calculate ball velocity from PathFollower's current movement
                    Vector3 ballVelocity = speed * (hit.collider.transform.position - transform.position).normalized;
                    Vector3 hitPoint = hit.point;
                    wicketSystem.BreakWicket(ballVelocity, hitPoint);
                }
                else
                {
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
                return false;
            }
            
            return true; // Default behavior - destroy PathFollower
        }
    }
}

