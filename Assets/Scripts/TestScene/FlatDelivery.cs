using UnityEngine;

namespace CricketGame
{
    public class FlatDelivery : MonoBehaviour
    {
        [Header("Flat Delivery Settings")]
        [Tooltip("Enable/disable flat delivery")]
        [SerializeField] private bool enableFlatDelivery = true;
        
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
            return "Flat Delivery - Straight trajectory";
        }
    }
}
