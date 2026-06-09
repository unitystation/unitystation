using UnityEngine;
using US13.Systems.Inventory;

namespace US13.Items.Weapons.ActivatableWeaponComponents.Server
{
	public class ChangeSizeOnActivate : ServerActivatableWeaponComponent
	{
		private ItemAttributesV2 itemAttributes;

		[SerializeField] private Size activatedSize;
		private Size defaultSize;

		private void Start()
		{
			itemAttributes = GetComponent<ItemAttributesV2>();
			defaultSize = itemAttributes.Size;
		}

		public override void ServerActivateBehaviour(GameObject performer)
		{
			itemAttributes.ServerSetSize(activatedSize);
		}

		public override void ServerDeactivateBehaviour(GameObject performer)
		{
			itemAttributes.ServerSetSize(defaultSize);
		}
	}
}


