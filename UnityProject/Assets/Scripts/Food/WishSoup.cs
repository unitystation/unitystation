using UnityEngine;
using System.Collections;

public class WishSoup : Edible
{
	private float wishChance;
	public override void Eat(PlayerScript eater, PlayerScript feeder)
	{
		wishChance = Random.value;
		if (wishChance <= 0.25)
		{
			SoundManager.PlayNetworkedAtPos(sound, eater.WorldPos, sourceObj: eater.gameObject);

			eater.playerHealth.Metabolism
				.AddEffect(new MetabolismEffect(NutritionLevel, 0, MetabolismDuration.Food));

			var feederSlot = feeder.ItemStorage.GetActiveHandSlot();
			Inventory.ServerDespawn(gameObject);

			if (leavings != null)
			{
				var leavingsInstance = Spawn.ServerPrefab(leavings).GameObject;
				var pickupable = leavingsInstance.GetComponent<Pickupable>();
				bool added = Inventory.ServerAdd(pickupable, feederSlot);
				if (!added)
				{
					//If stackable has leavings and they couldn't go in the same slot, they should be dropped
					pickupable.CustomNetTransform.SetPosition(feeder.WorldPos);
				}
			}
		}
		else
		{
			SoundManager.PlayNetworkedAtPos(sound, eater.WorldPos, sourceObj: eater.gameObject);

			eater.playerHealth.Metabolism
				.AddEffect(new MetabolismEffect(0, 0, MetabolismDuration.Food));

			var feederSlot = feeder.ItemStorage.GetActiveHandSlot();
			Inventory.ServerDespawn(gameObject);

			if (leavings != null)
			{
				var leavingsInstance = Spawn.ServerPrefab(leavings).GameObject;
				var pickupable = leavingsInstance.GetComponent<Pickupable>();
				bool added = Inventory.ServerAdd(pickupable, feederSlot);
				if (!added)
				{
					//If stackable has leavings and they couldn't go in the same slot, they should be dropped
					pickupable.CustomNetTransform.SetPosition(feeder.WorldPos);
				}
			}
		}
	}
}
