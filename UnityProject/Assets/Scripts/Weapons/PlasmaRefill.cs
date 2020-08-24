using UnityEngine;

namespace Weapons
{
	public class PlasmaRefill : MonoBehaviour, ICheckedInteractable<HandApply>, ICheckedInteractable<InventoryApply>
	{
		private MagazineBehaviour magazineBehaviour;

		private void Start()
		{
			magazineBehaviour = GetComponent<Gun>().CurrentMagazine;
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;

			if (Validations.HasItemTrait(interaction.HandObject,
				CommonTraits.Instance.SolidPlasma) == false) return false;

			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (!Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.SolidPlasma)) return;
			var needAmmo = magazineBehaviour.magazineSize - magazineBehaviour.ServerAmmoRemains;
			if (needAmmo <= 0) return;

			var stackable = interaction.HandObject.GetComponent<Stackable>();
			Refill(stackable, needAmmo);
		}

		private void Refill(Stackable stackable, int needAmmo)
		{
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

		public bool WillInteract(InventoryApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;

			if (interaction.FromSlot.Item == null) return false;
			if (Validations.HasItemTrait(interaction.FromSlot.Item.gameObject,
				CommonTraits.Instance.SolidPlasma) == false) return false;

			return true;
		}

		public void ServerPerformInteraction(InventoryApply interaction)
		{
			var needAmmo = magazineBehaviour.magazineSize - magazineBehaviour.ServerAmmoRemains;
			if (needAmmo <= 0) return;

			var stackable = interaction.FromSlot.Item.GetComponent<Stackable>();
			Refill(stackable, needAmmo);
		}
	}
}
