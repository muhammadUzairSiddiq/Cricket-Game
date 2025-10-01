using UnityEngine;

namespace CricketGame
{
	/// <summary>
	/// Leg Spin Delivery - approaches target similar to Seam In, then turns after pitching/target contact
	/// </summary>
	public class LegSpinDelivery : MonoBehaviour
	{
		[Header("Leg Spin Settings")]
		[SerializeField] private bool enableLegSpin = true;
		[Tooltip("Base spin force multiplier influencing curve into the pitch (pre-target)")]
		[SerializeField] private float baseSpinForce = 1.0f;
		[Tooltip("Minimum curve at low speed (speed 9)")]
		[SerializeField] private float minCurveAtSpeed9 = 0.2f;
		[Tooltip("Maximum curve at high speed (speed 16)")]
		[SerializeField] private float maxCurveAtSpeed16 = 2.5f;

		[Header("Post Target Turn")]
		[Tooltip("Yaw deflection in degrees applied after touching target/pitch. Negative turns to the right for leg spin.")]
		[SerializeField] private float postTargetDeflectionAngleDeg = -2f;

		[Header("Debug")]
		[SerializeField] private bool showDebugLogs = true;

		void Start()
		{
			if (showDebugLogs)
			{
				Debug.Log("🎯 LegSpinDelivery: Ready for leg spin deliveries");
			}
		}

		/// <summary>
		/// Calculate leg spin trajectory (pre-target) - FLAT/STRAIGHT to target
		/// </summary>
		public Vector3 CalculateTrajectory(Vector3 startPos, Vector3 targetPos, float ballSpeed)
		{
			// For leg spin, approach is straight like a flat/seam ball. No pre-target curve.
			return targetPos;
		}

		/// <summary>
		/// Direction used by the launcher when computing the shot
		/// </summary>
		public Vector3 GetDeliveryDirection(Vector3 startPos, Vector3 targetPos, float ballSpeed)
		{
			// Straight direction towards the target for pre-target flight
			return (targetPos - startPos).normalized;
		}

		/// <summary>
		/// Angle to apply after reaching target to create the leg-spin turn
		/// </summary>
		public float GetPostTargetDeflectionAngleDeg()
		{
			return postTargetDeflectionAngleDeg;
		}

		/// <summary>
		/// Reset for new ball
		/// </summary>
		public void ResetDelivery()
		{
			if (showDebugLogs)
			{
				Debug.Log("🎯 LegSpinDelivery: Reset for new ball");
			}
		}

		public string GetDeliveryInfo()
		{
			return "Leg Spin Delivery - Curves in, then turns after pitching";
		}

		private float CalculateCurveForce(float speed)
		{
			// Currently unused (kept for future spin shaping if needed)
			float normalizedSpeed = Mathf.InverseLerp(9f, 16f, speed);
			float curveForce = Mathf.Lerp(minCurveAtSpeed9, maxCurveAtSpeed16, normalizedSpeed) * baseSpinForce;
			return curveForce;
		}

		private Vector3 CalculateBezierCurveTarget(Vector3 startPos, Vector3 targetPos, float curveForce)
		{
			// Unused in current flat approach; kept for potential future use
			return targetPos;
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
	}
}


