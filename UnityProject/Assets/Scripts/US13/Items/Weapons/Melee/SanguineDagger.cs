using System.Collections.Generic;
using Chemistry;
using UnityEngine;
using US13.ChemistryComponents;
using US13.Core.Addressables.Types;
using US13.Core.Chat;
using US13.Core.Lifecycle;
using US13.Managers;
using US13.Managers.MatrixManager;
using US13.Systems.Inventory;
using Util;

public class SanguineDagger : MonoBehaviour, IServerInventoryMove
{
	[SerializeField] private AddressableAudioSource shatterSound = null;
	private ReagentMix bloodReagentMix = null;

	public void FillReagentMix(ReagentMix reagentMix)
	{
		bloodReagentMix = reagentMix;
		foreach (var reagent in bloodReagentMix.reagents)
		{
			bloodReagentMix.Add(reagent.Key, reagent.Value); //Basically just increases the spilled reagent to look more dramatic
		}
	}
	public void OnInventoryMoveServer(InventoryMove move)
	{
		List<ItemSlot> handSlots = move.FromPlayer.PlayerScript.DynamicItemStorage.GetNamedItemSlots(NamedSlot.hands);

		if (move.ToSlot != null && handSlots.Contains(move.ToSlot)) return;

		Chat.AddWarningMsgFromServer(move.FromPlayer.gameObject,$"The {gameObject.ExpensiveName()} shatters as it leaves your hands!");

		MatrixManager.ReagentReact(bloodReagentMix, move.FromPlayer.WorldPositionServer);
		if(shatterSound != null) SoundManager.PlayNetworkedAtPosAsync(shatterSound, move.FromPlayer.WorldPositionServer, sourceObj: gameObject);
		_ = Despawn.ServerSingle(gameObject);
	}
}
