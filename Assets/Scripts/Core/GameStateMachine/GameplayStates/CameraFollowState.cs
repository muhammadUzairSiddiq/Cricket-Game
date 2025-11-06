using UnityEngine;
using CricketGame.Core;

namespace CricketGame.GameplayStates
{
	/// <summary>
	/// Camera follow state - camera follows bowler. User presses Space to start bowling.
	/// </summary>
	public class CameraFollowState : MonoBehaviour, IGameState
	{
		[Header("Camera Settings")]
		[SerializeField] private CricketGame.BowlerFollowCamera bowlerFollowCamera;
		[SerializeField] private Camera introCam; // optional: disabled on enter
		[SerializeField] private Camera pitchCam; // optional: disabled on enter

	private GameStateMachine stateMachine;

		public string StateName => "CameraFollow";

		public void OnEnter()
		{
		stateMachine = GetComponent<GameStateMachine>();

		// Ensure only follow camera is active; disable others
		if (introCam != null) introCam.gameObject.SetActive(false);
		if (pitchCam != null) pitchCam.gameObject.SetActive(false);

		// Ensure camera is following (BowlerFollowCamera handles its own camera activation)
		if (bowlerFollowCamera != null)
		{
			bowlerFollowCamera.enabled = true;
			// Activate camera GameObject if it's disabled
			if (bowlerFollowCamera.gameObject != null)
			{
				bowlerFollowCamera.gameObject.SetActive(true);
			}

			// Ensure camera resumes following (in case it was paused)
			bowlerFollowCamera.ResumeFollowing();

			// Capture current pose as home for later reset
			bowlerFollowCamera.CaptureCurrentAsHome();
		}

		}



		public void OnUpdate()
		{
			// Wait for P key to start bowling
			if (Input.GetKeyDown(KeyCode.P))
			{
				stateMachine.TransitionToState("Bowling");
			}
		}

		public void OnExit()
		{
			// Keep follow camera active when leaving for Bowling state
			// No camera toggling here to avoid gaps in rendering during the P transition
		}
	}
}

