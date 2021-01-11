using UnityEngine;

/// <summary>
/// Handles the change of StepType when players equip or unequip this item
/// </summary>
public class StepChanger : MonoBehaviour, IServerInventoryMove
{
	[SerializeField] private FloorSounds SoundChange;

	private RegisterPlayer Player;

	public void OnInventoryMoveServer(InventoryMove info)
	{
		// NamedSlot slot = wearType == WearType.hardsuit ? NamedSlot.outerwear : NamedSlot.feet;

		if (info.FromPlayer != null)
		{
			var mind =info.FromPlayer.PlayerScript.mind;
			if (mind.StepSound == SoundChange)
			{
				mind.StepSound = null;
			}
		}

		if (info.ToPlayer != null)
		{
			var mind =info.ToPlayer.PlayerScript.mind;
			if (mind.StepSound == null)
			{
				Player = info.ToPlayer;
				mind.StepSound = SoundChange;
			}

		}



		//Wearing
		// if (info.ToSlot != null && info.ToSlot?.NamedSlot != null)
		// {
			// var mind = info.ToRootPlayer.PlayerScript.mind;
			// if(mind != null && info.ToSlot.NamedSlot == slot)
			// {
				// TryChange(mind, info.ToSlot.NamedSlot, info.ToPlayer);
			// }
		// }
		//taking off
		// if (info.FromSlot != null && info.FromSlot?.NamedSlot != null)
		// {
			// var mind = info.FromPlayer.PlayerScript.mind;
			// if(mind != null && info.FromSlot.NamedSlot == slot)
			// {
				// TryChange(mind, info.FromSlot.NamedSlot, info.FromPlayer, true);
			// }
		// }
	}

	private void TryChange(Mind mind, NamedSlot? changeSlot, RegisterPlayer player, bool removing = false)
	{
		// if (!HasHardsuit(mind, changeSlot))
		// {
		// 	if (removing && changeSlot == NamedSlot.outerwear)
		// 	{
		// 		WearType feet = WearType.barefoot;
		// 		var tryGetItem = player.PlayerScript.Equipment.ItemStorage.GetNamedItemSlot(NamedSlot.feet).Item;
		// 		if (tryGetItem != null)
		// 		{
		// 			var stepChanger = tryGetItem.GetComponent<StepChanger>();
		// 			if (stepChanger != null)
		// 			{
		// 				feet = stepChanger.wearType;
		// 			}
		// 		}
		//
		// 		mind.stepType = (StepType)feet;
		// 		return;
		// 	}
		//
		// 	if (removing && changeSlot == NamedSlot.feet)
		// 	{
		// 		mind.stepType = StepType.Barefoot;
		// 		return;
		// 	}
		//
		// 	mind.stepType = (StepType)wearType;
		// }
	}

	// private bool HasHardsuit(Mind mind, NamedSlot? changeSlot) => mind.stepType == StepType.Suit && changeSlot != NamedSlot.outerwear;

	private enum WearType
	{
		barefoot = StepType.Barefoot,
		shoes = StepType.Shoes,
		// clownshoes = StepType.Clown,
		// hardsuit = StepType.Suit
	}
}
