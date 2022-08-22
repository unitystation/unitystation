using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

namespace MiniGames
{
	/// <summary>
	/// General component to tell the to call IMiniGame functions to call a function when a MiniGame has returned a won state.
	/// </summary>
	public class MiniGameResultTracker : NetworkBehaviour
	{
		// We use UnitEvents to assign these in the inspector //
		// This also lets us house more than one MGRT on one gameObject when there are multiple MiniGames //
		// And each MiniGame can has it's own callback to other functions on many different components and gameObjects. //


		public UnityEvent OnStartGame;
		public UnityEvent OnGameLoss;
		public UnityEvent OnGameWon;

		public void StartGame()
		{
			OnStartGame?.Invoke();
		}

		public void OnGameEnd(bool hasWon)
		{
			if (hasWon)
			{
				OnGameWon?.Invoke();
				return;
			}
			OnGameLoss?.Invoke();
		}
	}
}