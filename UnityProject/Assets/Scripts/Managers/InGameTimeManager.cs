using System;
using AdminCommands;
using Shared.Managers;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Managers
{
	public class InGameTimeManager : SingletonManager<InGameTimeManager>
	{
		public DateTime UniversalSpaceTime { get; set;}
		public DateTime UtcTime { get; private set; }
		private const int GAME_YEAR = 2562;


		public override void Awake()
		{
			base.Awake();
			EventManager.AddHandler(Event.RoundStarted, ServerSetupUniversalSpaceTime);
			EventManager.RemoveHandler(Event.PlayerRejoined, UpdateClients);
			EventManager.RemoveHandler(Event.PlayerSpawned, UpdateClients);
			UtcTime = DateTime.UtcNow.AddYears(GAME_YEAR);
		}

		private void OnDisable()
		{
			EventManager.RemoveHandler(Event.RoundStarted, ServerSetupUniversalSpaceTime);
			EventManager.RemoveHandler(Event.PlayerRejoined, UpdateClients);
			EventManager.RemoveHandler(Event.PlayerSpawned, UpdateClients);
		}

		private void ServerSetupUniversalSpaceTime()
		{
			if (CustomNetworkManager.Instance == null || CustomNetworkManager.IsServer == false) return;
			UniversalSpaceTime = DateTime.Now.AddYears(GAME_YEAR);
			UniversalSpaceTime = DateTime.Now.AddDays(Random.Range(1,5));
			UniversalSpaceTime = DateTime.Now.AddMonths(Random.Range(1,5));
		}

		private void UpdateClients()
		{
			AdminCommandsManager.Instance.UpdateInGameTime(UniversalSpaceTime);
		}
	}
}