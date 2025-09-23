using UnityEngine;

namespace CricketGame
{
    public class SeamOutDelivery : MonoBehaviour
    {
        [Header("Seam Out Settings")]
        [Tooltip("Enable/disable seam out delivery")]
        [UnityEngine.Serialization.FormerlySerializedAs("enableOutSwing")]
        [SerializeField] private bool enableSeamOut = true;
        
        [Tooltip("Base swing force multiplier")]
        [SerializeField] private float baseSwingForce = 1.0f;
        
        [Tooltip("Minimum swing at speed 9 (low speed = less swing)")]
        [SerializeField] private float minSwingAtSpeed9 = 0.2f;
        
        [Tooltip("Maximum swing at speed 16 (high speed = extreme swing)")]
        [SerializeField] private float maxSwingAtSpeed16 = 1.47f;
        
        [Header("Swing Direction")]
        [Tooltip("Direction of seam out (positive X = right, negative X = left)")]
        [SerializeField] private Vector3 swingDirection = new Vector3(1f, 0f, 0f);
        
        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;
        
        void Start()
        {
            if (showDebugLogs)
            {
                Debug.Log("🎯 SeamOutDelivery: Ready for seam out deliveries");
            }
        }
        
        /// <summary>
        /// Calculate seam out trajectory (curves away from batsman)
        /// </summary>
        public Vector3 CalculateTrajectory(Vector3 startPos, Vector3 targetPos, float ballSpeed)
        {
            if (!enableSeamOut)
                return targetPos;
                
            // Calculate swing force based on speed (9 = less swing, 16 = extreme swing)
            float swingForce = CalculateSwingForce(ballSpeed);
            
            // Create a curved path using Bezier curve control points
            Vector3 swingTarget = CalculateBezierCurveTarget(startPos, targetPos, swingForce);
            
            if (showDebugLogs)
            {
                Debug.Log($"🎯 SeamOutDelivery: Calculated curved trajectory - Force: {swingForce:F2}, Speed: {ballSpeed:F1}");
            }
            
            return swingTarget;
        }
        
        /// <summary>
        /// Calculate Bezier curve target for smooth seam out trajectory
        /// </summary>
        private Vector3 CalculateBezierCurveTarget(Vector3 startPos, Vector3 targetPos, float swingForce)
        {
            // Calculate distance and direction
            Vector3 direction = (targetPos - startPos).normalized;
            float distance = Vector3.Distance(startPos, targetPos);
            
            // Create control points for Bezier curve
            Vector3 midPoint = Vector3.Lerp(startPos, targetPos, 0.5f);
            
            // Control point for right curve (seam out) - POSITIVE values
            Vector3 rightOffset = new Vector3(swingForce * 3f, 0, 0); // Curve right
            Vector3 controlPoint = midPoint + rightOffset;
            
            // Calculate final target using Bezier curve
            Vector3 curveTarget = CalculateBezierPoint(startPos, controlPoint, targetPos, 0.8f);
            
            return curveTarget;
        }
        
        /// <summary>
        /// Calculate point on Bezier curve
        /// </summary>
        private Vector3 CalculateBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;
            
            Vector3 p = uuu * p0; // (1-t)^3 * P0
            p += 3 * uu * t * p1; // 3(1-t)^2 * t * P1
            p += 3 * u * tt * p2; // 3(1-t) * t^2 * P2
            p += ttt * p2; // t^3 * P2
            
            return p;
        }
        
        /// <summary>
        /// Get seam out direction for trajectory calculation
        /// </summary>
        public Vector3 GetDeliveryDirection(Vector3 startPos, Vector3 targetPos, float ballSpeed)
        {
            if (!enableSeamOut)
                return (targetPos - startPos).normalized;
                
            float swingForce = CalculateSwingForce(ballSpeed);
            Vector3 baseDirection = (targetPos - startPos).normalized;
            
            // Add rightward curve to the direction - POSITIVE values
            Vector3 swingDirection = baseDirection + new Vector3(swingForce * 0.3f, 0, 0);
            
            if (showDebugLogs)
            {
                Debug.Log($"🎯 SeamOutDelivery: Swing direction calculated - Force: {swingForce:F2}");
            }
            
            return swingDirection.normalized;
        }
        
        /// <summary>
        /// Calculate swing force based on ball speed
        /// </summary>
        private float CalculateSwingForce(float speed)
        {
            // Linear interpolation between min swing (speed 9) and max swing (speed 16)
            float normalizedSpeed = Mathf.InverseLerp(9f, 16f, speed);
            float swingForce = Mathf.Lerp(minSwingAtSpeed9, maxSwingAtSpeed16, normalizedSpeed);
            
            return swingForce;
        }
        
        /// <summary>
        /// Reset seam out delivery for new ball
        /// </summary>
        public void ResetDelivery()
        {
            if (showDebugLogs)
            {
                Debug.Log("🎯 SeamOutDelivery: Reset for new ball");
            }
        }
        
        /// <summary>
        /// Get seam out delivery info
        /// </summary>
        public string GetDeliveryInfo()
        {
            return "Seam Out Delivery - Curves away from batsman";
        }
        
        /// <summary>
        /// Update swing settings from UI
        /// </summary>
        public void UpdateSwingSettings(float minSwing, float maxSwing, float baseForce)
        {
            minSwingAtSpeed9 = minSwing;
            maxSwingAtSpeed16 = maxSwing;
            baseSwingForce = baseForce;
            
            if (showDebugLogs)
            {
                Debug.Log($"🎯 SeamOutDelivery: Updated settings - Min: {minSwing}, Max: {maxSwing}, Base: {baseForce}");
            }
        }
    }
}