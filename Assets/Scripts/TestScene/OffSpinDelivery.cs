using UnityEngine;

namespace CricketGame
{
	/// <summary>
	/// Off Spin Delivery - Ball can follow curved or straight path with spin effect
	/// Set enableCurvedPath = false for straight line delivery
	/// Use context menu "Disable Curved Path (Use Straight Line)" to enable straight path
	/// </summary>
	public class OffSpinDelivery : MonoBehaviour
	{
        [Header("Off Spin Settings")]
        [Tooltip("Enable/disable off spin delivery")]
        public bool enableOffSpin = true;
        
        [Header("Post-Bounce Spin Effect")]
        [Tooltip("Enable lateral spin movement AFTER ball bounces on pitch (realistic off spin physics)")]
        public bool enablePostBounceSpinEffect = true;
        
        [Tooltip("Multiplier for lateral spin strength after bounce (POSITIVE = spin right, NEGATIVE = spin left, higher magnitude = more sideways movement)")]
        [Range(-2.0f, 2.0f)]
        public float postBounceSpinStrength = -0.5f; // Negative for off spin (left)

        [Header("Curved Path Settings")]
        [Tooltip("Enable curved path following (ball follows Bezier curve) - DISABLED for straight line with swing")]
        public bool enableCurvedPath = false; // Changed to false for straight path with swing effect

        [Tooltip("Curve intensity multiplier")]
        public float curveIntensity = 1.0f;

        [Header("Vertical Arc (Elevation)")]
        [Tooltip("Vertical arc added while following the curve (0.5-1.5 for realistic cricket arc)")]
        public float pathArcHeight = 0.8f; // Realistic cricket bowling arc

        [Header("Lateral Bend")]
        [Tooltip("How much of the start→target distance to use for sideways bend. Lower = less spin.")]
        public float bendDistanceScale = 0.10f; // 10% of distance by default

        [Header("Debug")]
        public bool showDebugLogs = true;
        public bool showCurvedPathInScene = false;

        void Start()
        {
            if (showDebugLogs)
            {
                Debug.Log("🎯 OffSpinDelivery: Ready for off spin deliveries");
            }
        }

        public Vector3 CalculateTrajectory(Vector3 startPos, Vector3 targetPos, float ballSpeed)
        {
            if (!enableOffSpin)
                return targetPos;

            // If curved path is disabled, return straight trajectory
            if (!enableCurvedPath)
            {
                if (showDebugLogs)
                {
                    Debug.Log($"🎯 OffSpinDelivery: Straight trajectory - Speed: {ballSpeed:F1} m/s");
                }
                return targetPos;
            }

            // Simple curved path for visual effect (not used for post-bounce spin)
            Vector3 spinTarget = CalculateBezierCurveTarget(startPos, targetPos, 1.0f);

            if (showDebugLogs)
            {
                Debug.Log($"🎯 OffSpinDelivery: Calculated curved trajectory - Speed: {ballSpeed:F1}");
            }

            return spinTarget;
        }

        private Vector3 CalculateBezierCurveTarget(Vector3 startPos, Vector3 targetPos, float spinForce)
        {
            Vector3 direction = (targetPos - startPos).normalized;
            float distance = Vector3.Distance(startPos, targetPos);

            Vector3 midPoint = Vector3.Lerp(startPos, targetPos, 0.5f);
            Vector3 right = Vector3.Cross(direction, Vector3.up).normalized; // right relative to path
            float lateralMeters = spinForce * curveIntensity * (distance * bendDistanceScale);
            Vector3 controlPoint = midPoint + right * lateralMeters;

            return CalculateBezierPoint(startPos, controlPoint, targetPos, 0.8f);
        }

        private Vector3 CalculateBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            Vector3 p = uuu * p0;
            p += 3 * uu * t * p1;
            p += 3 * u * tt * p2;
            p += ttt * p2;

            return p;
        }

        public Vector3 GetCurvedPathPoint(Vector3 startPos, Vector3 targetPos, float ballSpeed, float t)
        {
            if (!enableOffSpin || !enableCurvedPath)
                return Vector3.Lerp(startPos, targetPos, t);

            // Simple curved path calculation
            Vector3 dir = (targetPos - startPos).normalized;
            Vector3 right = Vector3.Cross(dir, Vector3.up).normalized;
            float distance = Vector3.Distance(startPos, targetPos);
            float lateralMeters = curveIntensity * (distance * bendDistanceScale);
            Vector3 controlPoint = Vector3.Lerp(startPos, targetPos, 0.5f) + right * lateralMeters;

            return CalculateBezierPoint(startPos, controlPoint, targetPos, t);
        }

        public Vector3[] GetCurvedPathPoints(Vector3 startPos, Vector3 targetPos, float ballSpeed, int segments = 30)
        {
            if (!enableOffSpin || !enableCurvedPath)
            {
                Vector3[] straight = new Vector3[Mathf.Max(2, segments + 1)];
                for (int i = 0; i < straight.Length; i++)
                {
                    float t = (float)i / (straight.Length - 1);
                    straight[i] = Vector3.Lerp(startPos, targetPos, t);
                }
                
                if (showDebugLogs)
                {
                    Debug.Log($"🎯 OFF SPIN PATH: Straight path - {straight.Length} points from {startPos} to {targetPos}");
                }
                
                return straight;
            }

            // 🎯 DYNAMIC PATH CALCULATION: Works from ANY spawn point
            // Calculate bowling direction from actual spawn-to-target positions
            Vector3 dir = (targetPos - startPos).normalized;
            
            // 🎯 CRITICAL: Calculate RIGHT direction relative to bowling direction
            // Cross(dir, up) = perpendicular RIGHT direction (works from any orientation!)
            Vector3 right = Vector3.Cross(dir, Vector3.up).normalized;
            
            float distance = Vector3.Distance(startPos, targetPos);
            float lateralMeters = curveIntensity * (distance * bendDistanceScale);
            Vector3 controlPoint = Vector3.Lerp(startPos, targetPos, 0.5f) + right * lateralMeters;

            if (showDebugLogs)
            {
                Debug.Log($"🎯 OFF SPIN PATH GENERATION:");
                Debug.Log($"   Start: {startPos}, Target: {targetPos}");
                Debug.Log($"   Bowling Direction: {dir}");
                Debug.Log($"   Lateral Right: {right}");
                Debug.Log($"   Control Point Offset: {lateralMeters:F2}m");
                Debug.Log($"   ✅ Directions calculated DYNAMICALLY - works from ANY spawn point!");
            }

            int count = Mathf.Max(2, segments + 1);
            Vector3[] points = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / (count - 1);
                points[i] = CalculateBezierPoint(startPos, controlPoint, targetPos, t);
            }
            return points;
        }

        public Vector3 GetDeliveryDirection(Vector3 startPos, Vector3 targetPos, float ballSpeed)
        {
            if (!enableOffSpin)
                return (targetPos - startPos).normalized;

            // Always return straight direction (post-bounce spin handles lateral movement)
            Vector3 straightDirection = (targetPos - startPos).normalized;
            if (showDebugLogs)
            {
                Debug.Log($"🎯 OffSpinDelivery: Straight direction - Spin occurs after bounce");
            }
            return straightDirection;
        }


        public bool IsCurvedPathEnabled()
        {
            return enableOffSpin && enableCurvedPath;
        }

        public void ResetDelivery()
        {
            if (showDebugLogs)
            {
                Debug.Log("🎯 OffSpinDelivery: Reset for new ball");
            }
        }

        public string GetDeliveryInfo()
        {
            string pathInfo = enableCurvedPath ? "Curved path" : "Straight path";
            string spinInfo = "";
            
            if (enablePostBounceSpinEffect)
            {
                if (postBounceSpinStrength > 0)
                    spinInfo = " with post-bounce spin RIGHT (Leg Spin)";
                else if (postBounceSpinStrength < 0)
                    spinInfo = " with post-bounce spin LEFT (Off Spin)";
                else
                    spinInfo = " with no post-bounce spin";
            }
            
            return $"Off Spin Delivery - {pathInfo}{spinInfo}";
        }

        public void UpdateSpinStrength(float strength)
        {
            postBounceSpinStrength = Mathf.Clamp(strength, -2.0f, 2.0f);
            
            if (showDebugLogs)
            {
                string direction = strength > 0 ? "RIGHT" : strength < 0 ? "LEFT" : "NONE";
                Debug.Log($"🎯 OffSpinDelivery: Updated spin strength to {strength:F2} ({direction})");
            }
        }

        /// <summary>
        /// Context menu option to force disable curved path for straight line delivery
        /// </summary>
        [ContextMenu("Disable Curved Path (Use Straight Line)")]
        void ForceStraightPath()
        {
            enableCurvedPath = false;
            Debug.Log("🎯 OffSpinDelivery: Curved path DISABLED - Ball will now follow straight line!");
            Debug.Log($"🎯 OffSpinDelivery: IsCurvedPathEnabled = {IsCurvedPathEnabled()}");
        }
        
        /// <summary>
        /// Context menu option to enable curved path
        /// </summary>
        [ContextMenu("Enable Curved Path")]
        void ForceCurvedPath()
        {
            enableCurvedPath = true;
            Debug.Log("🎯 OffSpinDelivery: Curved path ENABLED - Ball will follow Bezier curve!");
            Debug.Log($"🎯 OffSpinDelivery: IsCurvedPathEnabled = {IsCurvedPathEnabled()}");
        }
        
        /// <summary>
        /// Context menu to check current path mode
        /// </summary>
        [ContextMenu("Check Current Path Mode")]
        void CheckPathMode()
        {
            Debug.Log($"🎯 OffSpinDelivery Path Mode:");
            Debug.Log($"   - Enable Off Spin: {enableOffSpin}");
            Debug.Log($"   - Enable Curved Path: {enableCurvedPath}");
            Debug.Log($"   - Is Curved Path Enabled: {IsCurvedPathEnabled()}");
            Debug.Log($"   - Mode: {(IsCurvedPathEnabled() ? "CURVED PATH (Bezier)" : "STRAIGHT PATH")}");
            Debug.Log($"");
            Debug.Log($"🎯 OffSpinDelivery Post-Bounce Spin Settings:");
            Debug.Log($"   - Enable Post-Bounce Spin: {enablePostBounceSpinEffect}");
            Debug.Log($"   - Spin Strength: {postBounceSpinStrength:F2}");
            string spinDir = postBounceSpinStrength > 0 ? "RIGHT →" : postBounceSpinStrength < 0 ? "← LEFT" : "NONE";
            Debug.Log($"   - Spin Direction: {spinDir}");
        }
        
        /// <summary>
        /// Context menu to test off spin configuration
        /// </summary>
        [ContextMenu("Show Complete Off Spin Configuration")]
        void ShowCompleteConfiguration()
        {
            Debug.Log("═══════════════════════════════════════════════════════");
            Debug.Log("🎯 OFF SPIN DELIVERY - COMPLETE CONFIGURATION");
            Debug.Log("═══════════════════════════════════════════════════════");
            Debug.Log($"");
            Debug.Log($"📍 PATH SETTINGS:");
            Debug.Log($"   ✓ Delivery Mode: {(IsCurvedPathEnabled() ? "CURVED PATH" : "STRAIGHT PATH")}");
            Debug.Log($"   ✓ Path follows: {(IsCurvedPathEnabled() ? "Bezier curve with lateral movement" : "Direct line to target")}");
            Debug.Log($"");
            string spinDirectionText = postBounceSpinStrength > 0 ? "RIGHT →" : postBounceSpinStrength < 0 ? "← LEFT" : "NONE";
            string spinTypeText = postBounceSpinStrength > 0 ? "Leg Spin (away from batsman)" : postBounceSpinStrength < 0 ? "Off Spin (towards batsman)" : "No Spin";
            Debug.Log($"⚡ POST-BOUNCE SPIN EFFECT:");
            Debug.Log($"   ✓ Enabled: {enablePostBounceSpinEffect}");
            Debug.Log($"   ✓ Spin Strength: {postBounceSpinStrength:F2}x");
            Debug.Log($"   ✓ Spin Direction: {spinDirectionText}");
            Debug.Log($"   ✓ Spin Type: {spinTypeText}");
            Debug.Log($"   ✓ Effect: Ball moves sideways AFTER bouncing on pitch");
            Debug.Log($"   ✓ Realistic Physics: Spin takes effect on bounce (like real cricket!)");
            Debug.Log($"");
            Debug.Log("═══════════════════════════════════════════════════════");
        }

        void OnDrawGizmos()
        {
            if (!showCurvedPathInScene || !enableOffSpin || !enableCurvedPath)
                return;

            BowlingController bowlingController = FindObjectOfType<BowlingController>();
            if (bowlingController == null)
                return;

            Transform ballSpawnPoint = null;
            Transform target = null;
            var spawnPointField = typeof(BowlingController).GetField("ballSpawnPoint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var targetField = typeof(BowlingController).GetField("target", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (spawnPointField != null) ballSpawnPoint = spawnPointField.GetValue(bowlingController) as Transform;
            if (targetField != null) target = targetField.GetValue(bowlingController) as Transform;
            if (ballSpawnPoint == null || target == null) return;

            Vector3 startPos = ballSpawnPoint.position;
            Vector3 targetPos = target.position;
            float ballSpeed = 12f;

            Gizmos.color = Color.magenta; // Different color for off spin visualization
            int segments = 20;
            for (int i = 0; i < segments; i++)
            {
                float t1 = (float)i / segments;
                float t2 = (float)(i + 1) / segments;
                Vector3 point1 = GetCurvedPathPoint(startPos, targetPos, ballSpeed, t1);
                Vector3 point2 = GetCurvedPathPoint(startPos, targetPos, ballSpeed, t2);
                Gizmos.DrawLine(point1, point2);
            }
        }
    }
}

