using UnityEngine;

namespace CricketGame
{
    /// <summary>
    /// In Swing Delivery - Ball curves in towards the batsman
    /// 🎯 WORKS FROM ANY SPAWN POINT - Uses relative lateral directions
    /// Simplified implementation similar to SeamInDelivery but with curved path following
    /// </summary>
    public class InswingDelivery : MonoBehaviour
    {
        [Header("In Swing Settings")]
        [Tooltip("Enable/disable in swing delivery")]
        public bool enableInSwing = true;
        
        [Tooltip("Base swing force multiplier")]
        public float baseSwingForce = 1.0f;
        
        [Tooltip("Minimum swing at speed 9 (low speed = less swing)")]
        public float minSwingAtSpeed9 = -0.2f;
        
        [Tooltip("Maximum swing at speed 16 (high speed = extreme swing)")]
        public float maxSwingAtSpeed16 = -1.35f;
        
        [Header("Swing Direction")]
        [Tooltip("Direction of in swing (negative X = left, positive X = right)")]
        public Vector3 swingDirection = new Vector3(-1f, 0f, 0f);
        
        [Header("Curved Path Settings")]
        [Tooltip("Enable curved path following (ball follows Bezier curve)")]
        public bool enableCurvedPath = true;
        
        [Tooltip("Curve intensity multiplier")]
        public float curveIntensity = 1.0f;

        [Header("Vertical Arc (Elevation)")]
        [Tooltip("Vertical arc added while following the curve (0.5-1.5 for realistic cricket arc)")]
        public float pathArcHeight = 0.8f; // Realistic cricket bowling arc

        [Header("Lateral Bend")]
        [Tooltip("How much of the start→target distance to use for sideways bend. Lower = less swing.")]
        public float bendDistanceScale = 0.10f; // 10% of distance by default
        
        [Header("Debug")]
        public bool showDebugLogs = true;
        public bool showCurvedPathInScene = false; // default off per user request
        
		void Start()
		{
		}
        
        /// <summary>
        /// Calculate in swing trajectory (curves in towards batsman)
        /// </summary>
        public Vector3 CalculateTrajectory(Vector3 startPos, Vector3 targetPos, float ballSpeed)
        {
            if (!enableInSwing)
                return targetPos;
                
            // Calculate swing force based on speed (9 = less swing, 16 = extreme swing)
            float swingForce = CalculateSwingForce(ballSpeed);
            
            // Create a curved path using Bezier curve control points
            Vector3 swingTarget = CalculateBezierCurveTarget(startPos, targetPos, swingForce);
            
            return swingTarget;
        }
        
        /// <summary>
        /// Calculate Bezier curve target for smooth in swing trajectory
        /// 🎯 WORKS FROM ANY SPAWN POINT - Uses relative directions
        /// </summary>
        private Vector3 CalculateBezierCurveTarget(Vector3 startPos, Vector3 targetPos, float swingForce)
        {
            // Calculate distance and direction
            Vector3 direction = (targetPos - startPos).normalized;
            float distance = Vector3.Distance(startPos, targetPos);
            
            // 🎯 DYNAMIC LATERAL: Calculate left direction relative to bowling direction
            // This works from ANY spawn point orientation
            Vector3 midPoint = Vector3.Lerp(startPos, targetPos, 0.5f);
            Vector3 left = Vector3.Cross(Vector3.up, direction).normalized; // left relative to bowling direction
            float swing = swingForce * baseSwingForce; // apply base multiplier
            float lateralMeters = swing * curveIntensity * (distance * bendDistanceScale);
            Vector3 controlPoint = midPoint + left * lateralMeters;
            
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
        /// Get curved path point for ball to follow during movement
        /// 🎯 WORKS FROM ANY SPAWN POINT - Uses relative directions
        /// </summary>
        public Vector3 GetCurvedPathPoint(Vector3 startPos, Vector3 targetPos, float ballSpeed, float t)
        {
            if (!enableInSwing || !enableCurvedPath)
                return Vector3.Lerp(startPos, targetPos, t);
                
            float swingForce = CalculateSwingForce(ballSpeed);
            
            // 🎯 DYNAMIC LATERAL: Calculate left direction relative to bowling direction
            Vector3 dir = (targetPos - startPos).normalized;
            Vector3 left = Vector3.Cross(Vector3.up, dir).normalized; // Left relative to bowling direction
            float distance = Vector3.Distance(startPos, targetPos);
            float swing = swingForce * baseSwingForce;
            float lateralMeters = swing * curveIntensity * (distance * bendDistanceScale);
            Vector3 controlPoint = Vector3.Lerp(startPos, targetPos, 0.5f) + left * lateralMeters;
            
            // Calculate point on Bezier curve at time t
            Vector3 curvePoint = CalculateBezierPoint(startPos, controlPoint, targetPos, t);
            
            return curvePoint;
        }

        /// <summary>
        /// Generate a set of curved path points from start to target.
        /// Useful for path-following components.
        /// </summary>
        public Vector3[] GetCurvedPathPoints(Vector3 startPos, Vector3 targetPos, float ballSpeed, int segments = 30)
        {
            if (!enableInSwing || !enableCurvedPath)
            {
                // Return straight line segments if curved path disabled
                Vector3[] straight = new Vector3[Mathf.Max(2, segments + 1)];
                for (int i = 0; i < straight.Length; i++)
                {
                    float t = (float)i / (straight.Length - 1);
                    straight[i] = Vector3.Lerp(startPos, targetPos, t);
                }
                
                return straight;
            }

            // 🎯 DYNAMIC PATH CALCULATION: Works from ANY spawn point
            float swingForce = CalculateSwingForce(ballSpeed);
            
            // Calculate bowling direction from actual spawn-to-target positions
            Vector3 dir = (targetPos - startPos).normalized;
            
            // 🎯 CRITICAL: Calculate LEFT direction relative to bowling direction
            // Cross(up, dir) = perpendicular LEFT direction (works from any orientation!)
            Vector3 left = Vector3.Cross(Vector3.up, dir).normalized;
            
            float distance = Vector3.Distance(startPos, targetPos);
            float swing = swingForce * baseSwingForce;
            float lateralMeters = swing * curveIntensity * (distance * bendDistanceScale);
            Vector3 controlPoint = Vector3.Lerp(startPos, targetPos, 0.5f) + left * lateralMeters;

            int count = Mathf.Max(2, segments + 1);
            Vector3[] points = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / (count - 1);
                points[i] = CalculateBezierPoint(startPos, controlPoint, targetPos, t);
            }
            return points;
        }
        
        /// <summary>
        /// Get in swing direction for trajectory calculation
        /// 🎯 WORKS FROM ANY SPAWN POINT - Uses relative lateral direction
        /// </summary>
        public Vector3 GetDeliveryDirection(Vector3 startPos, Vector3 targetPos, float ballSpeed)
        {
            if (!enableInSwing)
                return (targetPos - startPos).normalized;
                
            float swingForce = CalculateSwingForce(ballSpeed);
            Vector3 baseDirection = (targetPos - startPos).normalized;
            
            // 🎯 CRITICAL FIX: Calculate LEFT direction relative to bowling direction
            // This works from ANY spawn point, not just hardcoded X-axis!
            Vector3 leftDirection = Vector3.Cross(Vector3.up, baseDirection).normalized;
            
            // Add leftward curve to the direction
            Vector3 swingDirection = baseDirection + leftDirection * swingForce * 0.3f;
            
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
        /// Check if curved path is enabled
        /// </summary>
        public bool IsCurvedPathEnabled()
        {
            return enableInSwing && enableCurvedPath;
        }
        
        /// <summary>
        /// Reset in swing delivery for new ball
        /// </summary>
        public void ResetDelivery()
        {
			
        }
        
        /// <summary>
        /// Get in swing delivery info
        /// </summary>
        public string GetDeliveryInfo()
        {
            return "In Swing Delivery - Curves in towards batsman";
        }
        
        /// <summary>
        /// Update swing settings from UI
        /// </summary>
        public void UpdateSwingSettings(float minSwing, float maxSwing, float baseForce)
        {
            minSwingAtSpeed9 = minSwing;
            maxSwingAtSpeed16 = maxSwing;
            baseSwingForce = baseForce;
            
			
        }
        
        /// <summary>
        /// Draw curved path in scene view for debugging
        /// </summary>
        void OnDrawGizmos()
        {
            if (!showCurvedPathInScene || !enableInSwing || !enableCurvedPath)
                return;
                
            // Find bowling controller to get start and target positions
            BowlingController bowlingController = FindObjectOfType<BowlingController>();
            if (bowlingController == null)
                return;
                
            // Get ball spawn point and target from bowling controller fields
            Transform ballSpawnPoint = null;
            Transform target = null;
            
            // Try to get spawn point and target from bowling controller
            var spawnPointField = typeof(BowlingController).GetField("ballSpawnPoint", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var targetField = typeof(BowlingController).GetField("target", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
            if (spawnPointField != null)
                ballSpawnPoint = spawnPointField.GetValue(bowlingController) as Transform;
            if (targetField != null)
                target = targetField.GetValue(bowlingController) as Transform;
            
            if (ballSpawnPoint == null || target == null)
                return;
                
            Vector3 startPos = ballSpawnPoint.position;
            Vector3 targetPos = target.position;
            float ballSpeed = 12f; // Default speed for visualization
            
            // Draw the curved path
            Gizmos.color = Color.red;
            int segments = 20;
            for (int i = 0; i < segments; i++)
            {
                float t1 = (float)i / segments;
                float t2 = (float)(i + 1) / segments;
                
                Vector3 point1 = GetCurvedPathPoint(startPos, targetPos, ballSpeed, t1);
                Vector3 point2 = GetCurvedPathPoint(startPos, targetPos, ballSpeed, t2);
                
                Gizmos.DrawLine(point1, point2);
            }
            
            // Draw control points
            Gizmos.color = Color.yellow;
            Vector3 midPoint = Vector3.Lerp(startPos, targetPos, 0.5f);
            float swingForce = CalculateSwingForce(ballSpeed);
            Vector3 leftOffset = new Vector3(-swingForce * curveIntensity * 3f, 0, 0);
            Vector3 controlPoint = midPoint + leftOffset;
            
            Gizmos.DrawWireSphere(controlPoint, 0.2f);
            Gizmos.DrawLine(startPos, controlPoint);
            Gizmos.DrawLine(controlPoint, targetPos);
            
            // Draw start and end points
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(startPos, 0.3f);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(targetPos, 0.3f);
        }
    }
}