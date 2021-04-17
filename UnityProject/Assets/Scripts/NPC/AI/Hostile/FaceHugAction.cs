using System;
using System.Collections.Generic;
using UnityEngine;
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

		protected override void ActOnLivingV2(Vector3 dir, LivingHealthMasterBase healthBehaviour)
		{
			TryFacehug(dir, healthBehaviour);
		}
		private void TryFacehug(Vector3 dir, LivingHealthMasterBase player)
		{
			var playerInventory = player.gameObject.GetComponent<PlayerScript>()?.Equipment;

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
				player.gameObject,
				BodyPartType.Head,
				null,
				verb);

			AudioSourceParameters audioSourceParameters = new AudioSourceParameters(pitch: 1f);
			SoundManager.PlayNetworkedAtPos(bite, player.gameObject.RegisterTile().WorldPositionServer,
				audioSourceParameters, true, player.gameObject);

			if (success)
			{
				RegisterPlayer registerPlayer = player.gameObject.GetComponent<RegisterPlayer>();
				Facehug(playerInventory, registerPlayer);
			}

		}
		private void Facehug(Equipment playerInventory, RegisterPlayer player)
		{
			var result = Spawn.ServerPrefab(maskObject);
			var mask = result.GameObject;

			Inventory.ServerAdd(
				mask,
				playerInventory.ItemStorage.GetNamedItemSlot(NamedSlot.mask),
				ReplacementStrategy.DespawnOther);

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

			foreach (var slot in faceSlots)
			{
				var item = equipment.ItemStorage.GetNamedItemSlot(slot)?.Item;
				if (item == null || item.gameObject == null)
				{
					continue;
				}

				if (!Validations.HasItemTrait(item.gameObject, CommonTraits.Instance.AntiFacehugger))
				{
					Inventory.ServerDrop(equipment.ItemStorage.GetNamedItemSlot(slot));
				}
				else
				{
					var integrity = item.gameObject.GetComponent<Integrity>();
					if (integrity != null)
					{
						// Your protection might break!
						integrity.ApplyDamage(7.5f, AttackType.Melee, DamageType.Brute);
					}
					antiHugger = true;
				}
			}
			return antiHugger;
		}
		private readonly List<NamedSlot> faceSlots = new List<NamedSlot>()
		{
			NamedSlot.eyes,
			NamedSlot.head,
			NamedSlot.mask
		};
	}
}
