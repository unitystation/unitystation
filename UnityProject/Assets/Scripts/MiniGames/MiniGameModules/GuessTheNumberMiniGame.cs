using Mirror;
using UnityEngine;

namespace MiniGames.MiniGameModules
{
	public class GuessTheNumberMiniGame : MiniGameModule
	{
		private int randomNumber;
		[SerializeField] private IMiniGame parent;

		public override void Setup(MiniGameResultTracker tracker)
		{
			base.Setup(tracker);
			parent = transform.parent.parent.GetComponent<IMiniGame>();
			randomNumber = Random.Range(100, 999); //Number will only exist on the server because only the server calls Setup()
		}

		public override void StartMiniGame()
		{
			base.StartMiniGame();
			//TODO : ADD MINIGAME UI AND HOOK UP STUFF
		}
	}
}