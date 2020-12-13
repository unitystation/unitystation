using UnityEngine;
using System.Collections;

public class WishSoup : Edible
{
	public override void Eat(PlayerScript eater, PlayerScript feeder)
	{
		float wishChance = Random.value;
		if (wishChance <= 0.25)
		{
			Eat(eater, feeder, NutritionLevel);
		}
		else
		{
			Eat(eater, feeder, 0);
		}
	}

	private void Eat(PlayerScript eater, PlayerScript feeder, int nutrition)
	{
		SoundManager.PlayNetworkedAtPos(eatSound, eater.WorldPos, sourceObj: eater.gameObject);

		eater.playerHealth.Metabolism.AddEffect(new MetabolismEffect(nutrition, 0, MetabolismDuration.Food));

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
