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
		public override Mind ServerSpawn(PlayerSpawnRequest spawnRequest)
		{
			// spawn them normally, with their preferred occupation
			return PlayerSpawn.NewSpawnPlayerV2(spawnRequest.Player, spawnRequest.RequestedOccupation, spawnRequest.CharacterSettings);
		}

		public override void AfterSpawn(Mind NewMind)
		{
			// = NewMind.Body.gameObject.AddComponent<ChangelingMain>();
			var ch = Spawn.ServerPrefab(ChangelingAbilityList.Instance.ChangelingMainPrefab).GameObject.GetComponent<ChangelingMain>();
			ch.Init(NewMind);
		}
	}
}