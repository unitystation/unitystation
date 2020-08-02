using System;
using UnityEngine;

namespace Weapons
{
	public class PlasmaRefill : MonoBehaviour, ICheckedInteractable<HandApply>
	{
		private MagazineBehaviour magazineBehaviour;

		private void Start()
		{
			magazineBehaviour = GetComponent<Gun>().CurrentMagazine;
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;

			if (!Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.SolidPlasma)) return false;

			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			var needAmmo = magazineBehaviour.magazineSize - magazineBehaviour.ServerAmmoRemains;
			if (needAmmo <= 0) return;

			var stackable = interaction.HandObject.GetComponent<Stackable>();
			var plasmaInStack = stackable.Amount;
			if (needAmmo >= plasmaInStack)
			{
				magazineBehaviour.ExpendAmmo(-plasmaInStack);
				stackable.ServerConsume(plasmaInStack);
			}
			else if (needAmmo < plasmaInStack)
			{
				magazineBehaviour.ExpendAmmo(-needAmmo);
				stackable.ServerConsume(needAmmo);
			}
		}
	}
}
