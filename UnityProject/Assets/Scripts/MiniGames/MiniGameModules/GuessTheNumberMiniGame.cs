using Mirror;
using UnityEngine;

namespace MiniGames.MiniGameModules
{
	public class GuessTheNumberMiniGame : MiniGameModule
	{
		private int randomNumber;

		public override void Setup(MiniGameResultTracker tracker)
		{
			base.Setup(tracker);
			var netID = tracker.GetComponent<NetworkIdentity>().netId.ToString();
			var firstThreeDigits = netID.Substring(0,3);
			randomNumber = int.Parse(firstThreeDigits);
		}

		public override void StartMiniGame()
		{
			base.StartMiniGame();
			//TODO : ADD MINIGAME UI AND HOOK UP STUFF 
		}

		public void CheckResult(int result)
		{
			var won = randomNumber == result;
			Tracker.OnGameDone?.Invoke(won);
		}
	}
}