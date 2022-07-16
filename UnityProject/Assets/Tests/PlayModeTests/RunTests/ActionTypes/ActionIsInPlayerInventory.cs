using System.Collections;
using System.Collections.Generic;
using GameRunTests;
using NaughtyAttributes;
using UnityEngine;
using Util;

// public class ActionIsInPlayerInventory : MonoBehaviour
// {
public partial class TestAction
{
	public bool ShowIsInPlayerInventory => SpecifiedAction == ActionType.IsInPlayerInventory;

	[AllowNesting] [ShowIf(nameof(ShowIsInPlayerInventory))] public IsInPlayerInventory DataIsInPlayerInventory;

	[System.Serializable]
	public class IsInPlayerInventory
	{
		public string MatrixName;

		public bool Inverse;

		public bool NotLocalPlayer;

		[AllowNesting] [ShowIf(nameof(NotLocalPlayer))] public Vector3 WorldPositionOfPlayer;

		public GameObject ObjectToSearchFor;

		public bool TargetSpecifiedSlot;
		[AllowNesting] [ShowIf(nameof(TargetSpecifiedSlot))] public NamedSlot TargetSlots = NamedSlot.none;

		public bool IncludeSubInventories;

		public string CustomFailedText;

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

			List<ItemSlot> ToCheck = null;

			if (TargetSpecifiedSlot)
			{
				ToCheck = DynamicItemStorage.GetNamedItemSlots(TargetSlots);
			}
			else
			{
				ToCheck = DynamicItemStorage.ServerTotal;
			}

			var OriginalID = ObjectToSearchFor.GetComponent<PrefabTracker>().ForeverID;

			foreach (var slot in ToCheck)
			{

				bool Found = false;
				if (slot.Item != null)
				{
					var Tracker = slot.Item.GetComponent<PrefabTracker>();
					if (Tracker != null)
					{
						if (Tracker.ForeverID == OriginalID)
						{
							Found = true;
						}
					}

					if (IncludeSubInventories && Found == false)
					{
						Found =	RecursiveSearch(slot.Item.gameObject, OriginalID);
					}

					if (Found)
					{
						if (Inverse)
						{
							TestRunSO.Report.AppendLine(CustomFailedText);
							TestRunSO.Report.AppendLine($"{ObjectToSearchFor.name} Prefab was found in players inventory ");
							return false;
						}
						else
						{
							return true;
						}
					}
				}
			}

			if (Inverse) //Nothing was found
			{
				return true;
			}
			else //Something wasn't found
			{
				TestRunSO.Report.AppendLine(CustomFailedText);
				TestRunSO.Report.AppendLine($"{ObjectToSearchFor.name} Prefab was not found in players inventory ");
				return false;
			}

		}
	}

	public static bool RecursiveSearch(GameObject ObjectToCheck, string OriginalID)
	{
		var Storage = ObjectToCheck.GetComponent<ItemStorage>();
		if (Storage != null)
		{
			foreach (var itemSlot in Storage.GetItemSlots())
			{
				if (itemSlot.Item != null)
				{
					var Tracker = itemSlot.Item.GetComponent<PrefabTracker>();
					if (Tracker != null)
					{
						if (Tracker.ForeverID == OriginalID)
						{
							return true;
						}
					}

					return RecursiveSearch(itemSlot.Item.gameObject, OriginalID);
				}
			}
		}

		return false;
	}

	public bool InitiateIsInPlayerInventory(TestRunSO TestRunSO)
	{
		return DataIsInPlayerInventory.Initiate(TestRunSO);
	}
}
