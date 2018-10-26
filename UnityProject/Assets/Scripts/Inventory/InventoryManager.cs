using UnityEngine;


	public class InventoryManager : MonoBehaviour
	{
		private static InventoryManager inventoryManager;

		public static InventoryManager Instance
		{
			get
			{
				if (!inventoryManager)
				{
					inventoryManager = FindObjectOfType<InventoryManager>();
				}
				return inventoryManager;
			}
		}
	}

	//Helps identify the position in syncEquip list
	public enum Epos
	{
		suit,
		belt,
		head,
		feet,
		face,
		mask,
		uniform,
		leftHand,
		rightHand,
		eyes,
		back,
		hands,
		ear,
		neck
	}
