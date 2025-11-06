using UnityEngine;
using CricketGame.Core;
using CricketGame;

namespace CricketGame.GameplayStates
{
	/// <summary>
	/// Pitch camera state - user drags target and selects speed within time limit.
	/// 15 seconds timer. Success -> CameraFollow, Timeout -> Failed.
	/// </summary>
	public class PitchCamState : MonoBehaviour, IGameState
	{
		[Header("Pitch Settings")]
		[SerializeField] private float timeLimit = 15f;
		[SerializeField] private Camera pitchCam;
		[SerializeField] private SpeedController speedController;
		[SerializeField] private Camera introCam; // optional: disabled on enter
		[SerializeField] private CricketGame.BowlerFollowCamera bowlerFollowCamera; // optional: disabled on enter
		[Tooltip("Root GameObject that contains the Speed Controller UI (e.g. 'Control Panel')")]
		[SerializeField] private GameObject speedPanelRoot;
		[SerializeField] private TargetDragger targetDragger;

		private GameStateMachine stateMachine;
		private float elapsedTime;
		private bool isSpeedSelected = false;
		private bool isTargetDragged = false;
		
		// Store original scale of Slides for restoration
		private Vector3 originalSlidesScale = Vector3.one;
		private bool originalScaleStored = false;
		private bool _hasTransitionedFromSpeed = false; // Prevent multiple transitions

		public string StateName => "PitchCam";

		public void OnEnter()
		{
			stateMachine = GetComponent<GameStateMachine>();
			elapsedTime = 0f;
			isSpeedSelected = false;
			isTargetDragged = false;
			_hasTransitionedFromSpeed = false; // Reset transition flag
			
			// CRITICAL: Force reset transition flag if stuck (safety mechanism)
			if (stateMachine != null && stateMachine.IsTransitioning())
			{
				Debug.LogWarning("🎯 PitchCamState: Entering with stuck transition flag, forcing reset");
				stateMachine.ForceResetTransition();
			}
			
			// CRITICAL: Stop all coroutines to prevent stuck state
			StopAllCoroutines();
			
			// CRITICAL: Reset speed selection FIRST before activating UI to prevent immediate transition
			if (speedController != null)
			{
				speedController.ResetSpeedSelection(); // Reset BEFORE activating UI
			}

			// Activate pitch camera
			if (pitchCam != null)
			{
				pitchCam.gameObject.SetActive(true);
			}

			// Enable target dragging
			if (targetDragger != null)
			{
				targetDragger.enabled = true;
			}


			// Ensure other cameras are disabled; only Pitch cam should be active in this state
			if (introCam == null)
			{
				var allCams = Resources.FindObjectsOfTypeAll<Camera>();
				foreach (var c in allCams)
				{
					if (c != null && c != pitchCam && c.gameObject.name.Contains("Intro"))
					{
						introCam = c;
						break;
					}
				}
			}
			if (bowlerFollowCamera == null)
			{
				var allFollows = Resources.FindObjectsOfTypeAll<CricketGame.BowlerFollowCamera>();
				if (allFollows != null && allFollows.Length > 0) bowlerFollowCamera = allFollows[0];
			}
			if (introCam != null) introCam.gameObject.SetActive(false);
			if (bowlerFollowCamera != null && bowlerFollowCamera.gameObject != null) bowlerFollowCamera.gameObject.SetActive(false);

			// Ensure speed controller reference (find inactive too if not assigned)
			if (speedController == null)
			{
				var all = Resources.FindObjectsOfTypeAll<SpeedController>();
				if (all != null && all.Length > 0)
					speedController = all[0];
			}

			// Find and enable the panel root if not assigned
            if (speedPanelRoot == null && speedController != null)
            {
                Transform p = speedController.transform.parent;
                if (p != null) speedPanelRoot = p.gameObject; // assume parent is the Control Panel
            }
            if (speedPanelRoot == null)
            {
                // fallback by name (works even if inactive)
                var allTransforms = Resources.FindObjectsOfTypeAll<RectTransform>();
                foreach (var rt in allTransforms)
                {
                    if (rt != null && rt.gameObject.name == "Control Panel")
                    {
                        speedPanelRoot = rt.gameObject;
                        break;
                    }
                }
            }

			if (speedPanelRoot != null)
				speedPanelRoot.SetActive(true);


			// Enable the speed meter UI only while in PitchCam state
			// NOTE: ResetSpeedSelection() was already called earlier to prevent immediate transition
			if (speedController != null)
			{
				speedController.ActivateUI(true);
			}

			// Link TargetDragger to use Pitch Cam for input
			if (targetDragger != null && pitchCam != null)
			{
				targetDragger.SetInputCamera(pitchCam);
			}

			// Ensure target visuals are restored when entering PitchCam again
			ForceShowTarget();
		}

		public void OnUpdate()
		{
			elapsedTime += Time.deltaTime;

			// CRITICAL: Force reset transition flag if stuck (safety mechanism)
			if (stateMachine != null && stateMachine.IsTransitioning() && elapsedTime > timeLimit + 1f)
			{
				Debug.LogWarning("🎯 PitchCamState: Transition flag stuck, forcing reset");
				stateMachine.ForceResetTransition();
			}

			// CRITICAL: Only check speed selection after a small delay to prevent immediate transition
			// This gives ResetSpeedSelection() time to complete
			if (elapsedTime > 0.1f)
			{
				// Check if speed is selected
				if (speedController != null && speedController.IsSpeedSelected)
				{
					isSpeedSelected = true;
				}
			}

			// Check if target has been dragged (any movement counts - target dragger is always active if enabled)
			if (targetDragger != null)
			{
				isTargetDragged = true; // Target is draggable, consider it ready
			}

			// Check timeout
			if (elapsedTime >= timeLimit)
			{
				// Timeout - go to failed state
				if (stateMachine != null && !stateMachine.IsTransitioning())
				{
					stateMachine.TransitionToState("Failed");
				}
				return;
			}

			// Check success conditions
			if (isSpeedSelected)
			{
				// CRITICAL: Only transition once per speed selection
				if (!_hasTransitionedFromSpeed)
				{
					_hasTransitionedFromSpeed = true;
					
					// Hide speed UI immediately before leaving
					if (speedController != null)
					{
						speedController.ActivateUI(false);
					}
					// Speed selected - transition to camera follow
					if (stateMachine != null && !stateMachine.IsTransitioning())
					{
						stateMachine.TransitionToState("CameraFollow");
					}
					else if (stateMachine != null && stateMachine.IsTransitioning())
					{
						// Force reset if stuck
						stateMachine.ForceResetTransition();
						stateMachine.TransitionToState("CameraFollow");
					}
				}
			}
		}

		public void OnExit()
		{
			// CRITICAL: Stop all coroutines to prevent stuck state
			StopAllCoroutines();
			
			// Reset transition flag
			_hasTransitionedFromSpeed = false;
			
			// Deactivate pitch camera
			if (pitchCam != null)
			{
				pitchCam.gameObject.SetActive(false);
			}

			// Disable target dragging
			if (targetDragger != null)
			{
				targetDragger.enabled = false;
			}

			// Deactivate speed meter UI when leaving PitchCam
			if (speedController != null)
			{
				speedController.ActivateUI(false);
			}
			if (speedPanelRoot != null)
			{
				speedPanelRoot.SetActive(false);
			}

			// Do not force-hide target here; hiding is controlled by bowler trigger logic
		}

		// --- Target helpers ---
		private void ForceShowTarget(bool hide = false)
		{
			// Find Target (even if inactive) and its child "Sides/Slides"
			GameObject targetGO = null;
			try { targetGO = GameObject.FindWithTag("Target"); } catch { targetGO = null; }
			if (targetGO == null)
			{
				// Fallback search through inactive objects
				var all = Resources.FindObjectsOfTypeAll<Transform>();
				for (int i = 0; i < all.Length; i++)
				{
					if (all[i] != null && all[i].CompareTag("Target"))
					{
						targetGO = all[i].gameObject;
						break;
					}
				}
			}
			if (targetGO == null)
			{
				Debug.LogWarning("🎯 PitchCamState: Target GameObject with 'Target' tag not found!");
				return;
			}

			// Ensure Target GameObject itself is active
			if (!hide)
			{
				targetGO.SetActive(true);
			}

			// Find Slides/Sides child (try both names)
			Transform sides = null;
			string[] possibleNames = new string[] { "Slides", "Sides" };
			foreach (string name in possibleNames)
			{
				sides = targetGO.transform.Find(name);
				if (sides != null) break;
			}

			// If direct child not found, search recursively
			if (sides == null)
			{
				foreach (string name in possibleNames)
				{
					sides = FindChildRecursive(targetGO.transform, name);
					if (sides != null) break;
				}
			}

			if (sides != null)
			{
				// Store original scale if not already stored (capture on first good encounter)
				if (!originalScaleStored)
				{
					Vector3 currentScale = sides.localScale;
					// If scale is reasonable (not hidden), store it as original
					if (currentScale.magnitude > 0.01f)
					{
						originalSlidesScale = currentScale;
						originalScaleStored = true;
					}
					else
					{
						// Scale is already hidden - try to get from PlayerAnimationController
						PlayerAnimationController animController = FindObjectOfType<PlayerAnimationController>();
						if (animController != null)
						{
							// Get original scale from PlayerAnimationController
							originalSlidesScale = animController.GetOriginalSidesScale();
							originalScaleStored = true;
						}
						else
						{
							// Last resort: use Vector3.one
							originalSlidesScale = Vector3.one;
							originalScaleStored = true;
							Debug.LogWarning("🎯 PitchCamState: Could not determine original Slides scale. Using Vector3.one as fallback.");
						}
					}
				}

				if (hide)
				{
					sides.localScale = Vector3.zero;
					sides.gameObject.SetActive(false);
					// Stop all particle systems
					StopAllParticleSystems(sides.gameObject);
				}
				else
				{
					// Ensure GameObject is active FIRST
					sides.gameObject.SetActive(true);
					// Restore to ORIGINAL scale (not Vector3.one)
					sides.localScale = originalSlidesScale;
					
					// Enable renderer if it exists
					Renderer renderer = sides.GetComponent<Renderer>();
					if (renderer != null)
					{
						renderer.enabled = true;
					}
					
					// Enable and play ALL particle systems (on this GameObject and all children)
					PlayAllParticleSystems(sides.gameObject);
				}
			}
			else
			{
				Debug.LogWarning($"🎯 PitchCamState: Could not find 'Slides' or 'Sides' child in Target GameObject!");
			}
		}

		private void PlayAllParticleSystems(GameObject obj)
		{
			if (obj == null) return;

			// Get all particle systems (including inactive ones)
			ParticleSystem[] allParticles = obj.GetComponentsInChildren<ParticleSystem>(true);
			foreach (ParticleSystem ps in allParticles)
			{
				if (ps != null)
				{
					ps.gameObject.SetActive(true);
					ps.Clear();
					ps.Play();
				}
			}

			// Also check on the GameObject itself
			ParticleSystem selfPs = obj.GetComponent<ParticleSystem>();
			if (selfPs != null)
			{
				selfPs.gameObject.SetActive(true);
				selfPs.Clear();
				selfPs.Play();
			}
		}

		private void StopAllParticleSystems(GameObject obj)
		{
			if (obj == null) return;

			ParticleSystem[] allParticles = obj.GetComponentsInChildren<ParticleSystem>(true);
			foreach (ParticleSystem ps in allParticles)
			{
				if (ps != null)
				{
					ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
				}
			}
		}

		private Transform FindChildRecursive(Transform parent, string name)
		{
			foreach (Transform child in parent)
			{
				if (child.name == name)
					return child;
				Transform found = FindChildRecursive(child, name);
				if (found != null)
					return found;
			}
			return null;
		}
	}
}

