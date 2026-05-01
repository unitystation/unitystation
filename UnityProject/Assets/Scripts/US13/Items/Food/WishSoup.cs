using UnityEngine;
using US13.Core.Lifecycle;
using US13.Player;
using US13.Systems.Inventory;

namespace US13.Items.Food
{
	/// <summary>
	/// Edible with a 25% chance to actually provide nutrients. Does NOT call base.Eat.
	/// </summary>
	public class WishSoup : Edible
	{
		protected override void Eat(PlayerScript eater, PlayerScript feeder, bool projectileFed = false)
		{
			float wishChance = Random.value;
			if (wishChance <= 0.25)
			{
				Eat(eater, feeder, true);
			}
			else
			{
				Eat(eater, feeder, false);
			}
		}

		private void Eat(PlayerScript eater, PlayerScript feeder, bool feedNutrients, bool projectileFed = false)
		{
			// TODO: sound missing?
			//SoundManager.PlayNetworkedAtPos(sound, eater.WorldPos, sourceObj: eater.gameObject);

			if (feedNutrients)
			{
				var stomachs = eater.playerHealth.GetStomachs();
				if (stomachs.Count == 0)
				{
					//No stomachs?!
					return;
				}
				foodContents.Divide(stomachs.Count);
				foreach (var stomach in stomachs)
				{
					stomach.StomachContents.Add(foodContents.CurrentReagentMix.Clone());
				}
			}

			InvokeOnConsumed(eater.gameObject, feeder.gameObject);
			var feederSlot = feeder.DynamicItemStorage.GetActiveHandSlot();
			_ = Inventory.ServerDespawn(gameObject);

			if (leavings != null)
			{
				var leavingsInstance = Spawn.ServerPrefab(leavings).GameObject;
				var pickupable = leavingsInstance.GetComponent<Pickupable>();
				bool added = Inventory.ServerAdd(pickupable, feederSlot);
				if (added == false)
				{
					//If stackable has leavings and they couldn't go in the same slot, they should be dropped
					pickupable.UniversalObjectPhysics.AppearAtWorldPositionServer(feeder.WorldPos);
				}
			}
		}
	}
}
