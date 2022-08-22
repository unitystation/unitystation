using UnityEngine;

namespace MiniGames
{
	/// <summary>
	/// MiniGameModules are gameObjects that sit on the gameObject that has the IMiniGame Interface.
	/// They are meant to be used when an object can house more than one minigame or if the minigame's behavior is too complex
	/// to be stored in just the StartMiniGame() method.
	/// </summary>
	public abstract class MiniGameModule : MonoBehaviour
	{
		protected MiniGameResultTracker Tracker;
		protected GameObject MiniGameParent;

		/// <summary>
		/// Use the Setup() function only when you're assigned events programatically and not from the inspector.
		/// </summary>
		public virtual void Setup(MiniGameResultTracker tracker, GameObject parent)
		{
			Tracker = tracker;
			MiniGameParent = parent;
			Tracker.OnStartGame.AddListener(StartMiniGame);
		}

		public virtual void StartMiniGame()
		{
			//Add your game setup/reset/ui show/etc here
		}

		protected virtual void OnGameDone(bool t)
		{
			//Add your logic here when a player has won or lost a game or has left the game
		}
	}
}