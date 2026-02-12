using UnityEngine;
using US13.HealthV2;
using US13.Objects;

namespace US13.Items.Weapons.ActivatableWeaponComponents.Server
{
	[RequireComponent(typeof(FlasherBase))]
	public class FlashOnActivate : ServerActivatableWeaponComponent
	{

		private FlasherBase flashComp;
		private ItemAttributesV2 attribs;

		private void Start()
		{
			flashComp = GetComponent<FlasherBase>();
			attribs = GetComponent<ItemAttributesV2>();
			attribs.OnBlock += Flash;
			attribs.OnMelee += TargetedFlash;
		}

		private void OnDestroy()
		{
			attribs.OnBlock -= Flash;
			attribs.OnMelee -= TargetedFlash;
		}

		public override void ServerActivateBehaviour(GameObject performer)
		{
			flashComp.FlashInRadius();
		}

		public override void ServerDeactivateBehaviour(GameObject performer)
		{
			//
		}

		public void Flash(GameObject attacker, float damage, DamageType damageType)
		{
			flashComp.FlashInRadius();
		}

		public void TargetedFlash(GameObject attacker, GameObject target)
		{
			flashComp.FlashTarget(target, flashComp.FlashTime, flashComp.FlashTime);
		}
	}
}