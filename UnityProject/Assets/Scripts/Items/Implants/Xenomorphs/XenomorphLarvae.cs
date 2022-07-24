using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using CameraEffects;
using UnityEngine;

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

		public override void ImplantPeriodicUpdate()
		{
			currentTime++;

			if (currentTime < incubationTime) return;

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

			if (RelatedPart.HealthMaster.TryGetComponent<PlayerScript>(out var playerScript) && playerScript.mind != null)
			{
				spawned.GameObject.GetComponent<PlayerScript>().mind = playerScript.mind;

				var connection = playerScript.connectionToClient;
				PlayerSpawn.ServerTransferPlayerToNewBody(connection, spawned.GameObject, playerScript.mind.GetCurrentMob(), Event.PlayerSpawned, playerScript.characterSettings);

				playerScript.mind = null;
			}

			RelatedPart.TryRemoveFromBody();

			Despawn.ServerSingle(gameObject);
		}
	}
}
