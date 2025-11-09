using UnityEngine;

namespace CricketGame
{
	/// <summary>
	/// Out Swing Delivery - Ball curves away from the batsman
	/// 🎯 WORKS FROM ANY SPAWN POINT - Uses relative lateral directions
	/// Mirrors InswingDelivery but uses positive swing values and right-side lateral offset
	/// </summary>
	public class OutswingDelivery : MonoBehaviour
	{
		[Header("Out Swing Settings")]
		[Tooltip("Enable/disable out swing delivery")]
		public bool enableOutSwing = true;
		
		[Tooltip("Base swing force multiplier")]
		public float baseSwingForce = 1.0f;
		
		[Tooltip("Minimum swing at speed 9 (low speed = less swing)")]
		public float minSwingAtSpeed9 = 0.2f; // positive for outswing
		
		[Tooltip("Maximum swing at speed 16 (high speed = extreme swing)")]
		public float maxSwingAtSpeed16 = 1.35f; // positive for outswing
		
		[Header("Swing Direction")]
		[Tooltip("Direction of out swing (positive X = right, negative X = left)")]
		public Vector3 swingDirection = new Vector3(1f, 0f, 0f);
		
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
		public bool showCurvedPathInScene = false;
		
		void Start()
		{
			if (showDebugLogs)
			{
			}
		}
		
		public Vector3 CalculateTrajectory(Vector3 startPos, Vector3 targetPos, float ballSpeed)
		{
			if (!enableOutSwing)
				return targetPos;
			
			float swingForce = CalculateSwingForce(ballSpeed);
			Vector3 swingTarget = CalculateBezierCurveTarget(startPos, targetPos, swingForce);
			
			if (showDebugLogs)
			{
			}
			
			return swingTarget;
		}
		
		private Vector3 CalculateBezierCurveTarget(Vector3 startPos, Vector3 targetPos, float swingForce)
		{
			// 🎯 DYNAMIC DIRECTION: Calculate based on actual spawn-to-target direction
			Vector3 direction = (targetPos - startPos).normalized;
			float distance = Vector3.Distance(startPos, targetPos);
			
			Vector3 midPoint = Vector3.Lerp(startPos, targetPos, 0.5f);
			// 🎯 CRITICAL: Cross(direction, up) gives RIGHT relative to bowling direction
			// This works from ANY spawn point orientation!
			Vector3 right = Vector3.Cross(direction, Vector3.up).normalized; // right relative to bowling direction
			float swing = swingForce * baseSwingForce;
			float lateralMeters = swing * curveIntensity * (distance * bendDistanceScale);
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
			if (!enableOutSwing || !enableCurvedPath)
				return Vector3.Lerp(startPos, targetPos, t);
			
			float swingForce = CalculateSwingForce(ballSpeed);
			Vector3 dir = (targetPos - startPos).normalized;
			Vector3 right = Vector3.Cross(dir, Vector3.up).normalized;
			float distance = Vector3.Distance(startPos, targetPos);
			float swing = swingForce * baseSwingForce;
			float lateralMeters = swing * curveIntensity * (distance * bendDistanceScale);
			Vector3 controlPoint = Vector3.Lerp(startPos, targetPos, 0.5f) + right * lateralMeters;
			
			return CalculateBezierPoint(startPos, controlPoint, targetPos, t);
		}
		
		public Vector3[] GetCurvedPathPoints(Vector3 startPos, Vector3 targetPos, float ballSpeed, int segments = 30)
		{
			if (!enableOutSwing || !enableCurvedPath)
			{
				Vector3[] straight = new Vector3[Mathf.Max(2, segments + 1)];
				for (int i = 0; i < straight.Length; i++)
				{
					float t = (float)i / (straight.Length - 1);
					straight[i] = Vector3.Lerp(startPos, targetPos, t);
				}
				
				if (showDebugLogs)
				{
				}
				
				return straight;
			}
			
			// 🎯 DYNAMIC PATH CALCULATION: Works from ANY spawn point
			float swingForce = CalculateSwingForce(ballSpeed);
			
			// Calculate bowling direction from actual spawn-to-target positions
			Vector3 dir = (targetPos - startPos).normalized;
			
			// 🎯 CRITICAL: Calculate RIGHT direction relative to bowling direction
			// Cross(dir, up) = perpendicular RIGHT direction (works from any orientation!)
			Vector3 right = Vector3.Cross(dir, Vector3.up).normalized;
			
			float distance = Vector3.Distance(startPos, targetPos);
			float swing = swingForce * baseSwingForce;
			float lateralMeters = swing * curveIntensity * (distance * bendDistanceScale);
			Vector3 controlPoint = Vector3.Lerp(startPos, targetPos, 0.5f) + right * lateralMeters;
			
			if (showDebugLogs)
			{

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
			if (!enableOutSwing)
				return (targetPos - startPos).normalized;
			
			float swingForce = CalculateSwingForce(ballSpeed);
			Vector3 baseDirection = (targetPos - startPos).normalized;
			Vector3 swingDir = baseDirection + new Vector3(swingForce * 0.3f, 0, 0); // rightwards
			
			if (showDebugLogs)
			{
			}
			
			return swingDir.normalized;
		}
		
		private float CalculateSwingForce(float speed)
		{
			float normalizedSpeed = Mathf.InverseLerp(9f, 16f, speed);
			float swingForce = Mathf.Lerp(minSwingAtSpeed9, maxSwingAtSpeed16, normalizedSpeed);
			return swingForce;
		}
		
		public bool IsCurvedPathEnabled()
		{
			return enableOutSwing && enableCurvedPath;
		}
		
		public void ResetDelivery()
		{
			if (showDebugLogs)
			{
			}
		}
		
		public string GetDeliveryInfo()
		{
			return "Out Swing Delivery - Curves away from batsman";
		}
		
		public void UpdateSwingSettings(float minSwing, float maxSwing, float baseForce)
		{
			minSwingAtSpeed9 = minSwing;
			maxSwingAtSpeed16 = maxSwing;
			baseSwingForce = baseForce;
			
			if (showDebugLogs)
			{
			}
		}
		
		void OnDrawGizmos()
		{
			if (!showCurvedPathInScene || !enableOutSwing || !enableCurvedPath)
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
