using UnityEngine;

namespace CricketGame
{
    /// <summary>
    /// Flat Delivery - Straight path with no swing or spin
    /// 🎯 WORKS FROM ANY SPAWN POINT - Uses PathFollower for 100% accuracy
    /// </summary>
    public class FlatDelivery : MonoBehaviour
    {
        [Header("Flat Delivery Settings")]
        [Tooltip("Enable/disable flat delivery")]
        [SerializeField] private bool enableFlatDelivery = true;
        
        [Tooltip("Use PathFollower for guaranteed accuracy (straight path)")]
        [SerializeField] private bool usePathFollower = true;
        
        [Header("Path Settings")]
        [Tooltip("Vertical arc height for realistic cricket trajectory (0.5-1.5 for realistic arc)")]
        public float pathArcHeight = 0.8f; // Realistic cricket bowling arc
        
        [Tooltip("Disable obstacle detection during path following (only disable if having issues with ground detection)")]
        public bool disableObstacleDetection = false; // Enabled for real obstacle detection (bat, stumps, etc.)
        
        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;
        
        void Start()
        {
            if (showDebugLogs)
            {
                Debug.Log("🎯 FlatDelivery: Ready for straight flat deliveries");
            }
        }
        
        /// <summary>
        /// Calculate flat trajectory (straight line to target)
        /// </summary>
        public Vector3 CalculateTrajectory(Vector3 startPos, Vector3 targetPos, float ballSpeed)
        {
            if (!enableFlatDelivery)
                return targetPos;
                
            // Flat delivery goes straight to target - no modification needed
            if (showDebugLogs)
            {
                Debug.Log($"🎯 FlatDelivery: Straight trajectory to target at speed {ballSpeed:F1} m/s");
            }
            
            return targetPos;
        }
        
        /// <summary>
        /// Get flat delivery direction (straight line)
        /// </summary>
        public Vector3 GetDeliveryDirection(Vector3 startPos, Vector3 targetPos, float ballSpeed)
        {
            if (!enableFlatDelivery)
                return (targetPos - startPos).normalized;
                
            // Flat delivery direction is straight line to target
            Vector3 direction = (targetPos - startPos).normalized;
            
            if (showDebugLogs)
            {
                Debug.Log($"🎯 FlatDelivery: Straight direction vector = {direction}");
            }
            
            return direction;
        }
        
        /// <summary>
        /// Reset flat delivery for new ball
        /// </summary>
        public void ResetDelivery()
        {
            if (showDebugLogs)
            {
                Debug.Log("🎯 FlatDelivery: Reset for new ball");
            }
        }
        
        /// <summary>
        /// Get flat delivery info
        /// </summary>
        public string GetDeliveryInfo()
        {
            return "Flat Delivery - Straight trajectory (PathFollower for accuracy)";
        }
        
        /// <summary>
        /// Generate straight path points for PathFollower
        /// 🎯 WORKS FROM ANY SPAWN POINT - Pure straight line for 100% accuracy
        /// </summary>
        public Vector3[] GetCurvedPathPoints(Vector3 startPos, Vector3 targetPos, float ballSpeed, int segments = 30)
        {
            // Flat delivery ALWAYS returns perfectly straight path
            Vector3[] straight = new Vector3[Mathf.Max(2, segments + 1)];
            for (int i = 0; i < straight.Length; i++)
            {
                float t = (float)i / (straight.Length - 1);
                straight[i] = Vector3.Lerp(startPos, targetPos, t);
            }
            
            if (showDebugLogs)
            {
                Debug.Log($"🎯 FLAT PATH: Perfectly straight path - {straight.Length} points");
                Debug.Log($"   Start: {startPos}");
                Debug.Log($"   Target: {targetPos}");
                Debug.Log($"   Direction: {(targetPos - startPos).normalized}");
                Debug.Log($"   ✅ Pure straight line - 100% ACCURATE - works from ANY spawn point!");
            }
            
            return straight;
        }
        
        /// <summary>
        /// Check if path follower is enabled
        /// </summary>
        public bool IsCurvedPathEnabled()
        {
            return enableFlatDelivery && usePathFollower;
        }
    }
}
