using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using CameraEffects;
using Messages.Server;
using Mirror;
using Systems.Antagonists;
using UnityEngine;
using Util;

namespace HealthV2
{
	public class XenomorphLarvae : BodyPartFunctionality
	{
		[SerializeField]
		[Tooltip("This GameObject will be spawned from the Larvae when its time")]
		private GameObject SpawnedLarvae;

		/// <summary>
		/// Time in seconds
		/// </summary>
		[SerializeField]
		private int incubationTime = 10;

		private int currentTime = 0;

		private CheckedComponent<PlayerScript> checkPlayerScript = new CheckedComponent<PlayerScript>();

		private static List<PlayerScript> infectedPlayers = new List<PlayerScript>();

		public override void AddedToBody(LivingHealthMasterBase livingHealth)
		{
			if(livingHealth.TryGetComponent<PlayerScript>(out var playerScript) == false) return;

			checkPlayerScript.DirectSetComponent(playerScript);

			AddToInfected(playerScript);
		}

		public override void RemovedFromBody(LivingHealthMasterBase livingHealth)
		{
			if(checkPlayerScript.HasComponent == false) return;

			RemoveFromInfected(checkPlayerScript.Component);

			checkPlayerScript.SetToNull();
		}

		public override void ImplantPeriodicUpdate()
		{
			currentTime++;

			if (currentTime < incubationTime)
			{
				if(checkPlayerScript.HasComponent == false) return;

				checkPlayerScript.Component.playerSprites.InfectedSpriteHandler
					.ChangeSprite(
						Mathf.RoundToInt(Mathf.Clamp(((currentTime / (float) incubationTime * 100) / 20f) - 1, 0, 4)));

				return;
			}

			//Can't hatch is player is dead, shouldn't be getting periodic updates if dead- but just as a double check.
			if (RelatedPart.HealthMaster.IsDead) return;

			RelatedPart.HealthMaster.ApplyDamageToBodyPart(
				gameObject,
				200,
				AttackType.Internal,
				DamageType.Brute,
				BodyPartType.Chest);

			var spawned = Spawn.ServerPrefab(SpawnedLarvae, RelatedPart.HealthMaster.gameObject.AssumedWorldPosServer());

			if (spawned.Successful == false)
			{
				return;
			}

			if (checkPlayerScript.HasComponent)
			{
				checkPlayerScript.Component.playerSprites.InfectedSpriteHandler.ChangeSprite(5);
			}

			if (checkPlayerScript.HasComponent && checkPlayerScript.Component.mind != null)
			{
				spawned.GameObject.GetComponent<PlayerScript>().mind = checkPlayerScript.Component.mind;

				var connection = checkPlayerScript.Component.connectionToClient;
				PlayerSpawn.ServerTransferPlayerToNewBody(connection, checkPlayerScript.Component.mind,
					spawned.GameObject, Event.PlayerSpawned, checkPlayerScript.Component.characterSettings);
			}

			var alienPlayer = spawned.GameObject.GetComponent<AlienPlayer>();

			alienPlayer.DoConnectCheck();

			RelatedPart.TryRemoveFromBody();

			_ = Despawn.ServerSingle(gameObject);
		}

		private static void AddToInfected(PlayerScript newPlayer)
		{
			infectedPlayers.Add(newPlayer);

			InfectedMessage.Send(newPlayer, true);

			if (CustomNetworkManager.IsHeadless == false) return;

			newPlayer.playerSprites.InfectedSpriteHandler.SpriteRenderer.enabled = true;
		}

		private static void RemoveFromInfected(PlayerScript oldPlayer)
		{
			infectedPlayers.Remove(oldPlayer);

			InfectedMessage.Send(oldPlayer, false);

			if(CustomNetworkManager.IsHeadless == false) return;

			oldPlayer.playerSprites.InfectedSpriteHandler.SpriteRenderer.enabled = false;
		}

		public static void Rejoined(NetworkConnectionToClient conn)
		{
			foreach (var player in infectedPlayers)
			{
				InfectedMessage.SendTo(conn, player, true);
			}
		}

		public static void LeftBody(NetworkConnectionToClient conn)
		{
			foreach (var player in infectedPlayers)
			{
				InfectedMessage.SendTo(conn, player, false);
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ClearStatics()
		{
			infectedPlayers = new List<PlayerScript>();
		}
	}

	public class InfectedMessage : ServerMessage<InfectedMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint netId;
			public bool IsInfected;
		}

		public override void Process(NetMessage msg)
		{
			LoadNetworkObject(msg.netId);
			if(NetworkObject == null) return;
			if(NetworkObject.TryGetComponent<PlayerScript>(out var playerScript) == false) return;

			playerScript.playerSprites.InfectedSpriteHandler.SpriteRenderer.enabled = msg.IsInfected;
		}

		public static void Send(PlayerScript infectedPlayer, bool isInfected)
		{
			//Send to aliens and ghosts
			var players = PlayerList.Instance.InGamePlayers.Where(x =>
				x.Script.PlayerState is PlayerStates.Alien or PlayerStates.Ghost);

			var msg = new NetMessage()
			{
				netId = infectedPlayer.netId,
				IsInfected = isInfected
			};

			foreach (var player in players)
			{
				SendTo(player, msg);
			}
		}

		public static void SendTo(NetworkConnectionToClient conn, PlayerScript infectedPlayer, bool isInfected)
		{
			var msg = new NetMessage()
			{
				netId = infectedPlayer.netId,
				IsInfected = isInfected
			};

			SendTo(conn, msg);
		}
	}
}
