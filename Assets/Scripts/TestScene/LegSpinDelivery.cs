using UnityEngine;

namespace CricketGame
{
	/// <summary>
	/// Leg Spin Delivery - Ball curves away from the batsman
	/// Mirrors OutswingDelivery but uses different variable names
	/// </summary>
	public class LegSpinDelivery : MonoBehaviour
	{
        [Header("Leg Spin Settings")]
        [Tooltip("Enable/disable leg spin delivery")]
        public bool enableLegSpin = true;

        [Tooltip("Base spin force multiplier")]
        public float baseSpinForce = 1.0f;

        [Tooltip("Minimum spin at speed 9 (low speed = less spin)")]
        public float minSpinAtSpeed9 = 0.2f; // positive for leg spin

        [Tooltip("Maximum spin at speed 16 (high speed = extreme spin)")]
        public float maxSpinAtSpeed16 = 1.35f; // positive for leg spin

        [Header("Spin Direction")]
        [Tooltip("Direction of leg spin (positive X = right, negative X = left)")]
        public Vector3 spinDirection = new Vector3(1f, 0f, 0f);

        [Header("Speed Resistance")]
        [Tooltip("Resistance factor for leg spin - reduces ball speed while keeping spin force same (0.0 = no resistance, 1.0 = max resistance)")]
        [Range(0f, 1f)]
        public float speedResistanceFactor = 0.3f; // 30% speed reduction by default

        [Header("Curved Path Settings")]
        [Tooltip("Enable curved path following (ball follows Bezier curve)")]
        public bool enableCurvedPath = true;

        [Tooltip("Curve intensity multiplier")]
        public float curveIntensity = 1.0f;

        [Header("Vertical Arc (Elevation)")]
        [Tooltip("Vertical arc added while following the curve (lower this to reduce elevation)")]
        public float pathArcHeight = 0.05f;

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
                Debug.Log("🎯 LegSpinDelivery: Ready for leg spin deliveries");
            }
        }

        public Vector3 CalculateTrajectory(Vector3 startPos, Vector3 targetPos, float ballSpeed)
        {
            if (!enableLegSpin)
                return targetPos;

            float spinForce = CalculateSpinForce(ballSpeed);
            Vector3 spinTarget = CalculateBezierCurveTarget(startPos, targetPos, spinForce);

            if (showDebugLogs)
            {
                Debug.Log($"🎯 LegSpinDelivery: Calculated curved trajectory - Force: {spinForce:F2}, Speed: {ballSpeed:F1}");
            }

            return spinTarget;
        }

        private Vector3 CalculateBezierCurveTarget(Vector3 startPos, Vector3 targetPos, float spinForce)
        {
            Vector3 direction = (targetPos - startPos).normalized;
            float distance = Vector3.Distance(startPos, targetPos);

            Vector3 midPoint = Vector3.Lerp(startPos, targetPos, 0.5f);
            Vector3 right = Vector3.Cross(direction, Vector3.up).normalized; // right relative to path
            float spin = spinForce * baseSpinForce;
            float lateralMeters = spin * curveIntensity * (distance * bendDistanceScale);
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
            if (!enableLegSpin || !enableCurvedPath)
                return Vector3.Lerp(startPos, targetPos, t);

            float spinForce = CalculateSpinForce(ballSpeed);
            Vector3 dir = (targetPos - startPos).normalized;
            Vector3 right = Vector3.Cross(dir, Vector3.up).normalized;
            float distance = Vector3.Distance(startPos, targetPos);
            float spin = spinForce * baseSpinForce;
            float lateralMeters = spin * curveIntensity * (distance * bendDistanceScale);
            Vector3 controlPoint = Vector3.Lerp(startPos, targetPos, 0.5f) + right * lateralMeters;

            return CalculateBezierPoint(startPos, controlPoint, targetPos, t);
        }

        public Vector3[] GetCurvedPathPoints(Vector3 startPos, Vector3 targetPos, float ballSpeed, int segments = 30)
        {
            if (!enableLegSpin || !enableCurvedPath)
            {
                Vector3[] straight = new Vector3[Mathf.Max(2, segments + 1)];
                for (int i = 0; i < straight.Length; i++)
                {
                    float t = (float)i / (straight.Length - 1);
                    straight[i] = Vector3.Lerp(startPos, targetPos, t);
                }
                return straight;
            }

            float spinForce = CalculateSpinForce(ballSpeed);
            Vector3 dir = (targetPos - startPos).normalized;
            Vector3 right = Vector3.Cross(dir, Vector3.up).normalized;
            float distance = Vector3.Distance(startPos, targetPos);
            float spin = spinForce * baseSpinForce;
            float lateralMeters = spin * curveIntensity * (distance * bendDistanceScale);
            Vector3 controlPoint = Vector3.Lerp(startPos, targetPos, 0.5f) + right * lateralMeters;

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
            if (!enableLegSpin)
                return (targetPos - startPos).normalized;

            float spinForce = CalculateSpinForce(ballSpeed);
            Vector3 baseDirection = (targetPos - startPos).normalized;
            Vector3 spinDir = baseDirection + new Vector3(spinForce * 0.3f, 0, 0); // rightwards

            if (showDebugLogs)
            {
                Debug.Log($"🎯 LegSpinDelivery: Spin direction calculated - Force: {spinForce:F2}");
            }

            return spinDir.normalized;
        }

        private float CalculateSpinForce(float speed)
        {
            float normalizedSpeed = Mathf.InverseLerp(9f, 16f, speed);
            float spinForce = Mathf.Lerp(minSpinAtSpeed9, maxSpinAtSpeed16, normalizedSpeed);
            return spinForce;
        }

        /// <summary>
        /// Applies speed resistance to leg spin deliveries - reduces ball speed while keeping spin force same
        /// </summary>
        public float ApplySpeedResistance(float originalSpeed)
        {
            float resistanceMultiplier = 1f - speedResistanceFactor;
            float effectiveSpeed = originalSpeed * resistanceMultiplier;
            
            if (showDebugLogs)
            {
                Debug.Log($"🎯 LegSpinDelivery: Speed resistance applied - Original: {originalSpeed:F1}, Effective: {effectiveSpeed:F1}, Resistance: {speedResistanceFactor:P0}");
            }
            
            return effectiveSpeed;
        }

        public bool IsCurvedPathEnabled()
        {
            return enableLegSpin && enableCurvedPath;
        }

        public void ResetDelivery()
        {
            if (showDebugLogs)
            {
                Debug.Log("🎯 LegSpinDelivery: Reset for new ball");
            }
        }

        public string GetDeliveryInfo()
        {
            return "Leg Spin Delivery - Curves away from batsman";
        }

        public void UpdateSpinSettings(float minSpin, float maxSpin, float baseForce)
        {
            minSpinAtSpeed9 = minSpin;
            maxSpinAtSpeed16 = maxSpin;
            baseSpinForce = baseForce;

            if (showDebugLogs)
            {
                Debug.Log($"🎯 LegSpinDelivery: Updated settings - Min: {minSpin}, Max: {maxSpin}, Base: {baseForce}");
            }
        }

        void OnDrawGizmos()
        {
            if (!showCurvedPathInScene || !enableLegSpin || !enableCurvedPath)
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

            Gizmos.color = Color.cyan;
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