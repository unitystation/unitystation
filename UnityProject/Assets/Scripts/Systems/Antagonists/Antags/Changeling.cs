using Antagonists;
using Blob;
using Messages.Server;
using Mirror;
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
		private PlayerInfo playerConn;
		public override Mind ServerSpawn(PlayerSpawnRequest spawnRequest)
		{
			// spawn them normally, with their preferred occupation
			playerConn = spawnRequest.Player;
			return PlayerSpawn.NewSpawnPlayerV2(spawnRequest.Player, spawnRequest.RequestedOccupation, spawnRequest.CharacterSettings);
		}

		public override void AfterSpawn(Mind NewMind)
		{
			var ch = Spawn.ServerPrefab(ChangelingAbilityList.Instance.ChangelingMainPrefab, parent: NewMind.Body.gameObject.transform).GameObject.GetComponent<ChangelingMain>();
			PlayerSpawn.TransferOwnershipFromToConnection(playerConn, null, ch.gameObject.GetComponent<NetworkIdentity>());

			ch.Init(NewMind);
		}
	}
}