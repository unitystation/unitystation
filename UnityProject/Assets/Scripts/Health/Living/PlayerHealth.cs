using System.Collections;
using System.Collections.Generic;
using Sprites;
using UnityEngine;
using UnityEngine.Networking;

namespace PlayGroup
{
	public class PlayerHealth : HealthBehaviour
	{
		private readonly float bleedRate = 2f;

		private int bleedVolume;

		//For now a simplified blood system will be here. To be refactored into a separate thing in the future.
		public int BloodLevel = (int) BloodVolume.NORMAL;
		//Health reporting is being performed on PlayerHealthReporting component. You should use the reporting component
		//to request health data of a particular player from the server. The reporting component also performs UI updates
		//for local players

		//Fill this in editor:
		//1 HumanHead, 1 HumanTorso & 4 HumanLimbs for a standard human
		//public Dictionary<BodyPartType, BodyPartBehaviour> BodyParts = new Dictionary<BodyPartType, BodyPartBehaviour>();
		public List<BodyPartBehaviour> BodyParts = new List<BodyPartBehaviour>();

		[Header("For harvestable animals")] public GameObject[] butcherResults;

		private PlayerMove playerMove;

		private PlayerNetworkActions playerNetworkActions;

		public bool IsBleeding { get; private set; }

		public override void OnStartClient()
		{
			playerNetworkActions = GetComponent<PlayerNetworkActions>();
			playerMove = GetComponent<PlayerMove>();

			PlayerScript playerScript = GetComponent<PlayerScript>();

			if (playerScript.JobType == JobType.NULL)
			{
				foreach (Transform t in transform)
				{
					t.gameObject.SetActive(false);
				}

				ConsciousState = ConsciousState.DEAD;
				playerMove.allowInput = false;
			}

			base.OnStartClient();
		}

		public override int ReceiveAndCalculateDamage(GameObject damagedBy, int damage, DamageType damageType,
			BodyPartType bodyPartAim)
		{
			base.ReceiveAndCalculateDamage(damagedBy, damage, damageType, bodyPartAim);

			BodyPartBehaviour bodyPart = findBodyPart(bodyPartAim); //randomise a bit here?
			bodyPart.ReceiveDamage(damageType, damage);

			if (isServer)
			{
				int bloodLoss = (int) (damage * BleedFactor(damageType));
				LoseBlood(bloodLoss);

				// don't start bleeding if limb is in ok condition after it received damage
				switch (bodyPart.Severity)
				{
					case DamageSeverity.Moderate:
					case DamageSeverity.Bad:
					case DamageSeverity.Critical:
						AddBloodLoss(bloodLoss);
						break;
				}
				if (headCritical(bodyPart))
				{
					Crit();
				}
			}
			return damage;
		}

		private static bool headCritical(BodyPartBehaviour bodyPart)
		{
			return bodyPart.Type.Equals(BodyPartType.HEAD) && bodyPart.Severity == DamageSeverity.Critical;
		}

		private BodyPartBehaviour findBodyPart(BodyPartType bodyPartAim)
		{
			//Don't like how you should iterate through bodyparts each time, but inspector doesn't seem to like dicts
			for (int i = 0; i < BodyParts.Count; i++)
			{
				if (BodyParts[i].Type == bodyPartAim)
				{
					return BodyParts[i];
				}
			}
			//dm code quotes:
			//"no bodypart, we deal damage with a more general method."
			//"missing limb? we select the first bodypart (you can never have zero, because of chest)"
			return BodyParts.PickRandom();
		}

		private void AddBloodLoss(int amount)
		{
			if (amount <= 0)
			{
				return;
			}
			bleedVolume += amount;
			TryBleed();
		}

		private void TryBleed()
		{
			//don't start another coroutine when already bleeding
			if (!IsBleeding)
			{
				IsBleeding = true;
				StartCoroutine(StartBleeding());
			}
		}

		private IEnumerator StartBleeding()
		{
			while (IsBleeding)
			{
				LoseBlood(bleedVolume);

				yield return new WaitForSeconds(bleedRate);
			}
		}

		//ReduceBloodLoss for bandages and stuff in the future?
		[Server]
		public void StopBleeding()
		{
			bleedVolume = 0;
			IsBleeding = false;
		}

		private void LoseBlood(int amount)
		{
			if (amount <= 0)
			{
				return;
			}
			//            Debug.LogFormat("Lost blood: {0}->{1}", BloodLevel, BloodLevel - amount);
			BloodLevel -= amount;
			BloodSplatSize scaleOfTragedy;
			if (amount > 0 && amount < 15)
			{
				scaleOfTragedy = BloodSplatSize.small;
			}
			else if (amount >= 15 && amount < 40)
			{
				scaleOfTragedy = BloodSplatSize.medium;
			}
			else
			{
				scaleOfTragedy = BloodSplatSize.large;
			}
			if (isServer)
			{
				EffectsFactory.Instance.BloodSplat(transform.position, scaleOfTragedy);
			}


			if (BloodLevel <= (int) BloodVolume.SURVIVE)
			{
				Crit();
			}

			if (BloodLevel <= 0)
			{
				Death();
			}
		}

		public override void Death()
		{
			StopBleeding();
			base.Death();
		}

		public void RestoreBodyParts()
		{
			foreach (BodyPartBehaviour bodyPart in BodyParts)
			{
				bodyPart.RestoreDamage();
			}
		}

		public void RestoreBlood()
		{
			BloodLevel = (int) BloodVolume.NORMAL;
		}

		public static float BleedFactor(DamageType damageType)
		{
			float random = Random.Range(-0.2f, 0.2f);
			switch (damageType)
			{
				case DamageType.BRUTE:
					return 0.6f + random;
				case DamageType.BURN:
					return 0.4f + random;
				case DamageType.TOX:
					return 0.2f + random;
			}
			return 0;
		}

		protected override void OnDeathActions()
		{
			if (CustomNetworkManager.Instance._isServer)
			{
				PlayerNetworkActions pna = gameObject.GetComponent<PlayerNetworkActions>();
				PlayerMove pm = gameObject.GetComponent<PlayerMove>();

				ConnectedPlayer player = PlayerList.Instance.Get(gameObject);
				
				string killerName = "stressfull work";
				if (LastDamagedBy != null)
				{
					killerName = PlayerList.Instance.Get(LastDamagedBy).Name;
				}

				if (killerName == player.Name)
				{
					PostToChatMessage.Send(player.Name + " commited suicide", ChatChannel.System); //Killfeed
				}
				else if (killerName.EndsWith(player.Name))
				{
					// chain reactions
					PostToChatMessage.Send(
						player.Name + " screwed himself up with some help (" + killerName + ")",
						ChatChannel.System); //Killfeed
				}
				else
				{
					PlayerList.Instance.UpdateKillScore(LastDamagedBy, gameObject);

					string departmentKillText = "";
					if (LastDamagedBy != null)
					{
						JobDepartment killerDepartment =
							SpawnPoint.GetJobDepartment(LastDamagedBy.GetComponent<PlayerScript>().JobType);
						JobDepartment victimDepartment =
							SpawnPoint.GetJobDepartment(gameObject.GetComponent<PlayerScript>().JobType);

						if (killerDepartment == victimDepartment)
						{
							departmentKillText = ", losing " + killerDepartment.GetDescription() +
							                     " 1 point for team killing!";
						}
						else
						{
							departmentKillText = ", 1 point to " + killerDepartment.GetDescription() + "!";
						}
					}

					//TDM demo killfeed
					PostToChatMessage.Send(killerName + " has killed " + player.Name + departmentKillText,
						ChatChannel.System);

					//Combat demo killfeed
					//PostToChatMessage.Send(killerName + " has killed " + gameObject.name, ChatChannel.System);
				}
				pna.ValidateDropItem("rightHand", true, transform.position);
				pna.ValidateDropItem("leftHand", true, transform.position);

				if (isServer)
				{
					EffectsFactory.Instance.BloodSplat(transform.position, BloodSplatSize.large);
				}

				pna.RpcSpawnGhost();
				pm.isGhost = true;
				pm.allowInput = true;
				RpcPassBullets(gameObject);
				PlayerDeathMessage.Send(gameObject);

				//FIXME Remove for next demo
				pna.RespawnPlayer(10);
			}
		}

		[ClientRpc]
		private void RpcPassBullets(GameObject target)
		{
			foreach (BoxCollider2D comp in target.GetComponents<BoxCollider2D>())
			{
				if (!comp.isTrigger)
				{
					comp.enabled = false;
				}
			}
		}

		[Server]
		public virtual void Harvest()
		{
			foreach (GameObject harvestPrefab in butcherResults)
			{
				ItemFactory.SpawnItem(harvestPrefab, transform.position, transform.parent);
			}
			EffectsFactory.Instance.BloodSplat(transform.position, BloodSplatSize.medium);
			//Remove the NPC after all has been harvested
			ObjectBehaviour objectBehaviour = gameObject.GetComponent<ObjectBehaviour>();
			objectBehaviour.visibleState = false;
		}
	}
}