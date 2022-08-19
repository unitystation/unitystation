using System.Collections;
using UnityEngine.Events;

namespace MiniGames
{
	public interface IMiniGame
	{
		public void StartGame();
		public void GameEnd(bool hasWon);
	}
}