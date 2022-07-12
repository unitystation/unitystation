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

			if (currentTime >= incubationTime)
			{
				if (RelatedPart.HealthMaster.IsDead) //Can't hatch is player is dead, shouldn't be getting periodic updates if dead- but just as a double check.
					return;

				this.GetComponent<BodyPart>().HealthMaster.ApplyDamageToBodyPart(
					gameObject,
					200,
					AttackType.Internal,
					DamageType.Brute,
					BodyPartType.Chest);

				this.GetComponent<BodyPart>().TryRemoveFromBody();

				Spawn.ServerPrefab(SpawnedLarvae, SpawnDestination.At(gameObject), 1);

				Despawn.ServerSingle(gameObject);
			}		
		}
	}
}
