using System.Collections.Generic;
using UnityEngine;
using CricketGame.UI;

namespace CricketGame.Core
{
	/// <summary>
	/// Flexible, scalable state machine for gameplay states.
	/// Handles state transitions with smooth loading panel integration.
	/// Frame-efficient: only active state updates.
	/// </summary>
	public class GameStateMachine : MonoBehaviour
	{
		[Header("Transition Settings")]
		[SerializeField] private bool useLoadingPanelTransitions = true;
		[SerializeField] private float transitionDuration = 0.4f;
		[SerializeField] private LoadingPanelManager.LoadingAnimationMode loadingAnimationMode = LoadingPanelManager.LoadingAnimationMode.Pulse;

		private IGameState currentState;
		private Dictionary<string, IGameState> registeredStates = new Dictionary<string, IGameState>();
		private bool isTransitioning = false;
		private float transitionStartTime = 0f;
		private const float MAX_TRANSITION_TIME = 5f; // Maximum time a transition should take

		/// <summary>
		/// Register a state with the machine. States must be registered before use.
		/// </summary>
		public void RegisterState(IGameState state)
		{
			if (state == null)
			{
				return;
			}

			string key = state.StateName;
			registeredStates[key] = state;
		}

		/// <summary>
		/// Transition to a new state by name. Smooth transition with loading panel.
		/// </summary>
		public void TransitionToState(string stateName)
		{
			// CRITICAL: Force reset if stuck for too long
			if (isTransitioning && Time.time - transitionStartTime > MAX_TRANSITION_TIME)
			{
				isTransitioning = false;
			}
			
			if (isTransitioning)
			{
				return;
			}

			if (!registeredStates.ContainsKey(stateName))
			{
				return;
			}

			StartCoroutine(TransitionCoroutine(stateName));
		}

		/// <summary>
		/// Transition to a new state directly (no loading panel).
		/// </summary>
		public void TransitionToStateImmediate(string stateName)
		{
			// CRITICAL: Force reset if stuck for too long
			if (isTransitioning && Time.time - transitionStartTime > MAX_TRANSITION_TIME)
			{
				isTransitioning = false;
			}
			
			if (isTransitioning)
			{
				return;
			}

			if (!registeredStates.ContainsKey(stateName))
			{
				return;
			}

			// CRITICAL: Reset transition flag before changing state to prevent getting stuck
			isTransitioning = false;
			transitionStartTime = 0f;
			ChangeStateImmediate(stateName);
		}

		/// <summary>
		/// Get current state name.
		/// </summary>
		public string GetCurrentStateName()
		{
			return currentState?.StateName ?? "None";
		}

		/// <summary>
		/// Check if currently transitioning.
		/// </summary>
		public bool IsTransitioning()
		{
			return isTransitioning;
		}

		/// <summary>
		/// Force reset transition flag if stuck (emergency recovery)
		/// </summary>
		public void ForceResetTransition()
		{
			if (isTransitioning)
			{
				isTransitioning = false;
				transitionStartTime = 0f;
				
				// CRITICAL: Stop all coroutines that might be stuck
				StopAllCoroutines();
				
				if (useLoadingPanelTransitions)
				{
					LoadingPanelManager.StopAnimation();
				}
			}
		}

		private System.Collections.IEnumerator TransitionCoroutine(string stateName)
		{
			isTransitioning = true;
			transitionStartTime = Time.time;

			try
			{
				// Show loading panel if enabled
				if (useLoadingPanelTransitions)
				{
					LoadingPanelManager.PlayAnimation(loadingAnimationMode);
					yield return new WaitForSeconds(transitionDuration);
				}

				// Change state (ChangeStateImmediate handles enabling new camera before disabling old one)
				ChangeStateImmediate(stateName);

				// Hide loading panel
				if (useLoadingPanelTransitions)
				{
					// Wait for pulse to complete (it auto-stops)
					yield return new WaitForSeconds(transitionDuration);
				}
			}
			finally
			{
				if (useLoadingPanelTransitions)
				{
					LoadingPanelManager.StopAnimation();
				}
				// CRITICAL: Always reset transition flag, even if something goes wrong
				isTransitioning = false;
				transitionStartTime = 0f;
			}
		}

		private void ChangeStateImmediate(string stateName)
		{
			IGameState newState = registeredStates[stateName];

			// CRITICAL: Enable new camera BEFORE disabling old one to prevent "No camera to render" error
			// Enter new state first (enables new camera)
			newState.OnEnter();
			
			// Then exit old state (disables old camera)
			// This ensures there's always at least one camera active during transition
			if (currentState != null)
			{
				currentState.OnExit();
			}

			currentState = newState;
		}

		private void Update()
		{
			// CRITICAL: Watchdog timer - auto-reset stuck transitions
			if (isTransitioning && transitionStartTime > 0f && Time.time - transitionStartTime > MAX_TRANSITION_TIME)
			{
				isTransitioning = false;
				transitionStartTime = 0f;
			}
			
			// Only update if not transitioning and state exists
			if (!isTransitioning && currentState != null)
			{
				currentState.OnUpdate();
			}
		}
	}
}

