using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using US13.Core.Addressables.Types;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Lifecycle;
using US13.Health.Objects;
using US13.HealthV2;
using US13.HealthV2.Living;
using US13.Items.Traits;
using US13.Managers;
using US13.Messages.Server.SoundMessages;
using US13.Mobs.Equipment;
using US13.Player;
using US13.Systems.Inventory;
using US13.Tilemaps.Behaviours.Objects;
using Util;


namespace US13.NPC.AI.Hostile
{
	public class FaceHugAction : MobMeleeAction
	{
		[SerializeField]
		private GameObject maskObject = null;

		public GameObject MaskObject {get{return maskObject;}}

		[FormerlySerializedAs("Bite")] [SerializeField]
		private AddressableAudioSource bite = null;

		protected override void ActOnLivingV2(Vector3 dir, LivingHealthMasterBase livingHealth)
		{
			TryFacehug(dir, livingHealth);
		}

		private void TryFacehug(Vector3 dir, LivingHealthMasterBase livingHealth)
		{
			if (livingHealth.gameObject.TryGetComponent<PlayerScript>(out var playerScript) == false) return;

			if(playerScript.PlayerType == PlayerTypes.Alien) return;

			var playerInventory = playerScript.Equipment;

			if (playerInventory == null) return;

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
		public static bool HasAntihuggerItem(Equipment equipment)
		{
			bool antiHugger = false;
			bool doubleBreak = false;

			foreach (var slot in FaceSlots)
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

						doubleBreak = true;
						antiHugger = true;
						break;
					}
				}

				if (doubleBreak)
				{
					break;
				}

			}
			return antiHugger;
		}

		private static readonly List<NamedSlot> FaceSlots = new List<NamedSlot>
		{
			NamedSlot.head,
			NamedSlot.eyes,
			NamedSlot.mask
		};
	}
}
