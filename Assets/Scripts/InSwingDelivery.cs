using UnityEngine;

namespace CricketGame
{
    public class InSwingDelivery : MonoBehaviour
    {
        [Header("In Swing Settings")]
        [Tooltip("Enable/disable in swing delivery")]
        [SerializeField] private bool enableInSwing = true;
        
        [Tooltip("Base swing force multiplier")]
        [SerializeField] private float baseSwingForce = 1.0f;
        
        [Tooltip("Minimum swing at speed 9 (low speed = less swing)")]
        [SerializeField] private float minSwingAtSpeed9 = 0.2f;
        
        [Tooltip("Maximum swing at speed 16 (high speed = extreme swing)")]
        [SerializeField] private float maxSwingAtSpeed16 = 2.5f;
        
        [Header("Swing Direction")]
        [Tooltip("Direction of in swing (negative X = left, positive X = right)")]
        [SerializeField] private Vector3 swingDirection = new Vector3(-1f, 0f, 0f);
        
        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;
        
        void Start()
        {
            if (showDebugLogs)
            {
                Debug.Log("🎯 InSwingDelivery: Ready for in swing deliveries");
            }
        }
        
        /// <summary>
        /// Calculate in swing trajectory (curves left towards batsman)
        /// </summary>
        public Vector3 CalculateTrajectory(Vector3 startPos, Vector3 targetPos, float ballSpeed)
        {
            if (!enableInSwing)
                return targetPos;
                
            // Calculate swing force based on speed (9 = less swing, 16 = extreme swing)
            float swingForce = CalculateSwingForce(ballSpeed);
            
            // Create a curved path using Bezier curve control points
            Vector3 swingTarget = CalculateBezierCurveTarget(startPos, targetPos, swingForce);
            
            if (showDebugLogs)
            {
                Debug.Log($"🎯 InSwingDelivery: Calculated curved trajectory - Force: {swingForce:F2}, Speed: {ballSpeed:F1}");
            }
            
            return swingTarget;
        }
        
        /// <summary>
        /// Calculate Bezier curve target for smooth in swing trajectory
        /// </summary>
        private Vector3 CalculateBezierCurveTarget(Vector3 startPos, Vector3 targetPos, float swingForce)
        {
            // Calculate distance and direction
            Vector3 direction = (targetPos - startPos).normalized;
            float distance = Vector3.Distance(startPos, targetPos);
            
            // Create control points for Bezier curve
            Vector3 midPoint = Vector3.Lerp(startPos, targetPos, 0.5f);
            
            // Control point for left curve (in swing)
            Vector3 leftOffset = new Vector3(-swingForce * 3f, 0, 0); // Curve left
            Vector3 controlPoint = midPoint + leftOffset;
            
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
        /// Get in swing direction for trajectory calculation
        /// </summary>
        public Vector3 GetDeliveryDirection(Vector3 startPos, Vector3 targetPos, float ballSpeed)
        {
            if (!enableInSwing)
                return (targetPos - startPos).normalized;
                
            float swingForce = CalculateSwingForce(ballSpeed);
            Vector3 baseDirection = (targetPos - startPos).normalized;
            
            // Add leftward curve to the direction
            Vector3 swingDirection = baseDirection + new Vector3(-swingForce * 0.3f, 0, 0);
            
            if (showDebugLogs)
            {
                Debug.Log($"🎯 InSwingDelivery: Swing direction calculated - Force: {swingForce:F2}");
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
        /// Reset in swing delivery for new ball
        /// </summary>
        public void ResetDelivery()
        {
            if (showDebugLogs)
            {
                Debug.Log("🎯 InSwingDelivery: Reset for new ball");
            }
        }
        
        /// <summary>
        /// Get in swing delivery info
        /// </summary>
        public string GetDeliveryInfo()
        {
            return "In Swing Delivery - Curves left towards batsman";
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
                Debug.Log($"🎯 InSwingDelivery: Updated settings - Min: {minSwing}, Max: {maxSwing}, Base: {baseForce}");
            }
        }
    }
}
