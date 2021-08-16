using System;
using System.Collections.Generic;
using System.Linq;
using Clothing;
using UnityEngine;
using Doors;
using Systems.Mob;
using Random = UnityEngine.Random;
using AddressableReferences;
using HealthV2;
using Messages.Server.SoundMessages;
using UnityEngine.Serialization;


namespace Systems.MobAIs
{
	public class FaceHugAction : MobMeleeAction
	{
		[SerializeField] private GameObject maskObject = null;
		public GameObject MaskObject {get{return maskObject;}}
		[FormerlySerializedAs("Bite")] [SerializeField] private AddressableAudioSource bite = null;

		protected override void ActOnLivingV2(Vector3 dir, LivingHealthMasterBase livingHealth)
		{
			TryFacehug(dir, livingHealth);
		}
		private void TryFacehug(Vector3 dir, LivingHealthMasterBase livingHealth)
		{
			var playerInventory = livingHealth.gameObject.GetComponent<PlayerScript>()?.Equipment;

			if (playerInventory == null)
			{
				return;
			}

			string verb;
			bool success;

			if (HasAntihuggerItem(playerInventory))
			{
				verb = "tried to hug";
				success = false;
			}
			else
			{
				verb = "hugged";
				success = true;
			}

			ServerDoLerpAnimation(dir);

			Chat.AddAttackMsgToChat(
				gameObject,
				livingHealth.gameObject,
				BodyPartType.Head,
				null,
				verb);

			AudioSourceParameters audioSourceParameters = new AudioSourceParameters(pitch: 1f);
			SoundManager.PlayNetworkedAtPos(bite, livingHealth.gameObject.RegisterTile().WorldPositionServer,
				audioSourceParameters, true, livingHealth.gameObject);

			if (success)
			{
				RegisterPlayer registerPlayer = livingHealth.gameObject.GetComponent<RegisterPlayer>();
				Facehug(playerInventory, registerPlayer);
			}

		}
		private void Facehug(Equipment playerInventory, RegisterPlayer player)
		{
			var result = Spawn.ServerPrefab(maskObject);
			var mask = result.GameObject;

			foreach (var itemSlot in playerInventory.ItemStorage.GetNamedItemSlots(NamedSlot.mask))
			{
				Inventory.ServerAdd(
					mask,
					itemSlot,
					ReplacementStrategy.DespawnOther);
				break;
			}


			_ = Despawn.ServerSingle(gameObject);
		}

		/// <summary>
		/// Check the player inventory for an item in head, mask or eyes slots with
		/// Antifacehugger trait. It also drops all items that doesn't have the trait.
		/// </summary>
		/// <param name="equipment"></param>
		/// <returns>True if the player is protected against huggers, false it not</returns>
		private bool HasAntihuggerItem(Equipment equipment)
		{
			bool antiHugger = false;
			bool DoubleBreak = false;
			foreach (var slot in faceSlots)
			{
				foreach (var itemSlot in equipment.ItemStorage.GetNamedItemSlots(slot))
				{
					var item = itemSlot?.Item;
					if (item == null || item.gameObject == null)
					{
						continue;
					}

					if (!Validations.HasItemTrait(item.gameObject, CommonTraits.Instance.AntiFacehugger))
					{
						Inventory.ServerDrop(itemSlot);
					}
					else
					{
						var integrity = item.gameObject.GetComponent<Integrity>();
						if (integrity != null)
						{
							// Your protection might break!
							integrity.ApplyDamage(7.5f, AttackType.Melee, DamageType.Brute);
						}

						DoubleBreak = true;
						antiHugger = true;
						break;
					}
				}

				if (DoubleBreak)
				{
					break;
				}

			}
			return antiHugger;
		}
		private readonly List<NamedSlot> faceSlots = new List<NamedSlot>()
		{
			NamedSlot.head,
			NamedSlot.eyes,
			NamedSlot.mask
		};
	}
}
