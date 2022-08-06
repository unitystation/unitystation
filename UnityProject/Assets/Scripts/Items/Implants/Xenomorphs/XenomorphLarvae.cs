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

		private static Dictionary<PlayerScript, short> infectedPlayers = new Dictionary<PlayerScript, short>();

		private short lastIndex;

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

				//Update the sprite based off of currentTime
				UpdateSprite();
				return;
			}

			//Can't hatch is player is dead, shouldn't be getting periodic updates if dead- but just as a double check.
			if (RelatedPart.HealthMaster.IsDead) return;

			RelatedPart.HealthMaster.ApplyDamageToBodyPart(
				gameObject,
				500,
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
				//6 bursted sprite
				SetSprite(6);
			}

			if (checkPlayerScript.HasComponent && checkPlayerScript.Component.mind != null)
			{
				spawned.GameObject.GetComponent<PlayerScript>().mind = checkPlayerScript.Component.mind;

				var connection = checkPlayerScript.Component.connectionToClient;
				PlayerSpawn.ServerTransferPlayerToNewBody(connection, checkPlayerScript.Component.mind,
					spawned.GameObject, Event.PlayerSpawned, null);
			}

			var alienPlayer = spawned.GameObject.GetComponent<AlienPlayer>();

			alienPlayer.SetNewPlayer();
			alienPlayer.DoConnectCheck();

			RelatedPart.TryRemoveFromBody();

			_ = Despawn.ServerSingle(gameObject);
		}

		private static void AddToInfected(PlayerScript newPlayer)
		{
			infectedPlayers.Add(newPlayer, 1);

			InfectedMessage.Send(newPlayer, 1);
		}

		private static void RemoveFromInfected(PlayerScript oldPlayer)
		{
			infectedPlayers.Remove(oldPlayer);

			InfectedMessage.Send(oldPlayer, 0);
		}

		private void UpdateSprite()
		{
			if(checkPlayerScript.HasComponent == false) return;

			//Sprites are between index 1-5 (0 is blank and 6 is fully bursted)
			var newIndex = Mathf.RoundToInt(Mathf.Clamp(((currentTime / (float)incubationTime * 100) / 20f), 1, 5));

			if(newIndex == lastIndex) return;
			SetSprite((short)newIndex);
		}

		private void SetSprite(short newIndex)
		{
			lastIndex = newIndex;
			infectedPlayers[checkPlayerScript.Component] = newIndex;
			InfectedMessage.Send(checkPlayerScript.Component, lastIndex);
		}

		public static void Rejoined(NetworkConnectionToClient conn)
		{
			foreach (var player in infectedPlayers)
			{
				InfectedMessage.SendTo(conn, player.Key, player.Value);
			}
		}

		public static void LeftBody(NetworkConnectionToClient conn)
		{
			foreach (var player in infectedPlayers)
			{
				InfectedMessage.SendTo(conn, player.Key, 0);
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ClearStatics()
		{
			infectedPlayers = new Dictionary<PlayerScript, short>();
		}

		private void OnDisable()
		{
			if(checkPlayerScript.HasComponent == false) return;

			RemoveFromInfected(checkPlayerScript.Component);
		}
	}
}
