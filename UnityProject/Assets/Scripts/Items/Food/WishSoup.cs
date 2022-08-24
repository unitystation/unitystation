using UnityEngine;
using System.Collections;

namespace Items.Food
{
	public class WishSoup : Edible
	{
		public override void Eat(PlayerScript eater, PlayerScript feeder)
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

		private void Eat(PlayerScript eater, PlayerScript feeder, bool feedNutrients)
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
				FoodContents.Divide(stomachs.Count);
				foreach (var stomach in stomachs)
				{
					stomach.StomachContents.Add(FoodContents.CurrentReagentMix.Clone());
				}
			}

			var feederSlot = feeder.DynamicItemStorage.GetActiveHandSlot();
			_ = Inventory.ServerDespawn(gameObject);

			if (leavings != null)
			{
				var leavingsInstance = Spawn.ServerPrefab(leavings).GameObject;
				var pickupable = leavingsInstance.GetComponent<Pickupable>();
				bool added = Inventory.ServerAdd(pickupable, feederSlot);
				if (!added)
				{
					//If stackable has leavings and they couldn't go in the same slot, they should be dropped
					pickupable.UniversalObjectPhysics.AppearAtWorldPositionServer(feeder.WorldPos);
				}
			}
		}
	}
}
