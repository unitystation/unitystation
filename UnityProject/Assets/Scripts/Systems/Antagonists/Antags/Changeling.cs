using Antagonists;
using Blob;
using Messages.Server;
using Player;
using System.Collections;
using System.Collections.Generic;
using Systems.Ai;
using UI.Systems.MainHUD.UI_Bottom;
using UnityEngine;

namespace Changeling
{
	[CreateAssetMenu(menuName = "ScriptableObjects/Antagonist/Changeling/Changeling")]
	public class Changeling : Antagonist
	{
		//[SerializeField] private Objective aiTraitorObjective;
		//[Header("Changeling attributes")]
		//[Tooltip("Abilites what is gona be in changeling store")]
		//[SerializeField] List<ChangelingActionData> abilitiesToBuy = new List<ChangelingActionData>();
		//[Tooltip("Abilites what is gona added on round start to changeling hands")]
		//[SerializeField] List<ChangelingActionData> abilitiesDefault = new List<ChangelingActionData>();

		public override Mind ServerSpawn(PlayerSpawnRequest spawnRequest)
		{
			// spawn them normally, with their preferred occupation
			return PlayerSpawn.NewSpawnPlayerV2(spawnRequest.Player, spawnRequest.RequestedOccupation, spawnRequest.CharacterSettings);
		}

		public override void AfterSpawn(Mind NewMind)
		{
			var ch = NewMind.Body.gameObject.AddComponent<ChangelingMain>();
			ch.Init(NewMind);
		}
	}
}