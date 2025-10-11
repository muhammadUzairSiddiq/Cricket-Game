using UnityEngine;

namespace CricketGame
{
    /// <summary>
    /// Seam In Delivery - Ball seams towards the batsman
    /// 🎯 WORKS FROM ANY SPAWN POINT - Uses PathFollower for 100% accuracy
    /// </summary>
    public class SeamInDelivery : MonoBehaviour
    {
        [Header("Seam In Settings")]
        [Tooltip("Enable/disable seam in delivery")]
        [UnityEngine.Serialization.FormerlySerializedAs("enableInSwing")]
        [SerializeField] private bool enableSeamIn = true;
        
        [Tooltip("Use PathFollower for guaranteed accuracy (straight path)")]
        [SerializeField] private bool usePathFollower = true;
        
        [Header("Path Settings")]
        [Tooltip("Vertical arc height for realistic cricket trajectory (0.5-1.5 for realistic arc)")]
        public float pathArcHeight = 0.8f; // Realistic cricket bowling arc
        
        [Header("Seam Angle Settings")]
        [Tooltip("Enable seam angle offset (ball lands slightly off-center)")]
        public bool enableSeamAngle = true;
        
        [Tooltip("Lateral offset distance (meters) - Ball lands this far to the RIGHT of target center")]
        [Range(0.1f, 1.0f)]
        public float seamAngleOffset = 0.3f; // Ball lands 0.3m to the RIGHT (seam in effect)
        
        [Header("Post-Bounce Seam Movement")]
        [Tooltip("Enable seam movement after ball bounces (ball continues moving right after landing)")]
        public bool enablePostBounceSeam = true;
        
        [Tooltip("Seam movement strength after bounce (positive = continues moving right, negative = reverses)")]
        [Range(-2.0f, 2.0f)]
        public float postBounceSeamStrength = 0.8f; // Continue moving right after bounce
        
        [Tooltip("Disable obstacle detection during path following (only disable if having issues with ground detection)")]
        public bool disableObstacleDetection = false; // Enabled for real obstacle detection (bat, stumps, etc.)
        
        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;
        
        void Start()
        {
            if (showDebugLogs)
            {
                Debug.Log("🎯 SeamInDelivery: Ready for seam in deliveries");
            }
        }
        
        /// <summary>
        /// Calculate seam in trajectory (straight path for PathFollower)
        /// </summary>
        public Vector3 CalculateTrajectory(Vector3 startPos, Vector3 targetPos, float ballSpeed)
        {
            if (!enableSeamIn)
                return targetPos;
            
            // Seam deliveries use straight path to target for 100% accuracy
            if (showDebugLogs)
            {
                Debug.Log($"🎯 SeamInDelivery: Straight trajectory - Speed: {ballSpeed:F1} m/s");
            }
            
            return targetPos;
        }
        
        /// <summary>
        /// Get seam in direction (straight to target)
        /// </summary>
        public Vector3 GetDeliveryDirection(Vector3 startPos, Vector3 targetPos, float ballSpeed)
        {
            if (!enableSeamIn)
                return (targetPos - startPos).normalized;
            
            // Return straight direction to target
            Vector3 straightDirection = (targetPos - startPos).normalized;
            
            if (showDebugLogs)
            {
                Debug.Log($"🎯 SeamInDelivery: Straight direction to target");
            }
            
            return straightDirection;
        }
        
        /// <summary>
        /// Generate straight path points for PathFollower with seam angle offset
        /// 🎯 WORKS FROM ANY SPAWN POINT - Straight line to offset target (seam effect)
        /// </summary>
        public Vector3[] GetCurvedPathPoints(Vector3 startPos, Vector3 targetPos, float ballSpeed, int segments = 30)
        {
            // 🎯 SEAM ANGLE: Apply lateral offset to target (ball lands to RIGHT of center)
            Vector3 adjustedTarget = targetPos;
            
            if (enableSeamAngle && seamAngleOffset > 0)
            {
                // Calculate bowling direction
                Vector3 bowlingDirection = (targetPos - startPos).normalized;
                
                // Calculate RIGHT direction (perpendicular to bowling direction)
                // Cross(bowlingDirection, up) = RIGHT relative to bowling direction
                Vector3 rightDirection = Vector3.Cross(bowlingDirection, Vector3.up).normalized;
                
                // Apply offset to the RIGHT (seam in effect)
                adjustedTarget = targetPos + rightDirection * seamAngleOffset;
                
                if (showDebugLogs)
                {
                    Debug.Log($"🎯 SEAM IN ANGLE: Target offset {seamAngleOffset:F2}m to the RIGHT");
                    Debug.Log($"   Original Target: {targetPos}");
                    Debug.Log($"   Adjusted Target: {adjustedTarget}");
                    Debug.Log($"   Right Direction: {rightDirection}");
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
                Debug.Log($"🎯 SEAM IN PATH: Straight path with angle - {straight.Length} points");
                Debug.Log($"   Start: {startPos}");
                Debug.Log($"   Target (Offset): {adjustedTarget}");
                Debug.Log($"   Direction: {(adjustedTarget - startPos).normalized}");
                Debug.Log($"   ✅ Seam angle applied - works from ANY spawn point!");
            }
            
            return straight;
        }
        
        /// <summary>
        /// Check if path follower is enabled
        /// </summary>
        public bool IsCurvedPathEnabled()
        {
            return enableSeamIn && usePathFollower;
        }
        
        /// <summary>
        /// Reset seam in delivery for new ball
        /// </summary>
        public void ResetDelivery()
        {
            if (showDebugLogs)
            {
                Debug.Log("🎯 SeamInDelivery: Reset for new ball");
            }
        }
        
        /// <summary>
        /// Get seam in delivery info
        /// </summary>
        public string GetDeliveryInfo()
        {
            string info = "Seam In Delivery";
            if (enableSeamAngle)
                info += $" - Lands {seamAngleOffset:F2}m RIGHT of target";
            if (enablePostBounceSeam)
                info += $" + Continues RIGHT after bounce ({postBounceSeamStrength:F1})";
            return info;
        }
    }
}
