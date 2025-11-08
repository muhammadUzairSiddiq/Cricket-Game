using UnityEngine;
using TMPro;
using CricketGame.Core;

namespace CricketGame.GameplayStates
{
	/// <summary>
	/// Failed state - shows "TIMEOUT" message.
	/// Can transition to menu or restart gameplay.
	/// </summary>
	public class FailedState : MonoBehaviour, IGameState
	{
		[Header("UI Settings")]
		[SerializeField] private TextMeshProUGUI timeoutText;
		[SerializeField] private float displayDuration = 3f;
		[SerializeField] private bool autoReturnToMenu = false;

		private GameStateMachine stateMachine;
		private float elapsedTime;

		public string StateName => "Failed";

		public void OnEnter()
		{
			stateMachine = GetComponent<GameStateMachine>();
			elapsedTime = 0f;
			
			if (stateMachine != null && stateMachine.IsTransitioning())
			{
				stateMachine.ForceResetTransition();
			}
			StopAllCoroutines();

			// Show timeout message
			if (timeoutText != null)
			{
				timeoutText.text = "TIMEOUT";
				timeoutText.gameObject.SetActive(true);
			}
		}

		public void OnUpdate()
		{
			elapsedTime += Time.deltaTime;

			// Return to PitchCam after displayDuration or when user presses P
			if (elapsedTime >= displayDuration || Input.GetKeyDown(KeyCode.P))
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

			// Hide timeout message
			if (timeoutText != null)
			{
				timeoutText.gameObject.SetActive(false);
			}
		}
	}
}

