using UnityEngine;
using CricketGame.Core;

namespace CricketGame.GameplayStates
{
	/// <summary>
	/// Bowling state - bowler bowls. Same as current P key functionality.
	/// Handles bowling animation and ball release.
	/// </summary>
	public class BowlingState : MonoBehaviour, IGameState
	{
		[Header("Bowling References")]
		[SerializeField] private CricketGame.BowlingController bowlingController;
		[SerializeField] private CricketGame.BowlerFollowCamera bowlerFollowCamera; // ensure active during bowling
		
		[Header("Post-Bowl Flow")]
		[Tooltip("Seconds to wait AFTER bowl before returning bowler and switching to PitchCam")] 
		[SerializeField] private float postBowlReturnDelay = 3f;
		[Tooltip("Use loading panel pulse while returning bowler to spawn")] 
		[SerializeField] private bool useLoadingPulseOnReturn = true;

		private GameStateMachine stateMachine;

		public string StateName => "Bowling";

		public void OnEnter()
		{
			stateMachine = GetComponent<GameStateMachine>();

			// CRITICAL: Clean up any existing coroutines first
			if (_postStopCoroutine != null)
			{
				StopCoroutine(_postStopCoroutine);
				_postStopCoroutine = null;
			}

			// Camera already active from CameraFollow state; no toggling needed
			
			// Pick new random Y rotation for bowling state
			if (bowlingController != null)
			{
				GameObject bowlerInstance = bowlingController.GetCurrentBowlerInstance();
				if (bowlerInstance != null)
				{
					PlayerAnimationController playerController = bowlerInstance.GetComponent<PlayerAnimationController>();
					if (playerController != null)
					{
						playerController.PickRandomYRotationForBowling();
					}
				}
			}
			
			// CRITICAL: Unsubscribe first to prevent duplicate subscriptions
			CricketGame.BowlerEvents.OnBowlerStopFollow -= HandleBowlerStopFollow;
			// Then subscribe (ensures only one subscription)
			CricketGame.BowlerEvents.OnBowlerStopFollow += HandleBowlerStopFollow;
		}

		public void OnUpdate()
		{
			// Bowling happens via animation events
			// State can transition to next state when ball is complete
			// For now, stay in this state until manual transition
		}

		public void OnExit()
		{
			// CRITICAL: Unsubscribe from events
			CricketGame.BowlerEvents.OnBowlerStopFollow -= HandleBowlerStopFollow;
			
			// CRITICAL: Stop all coroutines to prevent stuck state
			if (_postStopCoroutine != null)
			{
				StopCoroutine(_postStopCoroutine);
				_postStopCoroutine = null;
			}
			StopAllCoroutines();
			
			// CRITICAL: Force reset state machine if stuck
			if (stateMachine != null && stateMachine.IsTransitioning())
			{
				stateMachine.ForceResetTransition();
			}
			
			// Keep camera state unchanged; next state will manage cameras
		}

		private Coroutine _postStopCoroutine;

		private void HandleBowlerStopFollow()
		{
			// CRITICAL: Prevent multiple coroutines from starting
			if (_postStopCoroutine == null && stateMachine != null && !stateMachine.IsTransitioning())
			{
				_postStopCoroutine = StartCoroutine(PostStopTimer());
			}
		}

		private System.Collections.IEnumerator PostStopTimer()
		{
			if (postBowlReturnDelay > 0f)
			{
				yield return new WaitForSeconds(postBowlReturnDelay);
			}
			
			// CRITICAL: Check if we're still in this state before transitioning
			if (stateMachine == null || stateMachine.GetCurrentStateName() != "Bowling")
			{
				_postStopCoroutine = null;
				yield break;
			}
			
			// Fade and reset bowler, then go to PitchCam
			if (useLoadingPulseOnReturn)
			{
				CricketGame.UI.LoadingPanelManager.StartPulse();
			}
			if (bowlingController != null)
			{
				// Reset existing bowler to spawn (NOT destroy/recreate)
				bowlingController.ResetBowlerToSpawn();
			}
			// Reset follow camera back to its home pose so next follow starts from center
			if (bowlerFollowCamera != null)
			{
				bowlerFollowCamera.ResetToHome();
			}
			if (stateMachine != null && !stateMachine.IsTransitioning())
			{
				stateMachine.TransitionToStateImmediate("PitchCam");
			}
			_postStopCoroutine = null;
		}

		private System.Collections.IEnumerator PostBowlSequence()
		{
			if (postBowlReturnDelay > 0f)
			{
				yield return new WaitForSeconds(postBowlReturnDelay);
			}
			// Hide movement with loading pulse and reset bowler back to spawn
			if (useLoadingPulseOnReturn)
			{
				CricketGame.UI.LoadingPanelManager.StartPulse();
			}
			if (bowlingController != null)
			{
				bowlingController.InstantiateSelectedBowler();
			}
			// Switch directly to PitchCam (we already pulsed)
			if (stateMachine != null)
			{
				stateMachine.TransitionToStateImmediate("PitchCam");
			}
		}
	}
}

