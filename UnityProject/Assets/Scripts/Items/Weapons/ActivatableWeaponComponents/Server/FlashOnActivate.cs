using Items;
using Objects;
using UnityEngine;

namespace Weapons.ActivatableWeapons
{
	[RequireComponent(typeof(FlasherBase))]
	public class FlashOnActivate : ServerActivatableWeaponComponent
	{

		private FlasherBase flashComp;

		private void Start()
		{
			flashComp = GetComponent<FlasherBase>();
			var attribs = GetComponent<ItemAttributesV2>();
			attribs.OnBlock += flashComp.FlashInRadius;
		}

		public override void ServerActivateBehaviour(GameObject performer)
		{
			flashComp.FlashInRadius();
		}

		public override void ServerDeactivateBehaviour(GameObject performer)
		{
			//
		}
	}
}