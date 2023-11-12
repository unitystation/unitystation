using System.Collections.Generic;
using Gateway;
using Systems.Teleport;
using UnityEngine;

namespace Items.Science.Clothing
{
	/// <summary>
	/// A special type of armor that reacts to being hit. Randomly teleporting the wearer to a different location when getting hit.
	/// </summary>
	public class ReactiveArmor : MonoBehaviour, IServerInventoryMove
	{
		private Pickupable pickupable;

		[SerializeField] private List<DamageType> blacklistedDamageTypes = new List<DamageType> {
			DamageType.Clone,
			DamageType.Stamina,
			DamageType.Tox,
			DamageType.Radiation
		};

		private void Awake()
		{
			pickupable = GetComponent<Pickupable>();
		}

		public void OnInventoryMoveServer(InventoryMove info)
		{
			if (info.InventoryMoveType == InventoryMoveType.Remove)
			{
				info.FromRootPlayer.PlayerScript.playerHealth.OnTakeDamageType -= TeleportWearer;
				return;
			}
			if (info.ToSlot?.NamedSlot != NamedSlot.outerwear) return;
			info.FromRootPlayer.PlayerScript.playerHealth.OnTakeDamageType += TeleportWearer;
		}
		private void TeleportWearer(DamageType type, GameObject hitBy, float DMG)
		{
			if (blacklistedDamageTypes.Contains(type)) return;
			if (pickupable.ItemSlot?.Player == null) return;
			var player = pickupable.ItemSlot.Player.PlayerScript.playerMove;
			TransportUtility.TransportObjectAndPulled(player,
				 player.gameObject.AssumedWorldPosServer() + TeleportUtils.RandomTeleportLocation());
		}
	}
}