using UnityEngine;

namespace CricketGame.Core
{
	/// <summary>
	/// Listens for the global next-ball event (triggered by P) and transitions to PitchCam.
	/// Attach this to the same GameObject as GameStateMachine.
	/// </summary>
    public class NextBallStateRouter : MonoBehaviour
	{
		[SerializeField] private GameStateMachine stateMachine;
		[SerializeField] private string pitchCamStateName = "PitchCam";
        [SerializeField] private bool showDebugLogs = false;
        [SerializeField] private bool enableTransitionOnNextBall = false; // default off; BowlingState handles timed transition

		private void OnEnable()
		{
			BowlerEvents.OnNextBallReady += HandleNextBall;
			if (stateMachine == null) stateMachine = GetComponent<GameStateMachine>();
		}

		private void OnDisable()
		{
			BowlerEvents.OnNextBallReady -= HandleNextBall;
		}

        private void HandleNextBall()
		{
            if (!enableTransitionOnNextBall) return;
            if (stateMachine == null) return;
            if (showDebugLogs) Debug.Log("🎯 NextBallStateRouter: Switching to PitchCam state (NextBall)");
            if (stateMachine.IsTransitioning())
            {
                stateMachine.ForceResetTransition();
            }
            stateMachine.TransitionToStateImmediate(pitchCamStateName);
		}
	}
}
