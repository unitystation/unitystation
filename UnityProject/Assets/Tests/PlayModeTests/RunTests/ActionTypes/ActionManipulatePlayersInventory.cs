using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameRunTests;
using NaughtyAttributes;
using UnityEngine;

// public class ActionManipulatePlayersInventory : MonoBehaviour
// {
public partial class TestAction
{
	public bool ShowManipulatePlayersInventory => SpecifiedAction == ActionType.ManipulatePlayersInventory;

	[AllowNesting] [ShowIf(nameof(ShowManipulatePlayersInventory))] public ManipulatePlayersInventory InManipulatePlayersInventory;

	[System.Serializable]
	public class ManipulatePlayersInventory
	{
		public string MatrixName;

		public bool NotLocalPlayer;

		[AllowNesting] [ShowIf(nameof(NotLocalPlayer))] public Vector3 WorldPositionOfPlayer;

		public InteractionType Interaction;

		//It will target the first slotThis
		public NamedSlot TargetSlots = NamedSlot.none;

		public enum InteractionType
		{
			Destroy,
			TransferTo,
			Drop
		}



		public bool ShowTransferTo => Interaction == InteractionType.TransferTo;
		[AllowNesting] [ShowIf(nameof(ShowTransferTo))] public NamedSlot TargetSlotsTo;

		public bool Initiate(TestRunSO TestRunSO)
		{
			DynamicItemStorage DynamicItemStorage = null;

			if (NotLocalPlayer)
			{
				var Magix = UsefulFunctions.GetCorrectMatrix(MatrixName, WorldPositionOfPlayer);
				var List = Magix.Matrix.ServerObjects.Get(WorldPositionOfPlayer.ToLocal(Magix).RoundToInt());
				foreach (var registerTile in List)
				{
					if (registerTile.TryGetComponent<DynamicItemStorage>(out DynamicItemStorage))
					{
						break;
					}
				}
			}
			else
			{
				DynamicItemStorage = PlayerManager.LocalPlayerScript.DynamicItemStorage;
			}

			if (DynamicItemStorage == null)
			{
				TestRunSO.Report.AppendLine("Unable to find players inventory"); //IDK Maybe this should be here maybe not
				return false;
			}

			var  Slot = DynamicItemStorage.GetNamedItemSlots(TargetSlots).First();

			switch (Interaction)
			{
				case InteractionType.Drop:
					Inventory.ServerDrop(Slot);
					break;
				case InteractionType.Destroy:
					Inventory.ServerDespawn(Slot);
					break;
				case InteractionType.TransferTo:
					var TOSlot = DynamicItemStorage.GetNamedItemSlots(TargetSlotsTo).First();
					Inventory.ServerTransfer(Slot, TOSlot);
					break;
			}


			return true;
		}

	}
}
