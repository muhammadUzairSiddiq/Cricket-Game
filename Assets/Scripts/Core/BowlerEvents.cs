using System;
using UnityEngine;

namespace CricketGame
{
	/// <summary>
	/// Global broadcaster for notifying when a bowler (player) is instantiated or switched at runtime.
	/// Camera and other systems can subscribe to react without tight coupling.
	/// </summary>
	public static class BowlerEvents
	{
		/// <summary>
		/// Fired when a bowler has been instantiated and is ready. Payload is the bowler root Transform to follow.
		/// </summary>
		public static event Action<Transform> OnBowlerReady;

		/// <summary>
		/// Fired when bowler signals camera should stop following (e.g., enters trigger box).
		/// Camera will freeze until next ball (P key pressed).
		/// </summary>
		public static event Action OnBowlerStopFollow;

		/// <summary>
		/// Fired when next ball is ready (P key pressed).
		/// Camera resumes following and target shows again.
		/// </summary>
		public static event Action OnNextBallReady;

		/// <summary>
		/// Notify listeners that a bowler is ready to be followed.
		/// Safe to call from any script that spawns/switches the bowler.
		/// </summary>
		public static void NotifyBowlerReady(Transform bowlerRoot)
		{
			if (bowlerRoot == null)
			{
				return;
			}
			OnBowlerReady?.Invoke(bowlerRoot);
		}

		/// <summary>
		/// Notify camera to stop following (freeze movement).
		/// Call this from bowler scripts when they enter trigger zone or want to stop camera.
		/// </summary>
		public static void NotifyStopFollowing()
		{
			OnBowlerStopFollow?.Invoke();
		}

		/// <summary>
		/// Notify that next ball is ready (camera resumes, target shows again).
		/// Called when P key is pressed or next ball starts.
		/// </summary>
		public static void NotifyNextBallReady()
		{
			OnNextBallReady?.Invoke();
		}
	}
}
