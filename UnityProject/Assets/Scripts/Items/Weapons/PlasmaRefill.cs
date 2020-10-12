using UnityEngine;
using Items;

namespace Weapons
{
	public class PlasmaRefill : MonoBehaviour, ICheckedInteractable<HandApply>, ICheckedInteractable<InventoryApply>
	{
		public int oreEff = 20;

		// sheets should be about twice as efficent as ore
		public int sheetEff = 40;
		private int consumed;
		private int refilledAmmo;
		private MagazineBehaviour magazineBehaviour;

		private void Start()
		{
			magazineBehaviour = GetComponent<Gun>().CurrentMagazine;
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;

			if (!Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.SolidPlasma)
			 && !Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.OrePlasma))
			{
				return false;
			}

			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{

			// check if what is trying to be used as fuel
			// can actually be used as fuel
			// and if it can, define a variable stating what
			// fuel is being used
			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.SolidPlasma))
			{
				refilledAmmo = sheetEff;
			}
			else if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.OrePlasma))
			{
				refilledAmmo = oreEff;
			}
			else
			{
				return;
			}
			var needAmmo = magazineBehaviour.magazineSize - magazineBehaviour.ServerAmmoRemains;
			if (needAmmo <= 0) return;

			var stackable = interaction.HandObject.GetComponent<Stackable>();
			Refill(stackable, needAmmo, refilledAmmo);
		}

		private void Refill(Stackable stackable, int needAmmo, int refilledAmmo)
		{
			// get the amount of plasma being used to refill us
			var plasmaInStack = stackable.Amount;

			consumed = 0;
			// calculate the amount of fuel we would need to
			// refill us to capacity
			for (int i = needAmmo; i > 0;i -= refilledAmmo)
			{
				consumed++;
			}

			if (plasmaInStack < consumed)
			{
				// we do not have enough fuel to full refill, so calculate how much
				// we can fill with the amount we have
				magazineBehaviour.ServerSetAmmoRemains(magazineBehaviour.ServerAmmoRemains + (plasmaInStack * refilledAmmo));
				// plasmaInStack * refilledAmmo is the amount of ammo we can refill,
				// add this to the current amount of ammo we have as we are using a set method
				// we shouldnt need to clamp this value as it wont refill us to capacity

				// add the amount of projectiles we are loading to the clip's array
				for (int i = (plasmaInStack * refilledAmmo); i > 0; i--)
				{
					magazineBehaviour.LoadProjectile(magazineBehaviour.Projectile, 1);
				}
				// consume the entire stack because the amount cant fill us to capacity
				stackable.ServerConsume(plasmaInStack);
			}
			else
			{
				// we have enough fuel to fill our ammo to capacity
				// so dont bother calculating stuff and set our ammo to max
				magazineBehaviour.ServerSetAmmoRemains(magazineBehaviour.magazineSize);

				// add the amount of projectiles we are loading to the clip's array
				for (int i = needAmmo; i > 0; i--)
				{
					magazineBehaviour.LoadProjectile(magazineBehaviour.Projectile, 1);
				}
				// consume the amount that will leave us refilled to max and leave the rest
				stackable.ServerConsume(consumed);
			}

			if (needAmmo < plasmaInStack)
			{
				for (int i = plasmaInStack; i > 0; i--)
				{
					magazineBehaviour.LoadProjectile(magazineBehaviour.Projectile, 1);
				}
			}
		}

		public bool WillInteract(InventoryApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;

			if (interaction.FromSlot.Item == null) return false;
			if (!Validations.HasItemTrait(interaction.FromSlot.Item.gameObject, CommonTraits.Instance.SolidPlasma)
			 && !Validations.HasItemTrait(interaction.FromSlot.Item.gameObject, CommonTraits.Instance.OrePlasma))
			{
				return false;
			}

			return true;
		}

		public void ServerPerformInteraction(InventoryApply interaction)
		{
			var needAmmo = magazineBehaviour.magazineSize - magazineBehaviour.ServerAmmoRemains;
			if (needAmmo <= 0) return;
			// check if what is trying to be used as fuel
			// can actually be used as fuel
			// and if it can, define a variable stating what
			// fuel is being used
			if (Validations.HasItemTrait(interaction.FromSlot.Item.gameObject, CommonTraits.Instance.SolidPlasma))
			{
				refilledAmmo = sheetEff;
			}
			else if (Validations.HasItemTrait(interaction.FromSlot.Item.gameObject, CommonTraits.Instance.OrePlasma))
			{
				refilledAmmo = oreEff;
			}
			else
			{
				return;
			}
			var stackable = interaction.FromSlot.Item.GetComponent<Stackable>();
			Refill(stackable, needAmmo, refilledAmmo);
		}
	}
}
