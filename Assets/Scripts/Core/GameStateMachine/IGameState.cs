namespace CricketGame.Core
{
	/// <summary>
	/// Base interface for all game states.
	/// Provides clean entry/exit lifecycle and update loop.
	/// </summary>
	public interface IGameState
	{
		/// <summary>
		/// Called when entering this state. Setup and initialization happens here.
		/// </summary>
		void OnEnter();

		/// <summary>
		/// Called every frame while this state is active.
		/// </summary>
		void OnUpdate();

		/// <summary>
		/// Called when exiting this state. Cleanup happens here.
		/// </summary>
		void OnExit();

		/// <summary>
		/// State name for debugging/logging.
		/// </summary>
		string StateName { get; }
	}
}

