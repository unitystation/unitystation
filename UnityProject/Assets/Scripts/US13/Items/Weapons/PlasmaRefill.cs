using UnityEngine;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Items.Traits;
using US13.Systems.Construction.Parts;

namespace US13.Items.Weapons
{
	public class PlasmaRefill : MonoBehaviour, ICheckedInteractable<HandApply>, ICheckedInteractable<InventoryApply>
	{
		public int oreEff = 5000;

		// sheets should be about twice as efficent as ore
		public int sheetEff = 10000;
		private int toRefill;
		private int refilledAmmo;
		private int toConsume;

		private Gun gunComp;

		private void Start()
		{
			gunComp = GetComponent<Gun>();
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

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

			var currentMag = gunComp.CurrentMagazine;

			if (currentMag == null) return;

			var needAmmo = currentMag.magazineSize - currentMag.ServerAmmoRemains;
			if (needAmmo <= 0) return;

			var stackable = interaction.HandObject.GetComponent<Stackable>();
			Refill(stackable, needAmmo, refilledAmmo);
		}

		private void Refill(Stackable stackable, int needAmmo, int ChargingWatts)
		{

			// get the amount of plasma being used to refill
			var plasmaInStack = stackable.Amount;

			toRefill = 0;
			// calculate the amount of fuel we would need to
			// refill to capacity
			for (int i = needAmmo; i > 0;i -= refilledAmmo)
			{
				toRefill++;
			}

			if (plasmaInStack < toRefill)
			{
				// dont have enough plasma to refill to capacity
				toConsume = plasmaInStack;
			}
			else
			{
				// have enough to refill to capacity
				toConsume = toRefill;
			}
			AddCharge((ChargingWatts * toConsume));
			stackable.ServerConsume(toConsume);
		}

		private void AddCharge(int ChargingWatts)
		{
			var mag = gunComp.magSlot.Item;
			if (mag == null) return;

			var battery = mag.GetComponent<Battery>();
			if (battery != null)
			{
				battery.Watts += ChargingWatts;

				if (battery.Watts > battery.MaxWatts)
				{
					battery.Watts = battery.MaxWatts;
				}
			}

			var electricalMagazine = mag.GetComponent<ElectricalMagazine>();
			if (electricalMagazine != null)
			{
				electricalMagazine.AddCharge();
			}

			var GunElectrical = gameObject.GetComponent<GunElectrical>();
			if (GunElectrical != null)
			{
				GunElectrical.UpdateChargeSprite();
			}
		}

		public bool WillInteract(InventoryApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

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

			// check if what is trying to be used as fuel
			// can actually be used as fuel
			// and if it can, define a variable stating what
			// fuel is being used
			if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.SolidPlasma))
			{
				GetComponent<Gun>().LoadMagSound();
				refilledAmmo = sheetEff;
			}
			else if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.OrePlasma))
			{
				refilledAmmo = oreEff;
			}
			else
			{
				return;
			}

			var currentMag = gunComp.CurrentMagazine;

			if (currentMag == null) return;

			var needAmmo = currentMag.magazineSize - currentMag.ServerAmmoRemains;
			if (needAmmo <= 0) return;

			var stackable = interaction.UsedObject.GetComponent<Stackable>();
			Refill(stackable, needAmmo, refilledAmmo);
		}
	}
}
