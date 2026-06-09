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
	}

	public void OnInventoryMoveServer(InventoryMove move)
	{
		if (move.InventoryMoveType == InventoryMoveType.Add) return;
		List<ItemSlot> handSlots = move.FromPlayer?.PlayerScript?.DynamicItemStorage?.GetNamedItemSlots(NamedSlot.hands);
		if (handSlots == null) return;
		if (move.ToSlot != null && handSlots.Contains(move.ToSlot)) return;

		Chat.AddWarningMsgFromServer(move.FromPlayer.gameObject,$"The {gameObject.ExpensiveName()} shatters as it leaves your hands!");

		if(bloodReagentMix != null) MatrixManager.ReagentReact(bloodReagentMix, move.FromRootPlayer.WorldPositionServer);
		if(shatterSound != null) SoundManager.PlayNetworkedAtPos(shatterSound, move.FromRootPlayer.WorldPositionServer);
		_ = Despawn.ServerSingle(gameObject);
	}
}
