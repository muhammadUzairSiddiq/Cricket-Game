using System.Collections.Generic;
using UnityEngine;

namespace CricketGame.Core
{
	/// <summary>
	/// Flexible, scalable state machine for gameplay states.
	/// Handles state transitions with smooth loading panel integration.
	/// Frame-efficient: only active state updates.
	/// </summary>
	public class GameStateMachine : MonoBehaviour
	{
		[Header("State Management")]
		[SerializeField] private bool showDebugLogs = true;

		[Header("Transition Settings")]
		[SerializeField] private bool useLoadingPanelTransitions = true;
		[SerializeField] private float transitionDuration = 0.4f;

		private IGameState currentState;
		private Dictionary<string, IGameState> registeredStates = new Dictionary<string, IGameState>();
		private bool isTransitioning = false;

		/// <summary>
		/// Register a state with the machine. States must be registered before use.
		/// </summary>
		public void RegisterState(IGameState state)
		{
			if (state == null)
			{
				Debug.LogError("GameStateMachine: Cannot register null state!");
				return;
			}

			string key = state.StateName;
			if (registeredStates.ContainsKey(key))
			{
				Debug.LogWarning($"GameStateMachine: State '{key}' already registered. Overwriting.");
			}

			registeredStates[key] = state;
			if (showDebugLogs)
				Debug.Log($"✅ GameStateMachine: Registered state '{key}'");
		}

		/// <summary>
		/// Transition to a new state by name. Smooth transition with loading panel.
		/// </summary>
		public void TransitionToState(string stateName)
		{
			if (isTransitioning)
			{
				Debug.LogWarning($"GameStateMachine: Already transitioning. Ignoring transition to '{stateName}'");
				return;
			}

			if (!registeredStates.ContainsKey(stateName))
			{
				Debug.LogError($"GameStateMachine: State '{stateName}' not registered!");
				return;
			}

			StartCoroutine(TransitionCoroutine(stateName));
		}

		/// <summary>
		/// Transition to a new state directly (no loading panel).
		/// </summary>
		public void TransitionToStateImmediate(string stateName)
		{
			if (isTransitioning)
			{
				Debug.LogWarning($"GameStateMachine: Already transitioning. Ignoring transition to '{stateName}'");
				return;
			}

			if (!registeredStates.ContainsKey(stateName))
			{
				Debug.LogError($"GameStateMachine: State '{stateName}' not registered!");
				return;
			}

			// CRITICAL: Reset transition flag before changing state to prevent getting stuck
			isTransitioning = false;
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
				Debug.LogWarning("GameStateMachine: Force resetting stuck transition flag");
				isTransitioning = false;
			}
		}

		private System.Collections.IEnumerator TransitionCoroutine(string stateName)
		{
			isTransitioning = true;

			try
			{
				// Show loading panel if enabled
				if (useLoadingPanelTransitions)
				{
					CricketGame.UI.LoadingPanelManager.StartPulse();
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
				// CRITICAL: Always reset transition flag, even if something goes wrong
				isTransitioning = false;
			}
		}

		private void ChangeStateImmediate(string stateName)
		{
			IGameState newState = registeredStates[stateName];

			if (showDebugLogs)
				Debug.Log($"🔄 GameStateMachine: Transitioning from '{currentState?.StateName ?? "None"}' to '{newState.StateName}'");

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
			// Only update if not transitioning and state exists
			if (!isTransitioning && currentState != null)
			{
				currentState.OnUpdate();
			}
		}
	}
}

