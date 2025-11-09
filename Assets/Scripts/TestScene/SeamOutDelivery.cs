using UnityEngine;

namespace CricketGame
{
    /// <summary>
    /// Seam Out Delivery - Ball goes straight to target
    /// 🎯 WORKS FROM ANY SPAWN POINT - Uses PathFollower for 100% accuracy
    /// </summary>
    public class SeamOutDelivery : MonoBehaviour
    {
        [Header("Seam Out Settings")]
        [Tooltip("Enable/disable seam out delivery")]
        [SerializeField] private bool enableSeamOut = true;
        
        [Tooltip("Use PathFollower for guaranteed accuracy (straight path)")]
        [SerializeField] private bool usePathFollower = true;
        
        [Header("Path Settings")]
        [Tooltip("Vertical arc height for realistic cricket trajectory (0.5-1.5 for realistic arc)")]
        public float pathArcHeight = 0.8f; // Realistic cricket bowling arc
        
        [Header("Seam Angle Settings")]
        [Tooltip("Enable seam angle offset (ball lands slightly off-center)")]
        public bool enableSeamAngle = true;
        
        [Tooltip("Lateral offset distance (meters) - Ball lands this far to the LEFT of target center")]
        [Range(0.1f, 1.0f)]
        public float seamAngleOffset = 0.3f; // Ball lands 0.3m to the LEFT (seam out effect)
        
        [Header("Post-Bounce Seam Movement")]
        [Tooltip("Enable seam movement after ball bounces (ball continues moving left after landing)")]
        public bool enablePostBounceSeam = true;
        
        [Tooltip("Seam movement strength after bounce (positive = continues moving left, negative = reverses)")]
        [Range(-2.0f, 2.0f)]
        public float postBounceSeamStrength = 0.8f; // Continue moving left after bounce
        
        [Tooltip("Disable obstacle detection during path following (only disable if having issues with ground detection)")]
        public bool disableObstacleDetection = false; // Enabled for real obstacle detection (bat, stumps, etc.)
        
        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;
        
        void Start()
        {
            if (showDebugLogs)
            {
            }
        }
        
        /// <summary>
        /// Calculate seam out trajectory (straight path for PathFollower)
        /// </summary>
        public Vector3 CalculateTrajectory(Vector3 startPos, Vector3 targetPos, float ballSpeed)
        {
            if (!enableSeamOut)
                return targetPos;
            
            // Seam deliveries use straight path to target for 100% accuracy
            if (showDebugLogs)
            {
            }
            
            return targetPos;
        }
        
        /// <summary>
        /// Get seam out direction (straight to target)
        /// </summary>
        public Vector3 GetDeliveryDirection(Vector3 startPos, Vector3 targetPos, float ballSpeed)
        {
            if (!enableSeamOut)
                return (targetPos - startPos).normalized;
            
            // Return straight direction to target
            Vector3 straightDirection = (targetPos - startPos).normalized;
            
            if (showDebugLogs)
            {
            }
            
            return straightDirection;
        }
        
        /// <summary>
        /// Generate straight path points for PathFollower with seam angle offset
        /// 🎯 WORKS FROM ANY SPAWN POINT - Straight line to offset target (seam effect)
        /// </summary>
        public Vector3[] GetCurvedPathPoints(Vector3 startPos, Vector3 targetPos, float ballSpeed, int segments = 30)
        {
            // 🎯 SEAM ANGLE: Apply lateral offset to target (ball lands to LEFT of center)
            Vector3 adjustedTarget = targetPos;
            
            if (enableSeamAngle && seamAngleOffset > 0)
            {
                // Calculate bowling direction
                Vector3 bowlingDirection = (targetPos - startPos).normalized;
                
                // Calculate LEFT direction (perpendicular to bowling direction)
                // Cross(up, bowlingDirection) = LEFT relative to bowling direction
                Vector3 leftDirection = Vector3.Cross(Vector3.up, bowlingDirection).normalized;
                
                // Apply offset to the LEFT (seam out effect)
                adjustedTarget = targetPos + leftDirection * seamAngleOffset;
                
                if (showDebugLogs)
                {

                }
            }
            
            // Generate straight path to adjusted target
            Vector3[] straight = new Vector3[Mathf.Max(2, segments + 1)];
            for (int i = 0; i < straight.Length; i++)
            {
                float t = (float)i / (straight.Length - 1);
                straight[i] = Vector3.Lerp(startPos, adjustedTarget, t);
            }
            
            if (showDebugLogs)
            {

            }
            
            return straight;
        }
        
        /// <summary>
        /// Check if path follower is enabled
        /// </summary>
        public bool IsCurvedPathEnabled()
        {
            return enableSeamOut && usePathFollower;
        }
        
        /// <summary>
        /// Reset seam out delivery for new ball
        /// </summary>
        public void ResetDelivery()
        {
            if (showDebugLogs)
            {
            }
        }
        
        /// <summary>
        /// Get seam out delivery info
        /// </summary>
        public string GetDeliveryInfo()
        {
            string info = "Seam Out Delivery";
            if (enableSeamAngle)
                info += $" - Lands {seamAngleOffset:F2}m LEFT of target";
            if (enablePostBounceSeam)
                info += $" + Continues LEFT after bounce ({postBounceSeamStrength:F1})";
            return info;
        }
    }
}
