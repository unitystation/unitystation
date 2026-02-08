using UnityEngine;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2;
using US13.Health.Objects;
using US13.HealthV2;
using US13.Items;
using US13.Items.Traits;
using US13.Player;

namespace US13.Systems.Inventory
{
	public class DamageOnPickUp : MonoBehaviour, IServerInventoryMove
	{
		/// <summary>
		/// Does damage to active left or right arm.
		/// </summary>
		public bool doesDamage;

		/// <summary>
		/// 1 = 100%
		/// </summary>
		public float doesDamageChance = 1f;

		public float amountOfDamage = 10f;

		public AttackType attackType;

		public DamageType damageType;

		public ItemTrait[] protectionItemTraits;

		private PlayerScript player;

		public void OnInventoryMoveServer(InventoryMove info)
		{
			if (this.gameObject != info.MovedObject.gameObject) return;
			if (info.InventoryMoveType != InventoryMoveType.Add) return;

			if (info.ToSlot != null && info.ToSlot?.NamedSlot != null)
			{
				player = info.ToRootPlayer?.PlayerScript;

				if (player != null)
				{
					DoDamage(info);
				}
			}
		}

		private void DoDamage(InventoryMove info)
		{
			if (doesDamage && Random.value < doesDamageChance)
			{
				foreach (var trait in protectionItemTraits)
				{
					if (trait == null || Validations.HasItemTrait(player.Equipment.GetClothingItem(NamedSlot.hands).ServerGameObjectReference , trait)) return;
				}

				if (info.ToSlot.NamedSlot == NamedSlot.leftHand)
				{
					player.playerHealth.ApplyDamageToBodyPart(gameObject, amountOfDamage, attackType, damageType, BodyPartType.LeftArm);
				}
				else
				{
					player.playerHealth.ApplyDamageToBodyPart(gameObject, amountOfDamage, attackType, damageType, BodyPartType.RightArm);
				}

				Chat.AddExamineMsgFromServer(player.gameObject, "<color=red>You injure yourself picking up the " + GetComponent<ItemAttributesV2>().ArticleName + "</color>");
			}
		}
	}
}
