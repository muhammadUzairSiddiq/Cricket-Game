using UnityEngine;
using CricketGame.Core;
using CricketGame.GameplayStates;

namespace CricketGame.Core
{
	/// <summary>
	/// Manager that sets up and initializes the gameplay state machine.
	/// Attach to a GameObject in your gameplay scene to auto-register all states.
	/// </summary>
	public class GameplayStateManager : MonoBehaviour
	{
		[Header("State Machine")]
		[SerializeField] private GameStateMachine stateMachine;

		[Header("State Components")]
		[SerializeField] private IntroCamState introCamState;
		[SerializeField] private PitchCamState pitchCamState;
		[SerializeField] private FailedState failedState;
		[SerializeField] private CameraFollowState cameraFollowState;
		[SerializeField] private BowlingState bowlingState;

		[Header("Initial State")]
		[SerializeField] private string initialStateName = "IntroCam";

		private void Start()
		{
			// Auto-find state machine if not assigned
			if (stateMachine == null)
			{
				stateMachine = GetComponent<GameStateMachine>();
				if (stateMachine == null)
				{
					stateMachine = FindObjectOfType<GameStateMachine>();
				}
			}

			if (stateMachine == null)
			{
				Debug.LogError("GameplayStateManager: GameStateMachine not found! Please assign it in Inspector.");
				return;
			}

			// Auto-find states if not assigned
			AutoFindStates();

			// Register all states
			RegisterStates();

			// Start with initial state
			stateMachine.TransitionToStateImmediate(initialStateName);
		}

		/// <summary>
		/// Auto-find state components if not assigned
		/// </summary>
		private void AutoFindStates()
		{
			if (introCamState == null)
				introCamState = GetComponent<IntroCamState>();
			if (introCamState == null)
				introCamState = FindObjectOfType<IntroCamState>();

			if (pitchCamState == null)
				pitchCamState = GetComponent<PitchCamState>();
			if (pitchCamState == null)
				pitchCamState = FindObjectOfType<PitchCamState>();

			if (failedState == null)
				failedState = GetComponent<FailedState>();
			if (failedState == null)
				failedState = FindObjectOfType<FailedState>();

			if (cameraFollowState == null)
				cameraFollowState = GetComponent<CameraFollowState>();
			if (cameraFollowState == null)
				cameraFollowState = FindObjectOfType<CameraFollowState>();

			if (bowlingState == null)
				bowlingState = GetComponent<BowlingState>();
			if (bowlingState == null)
				bowlingState = FindObjectOfType<BowlingState>();
		}

		/// <summary>
		/// Register all states with the state machine
		/// </summary>
		private void RegisterStates()
		{
			if (introCamState != null)
				stateMachine.RegisterState(introCamState);
			else
				Debug.LogWarning("GameplayStateManager: IntroCamState not found!");

			if (pitchCamState != null)
				stateMachine.RegisterState(pitchCamState);
			else
				Debug.LogWarning("GameplayStateManager: PitchCamState not found!");

			if (failedState != null)
				stateMachine.RegisterState(failedState);
			else
				Debug.LogWarning("GameplayStateManager: FailedState not found!");

			if (cameraFollowState != null)
				stateMachine.RegisterState(cameraFollowState);
			else
				Debug.LogWarning("GameplayStateManager: CameraFollowState not found!");

			if (bowlingState != null)
				stateMachine.RegisterState(bowlingState);
			else
				Debug.LogWarning("GameplayStateManager: BowlingState not found!");
		}
	}
}

