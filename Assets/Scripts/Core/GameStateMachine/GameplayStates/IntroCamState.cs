using UnityEngine;
using CricketGame.Core;

namespace CricketGame.GameplayStates
{
	/// <summary>
	/// Intro camera state - plays intro animation for a few seconds, then transitions to PitchCam.
	/// Bowler is instantiated in background during this state.
	/// </summary>
	public class IntroCamState : MonoBehaviour, IGameState
	{
	[Header("Intro Settings")]
	[SerializeField] private float introDuration = 3f;
	[SerializeField] private Camera introCam;
	[SerializeField] private Camera pitchCam; // optional: disabled on enter
	[SerializeField] private CricketGame.BowlerFollowCamera bowlerFollowCamera; // optional: disabled on enter
	[SerializeField] private CricketGame.BowlingController bowlingController;

	private GameStateMachine stateMachine;
	private float elapsedTime;

		public string StateName => "IntroCam";

		public void OnEnter()
		{
			stateMachine = GetComponent<GameStateMachine>();
			elapsedTime = 0f;
			
			if (stateMachine != null && stateMachine.IsTransitioning())
			{
				stateMachine.ForceResetTransition();
			}
			StopAllCoroutines();

			// Disable other cameras first to ensure only Intro cam is active
			if (pitchCam == null)
			{
				// Try to find any camera tagged as MainCamera except introCam
				Camera[] cams = Resources.FindObjectsOfTypeAll<Camera>();
				foreach (var c in cams)
				{
					if (c != null && c != introCam && c.gameObject.name.Contains("Pitch Cam"))
					{
						pitchCam = c;
						break;
					}
				}
			}
			if (bowlerFollowCamera == null)
			{
				bowlerFollowCamera = Resources.FindObjectsOfTypeAll<CricketGame.BowlerFollowCamera>().Length > 0 ? Resources.FindObjectsOfTypeAll<CricketGame.BowlerFollowCamera>()[0] : null;
			}
			if (pitchCam != null) pitchCam.gameObject.SetActive(false);
			if (bowlerFollowCamera != null && bowlerFollowCamera.gameObject != null) bowlerFollowCamera.gameObject.SetActive(false);

			// Activate intro camera
			if (introCam != null)
			{
				introCam.gameObject.SetActive(true);
			}

		// Instantiate bowler using BowlingController's selected prefab and spawn point
		if (bowlingController != null)
		{
			bowlingController.InstantiateSelectedBowler();
			// Wait a frame for instantiation, then notify camera system
			StartCoroutine(NotifyBowlerAfterInstantiation());
		}
		}

		public void OnUpdate()
		{
			elapsedTime += Time.deltaTime;

			// Transition to PitchCam after duration
			if (elapsedTime >= introDuration)
			{
				if (stateMachine != null)
				{
					stateMachine.TransitionToStateImmediate("PitchCam");
				}
			}
		}

	public void OnExit()
	{
		StopAllCoroutines();
		if (stateMachine != null && stateMachine.IsTransitioning())
		{
			stateMachine.ForceResetTransition();
		}

		// Deactivate intro camera
		if (introCam != null)
		{
			introCam.gameObject.SetActive(false);
		}
	}

	private System.Collections.IEnumerator NotifyBowlerAfterInstantiation()
	{
		// Wait a frame for instantiation to complete
		yield return null;

		// Get instantiated bowler and notify camera system
		if (bowlingController != null)
		{
			GameObject instantiatedBowler = bowlingController.GetCurrentBowlerInstance();
			if (instantiatedBowler != null)
			{
				CricketGame.BowlerEvents.NotifyBowlerReady(instantiatedBowler.transform);
			}
		}
	}
}
}
