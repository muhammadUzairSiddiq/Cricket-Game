using UnityEngine;

namespace CricketGame
{
	/// <summary>
	/// Attach to the Target GameObject (requires a Collider set to IsTrigger).
	/// Notifies the BowlingController when the ball touches the target so leg spin deflection
	/// can be applied exactly on contact.
	/// </summary>
	[RequireComponent(typeof(Collider))]
	public class TargetHitTrigger : MonoBehaviour
	{
		[SerializeField] private BowlingController bowlingController;
		[SerializeField] private string ballTag = "Ball"; // optional tag filter
		[SerializeField] private bool showDebugLogs = true;

		void Reset()
		{
			Collider col = GetComponent<Collider>();
			if (col != null) col.isTrigger = true;
		}

		void OnValidate()
		{
			Collider col = GetComponent<Collider>();
			if (col != null) col.isTrigger = true;
		}

		void OnTriggerEnter(Collider other)
		{
			// Optional tag filter if user sets the ball's tag
			if (!string.IsNullOrEmpty(ballTag) && other.CompareTag(ballTag) == false)
			{
				// If no tag match, still allow if a Rigidbody exists (fallback)
			}

			Rigidbody rb = other.attachedRigidbody;
			if (rb == null) return;

			if (bowlingController != null)
			{
				bowlingController.OnTargetTouched(rb);
				if (showDebugLogs)
				{
				}
			}
			else if (showDebugLogs)
			{
			}
		}
	}
}

