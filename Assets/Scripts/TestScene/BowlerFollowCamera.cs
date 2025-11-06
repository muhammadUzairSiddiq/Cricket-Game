using UnityEngine;
using UnityEngine.SceneManagement;

namespace CricketGame
{
	/// <summary>
	/// Smooth, optimized camera follow for the active bowler.
	/// - Fixed center line: camera X can be locked to a field center so left/right bowler switches don't shift framing
	/// - Initial hard-lock: sticks to player initially, then transitions to smooth follow
	/// - Event-driven target updates via BowlerEvents, plus resilient auto-rescan when no event is fired
	/// - No per-frame allocations; defensive null checks
	/// </summary>
	[RequireComponent(typeof(Camera))]
	public class BowlerFollowCamera : MonoBehaviour
	{
		[Header("Follow Target")]
		[SerializeField] private Transform target; // Runtime assigned; can be set manually for testing
		[SerializeField] private bool tryFindInitialBowler = true;
		[SerializeField] private bool searchWholeSceneIfMissing = true;
		[SerializeField, Tooltip("Auto-rescan scene when target is missing (e.g., spawned from editor)")] private bool rescanIfMissing = true;
		[SerializeField, Tooltip("Seconds between rescans when missing target")] private float rescanInterval = 0.5f;
		[SerializeField, Tooltip("Tags that mark bowler prefabs in the scene; searched first for target")] private string[] bowlerTags = new[] { "Fast Bowler", "Spin Bowler", "Medium Pace Bowler" };

		[Header("Framing / Field Center")]
		[SerializeField, Tooltip("Lock camera X to this world value so it stays centered between stumps")] private bool lockXToCenter = true;
		[SerializeField, Tooltip("World X coordinate for center line")] private float centerLineX = 0f;

		[Header("Follow Settings")]
		[SerializeField] private Vector3 followOffset = new Vector3(0f, 3.5f, -6.5f);
		[SerializeField] private Vector3 lookAtOffset = new Vector3(0f, 1.5f, 0f);
		[SerializeField, Tooltip("Time to reach target position; lower = snappier, higher = smoother (only used when Stable Position Smoothing is off)")] private float positionDampTime = 0.18f;
		[SerializeField, Tooltip("Max SmoothDamp speed; limit large teleports (only used when Stable Position Smoothing is off)")] private float maxPositionSpeed = 40f;
		[SerializeField, Tooltip("Use exponential position smoothing for ultra-stable, jitter-free motion")] private bool stablePositionSmoothing = true;
		[SerializeField, Tooltip("Strength of exponential smoothing (higher = snappier)")] private float positionSmoothingStrength = 12f;
		[SerializeField, Tooltip("Rotation smoothing factor (deg/sec); 0 = no rotation smoothing")] private float rotationSmoothing = 10f;
		[SerializeField] private bool alignToLookAt = true;

		[Header("Initial Stickiness")] 
		[SerializeField, Tooltip("Seconds to hard-lock to the bowler before blending to smoothing")] private float initialHardLockDuration = 0.25f;
		[SerializeField, Tooltip("Seconds to blend from hard-lock to full smoothing after the lock period")] private float smoothBlendDuration = 0.3f;

		[Header("Performance")] 
		[SerializeField, Tooltip("Skip rotation smoothing to reduce math cost")] private bool simpleLook = false;
		[SerializeField, Tooltip("Disable updates if target becomes too far (saves CPU). 0 = disabled")] private float maxFollowDistance = 0f;

		// Internal state
		private Vector3 velocityRef; // SmoothDamp velocity (cached)
		private Transform cachedTransform;
		private float followEnableTime;
		private float nextRescanTime;
		private bool followPaused;

		private void OnEnable()
		{
			BowlerEvents.OnBowlerReady += HandleBowlerReady;
			BowlerEvents.OnBowlerStopFollow += HandleStopFollowing;
			cachedTransform = transform; // cache for speed
			SceneManager.activeSceneChanged += HandleSceneChanged;
		}

		private void OnDisable()
		{
			BowlerEvents.OnBowlerReady -= HandleBowlerReady;
			BowlerEvents.OnBowlerStopFollow -= HandleStopFollowing;
			SceneManager.activeSceneChanged -= HandleSceneChanged;
		}

		private void Start()
		{
			// Safe initial binding: try to find an already-present bowler (e.g., from previous scene)
			if (tryFindInitialBowler && target == null)
			{
				TryFindInitialBowler();
			}
			followEnableTime = Time.time;
		}

		private void Update()
		{
			// Check for P key to resume following (next ball)
			if (followPaused && Input.GetKeyDown(KeyCode.P))
			{
				ResumeFollowing();
				// Notify that next ball is ready (target will show again)
				BowlerEvents.NotifyNextBallReady();
			}
		}

		private void LateUpdate()
		{
			if (followPaused)
			{
				return; // Camera frozen - no movement
			}
			if (target == null)
			{
				// Rescan for a target if none (handles editor-instantiated prefabs without event)
				if (rescanIfMissing && searchWholeSceneIfMissing && Time.time >= nextRescanTime)
				{
					nextRescanTime = Time.time + rescanInterval;
					TryFindInitialBowler();
				}
				return; // Nothing to follow; zero work
			}

			// Optional culling by distance (performance on wide shots)
			if (maxFollowDistance > 0f)
			{
				float dist = (cachedTransform.position - target.position).sqrMagnitude;
				if (dist > maxFollowDistance * maxFollowDistance)
				{
					return;
				}
			}

			// Desired position based on target and offset
			Vector3 desiredPos = target.TransformPoint(followOffset);
			if (lockXToCenter)
			{
				desiredPos.x = centerLineX; // keep camera centered along the pitch
			}

			Vector3 newPos;

			// Initial hard lock then blend to smooth
			float timeSinceEnable = Time.time - followEnableTime;
			if (timeSinceEnable <= initialHardLockDuration)
			{
				newPos = desiredPos; // stick to player strictly
			}
			else if (timeSinceEnable <= initialHardLockDuration + smoothBlendDuration)
			{
				// progressively increase smoothing during blend window
				float t = Mathf.InverseLerp(initialHardLockDuration, initialHardLockDuration + smoothBlendDuration, timeSinceEnable);
				if (stablePositionSmoothing)
				{
					float strength = Mathf.Lerp(positionSmoothingStrength * 2f, positionSmoothingStrength, t);
					float a = 1f - Mathf.Exp(-strength * Time.deltaTime);
					newPos = Vector3.Lerp(cachedTransform.position, desiredPos, a);
				}
				else
				{
					float blendedDamp = Mathf.Lerp(0.02f, positionDampTime, t);
					newPos = Vector3.SmoothDamp(cachedTransform.position, desiredPos, ref velocityRef, blendedDamp, maxPositionSpeed, Time.deltaTime);
				}
			}
			else
			{
				if (stablePositionSmoothing)
				{
					float a = 1f - Mathf.Exp(-positionSmoothingStrength * Time.deltaTime);
					newPos = Vector3.Lerp(cachedTransform.position, desiredPos, a);
				}
				else
				{
					newPos = Vector3.SmoothDamp(cachedTransform.position, desiredPos, ref velocityRef, positionDampTime, maxPositionSpeed, Time.deltaTime);
				}
			}

			cachedTransform.position = newPos;

			if (alignToLookAt)
			{
				Vector3 lookTarget = target.position + lookAtOffset;
				if (lockXToCenter)
				{
					lookTarget.x = centerLineX; // keep look aligned with the center line as well
				}
				Vector3 toTarget = lookTarget - cachedTransform.position;
				if (toTarget.sqrMagnitude > 0.0001f)
				{
					Quaternion targetRot = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
					if (simpleLook || rotationSmoothing <= 0.0001f)
					{
						cachedTransform.rotation = targetRot;
					}
					else
					{
						float s = 1f - Mathf.Exp(-rotationSmoothing * Time.deltaTime);
						cachedTransform.rotation = Quaternion.Slerp(cachedTransform.rotation, targetRot, s);
					}
				}
			}
		}

		private void HandleBowlerReady(Transform newBowlerRoot)
		{
			SetTarget(newBowlerRoot);
		}

		private void HandleStopFollowing()
		{
			PauseFollowing();
		}

		private void HandleSceneChanged(Scene a, Scene b)
		{
			// Scene changes can kill references; let’s try to reacquire next frame
			nextRescanTime = 0f;
		}

		/// <summary>
		/// Manually set the follow target (safe at runtime).
		/// </summary>
		public void SetTarget(Transform newTarget)
		{
			target = newTarget;
			velocityRef = Vector3.zero; // reset damping to avoid drift
			followEnableTime = Time.time; // restart hard-lock phase
		}

		/// <summary>
		/// Stop following the current target immediately.
		/// </summary>
		public void StopFollowing()
		{
			target = null;
		}

		/// <summary>
		/// Pause following without losing the current target.
		/// </summary>
		public void PauseFollowing()
		{
			followPaused = true;
		}

		/// <summary>
		/// Resume following the current target.
		/// </summary>
		public void ResumeFollowing()
		{
			followPaused = false;
			followEnableTime = Time.time; // reapply initial lock/blend on resume
		}

		/// <summary>
		/// Trigger a rescan of the scene to find a bowler if none assigned.
		/// </summary>
		[ContextMenu("Rescan For Bowler")]
		public void RequestRetarget()
		{
			TryFindInitialBowler();
		}

		/// <summary>
		/// Attempts to find a bowler instance in scene preferring real bowler GameObjects over controllers.
		/// </summary>
		private void TryFindInitialBowler()
		{
			// Only use tags. Do NOT fall back to names or controllers.
			if (bowlerTags != null)
			{
				for (int t = 0; t < bowlerTags.Length; t++)
				{
					string tag = bowlerTags[t];
					if (string.IsNullOrEmpty(tag)) continue;
					try
					{
						var candidates = GameObject.FindGameObjectsWithTag(tag);
						for (int i = 0; i < candidates.Length; i++)
						{
							var go = candidates[i];
							if (go != null && go.activeInHierarchy)
							{
								SetTarget(go.transform);
								return;
							}
						}
					}
					catch (UnityException)
					{
						// Tag not defined in project; skip safely
					}
				}
			}

			// If we got here, no tagged bowler was found. Do not choose anything else.
			if (target == null)
			{
				Debug.LogWarning("BowlerFollowCamera: No bowler with required tags found (Fast Bowler / Spin Bowler / Medium Pace Bowler). Camera will wait for event.");
			}
		}

		// Home position storage for reset functionality
		private Vector3 homePosition;
		private Quaternion homeRotation;
		private bool hasHomePosition = false;

		/// <summary>
		/// Capture current camera pose as home position for reset
		/// </summary>
		public void CaptureCurrentAsHome()
		{
			homePosition = cachedTransform.position;
			homeRotation = cachedTransform.rotation;
			hasHomePosition = true;
		}

		/// <summary>
		/// Reset camera to home position
		/// </summary>
		public void ResetToHome()
		{
			if (hasHomePosition)
			{
				cachedTransform.position = homePosition;
				cachedTransform.rotation = homeRotation;
				velocityRef = Vector3.zero; // Reset damping
			}
		}
	}
}
