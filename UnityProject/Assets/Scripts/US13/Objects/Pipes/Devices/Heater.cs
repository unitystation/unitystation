using System.Collections.Generic;
using UnityEngine;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Sprite_Handler;
using US13.Managers.UpdateManager;
using US13.Systems.Construction;
using US13.Tilemaps.Behaviours.Meta.Atmospherics;
using US13.Tilemaps.Behaviours.Objects;

namespace US13.Objects.Pipes.Devices
{
	public class Heater : MonoBehaviour, IRefreshParts, ICheckedInteractable<HandApply>
	{
		public float PartsMultiplier = 1;

		public bool IsOn = false;
		public bool Heating = false;

		public float TargetTemperature = 293.15f;

		private float MinimumTemperature;
		private float MaximumTemperature;

		public float PowerConsumptionBaseLevel = 30000;

		private float PowerConsumption = 30000;

		private RegisterObject RegisterObject;

		private Machine machine;

		public SpriteHandler SpriteHandler;

		public void Awake()
		{
			RegisterObject = this.GetComponent<RegisterObject>();
			machine = this.GetComponent<Machine>();
		}

		public void RefreshParts(List<PartReference> partsInFrame, Machine Frame)
		{
			PartsMultiplier = Frame.GetPartMultiplier();
			MaximumTemperature = Kelvin.FromC(20 + (30 * PartsMultiplier));
			MinimumTemperature = Kelvin.FromC(20 - (30 * PartsMultiplier));
			PowerConsumption = PowerConsumptionBaseLevel * PartsMultiplier;
		}

		public void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE  , UpdateMe);
			IsOn = false;
			if (SpriteHandler.CataloguePage != 0)
			{
				SpriteHandler.SetCatalogueIndexSprite(0);
			}
		}


		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			return true;
		}

		public void UpdateMe()
		{

			if (machine.CurrentBatteryCapacity > 0)
			{

				var metaNode = RegisterObject.Matrix.MetaDataLayer.Get(transform.localPosition.RoundToInt());

				var Delta = metaNode.GasMixLocal.Temperature - TargetTemperature;

				if (Mathf.Abs(Delta) > 3f)
				{


					if (Delta < 0)
					{
						if (SpriteHandler.CataloguePage != 2)
						{
							SpriteHandler.SetCatalogueIndexSprite(2);
						}
					}
					else
					{
						if (SpriteHandler.CataloguePage != 3)
						{
							SpriteHandler.SetCatalogueIndexSprite(3);
						}
					}


					var Energy = metaNode.GasMixLocal.InternalEnergy;
					var WantedEnergy = metaNode.GasMixLocal.WholeHeatCapacity * TargetTemperature;

					var EnergyDifference = WantedEnergy - Energy;

					var maxDifference = PowerConsumption;

					if (EnergyDifference < 0)
					{
						maxDifference = -PowerConsumption;
					}



					var EnergyToChange  = Mathf.Abs(maxDifference) < Mathf.Abs(EnergyDifference) ? maxDifference : EnergyDifference;;

					metaNode.GasMixLocal.InternalEnergy += EnergyToChange;

					machine.BatteryChangeChargedByDelta(Mathf.RoundToInt(-Mathf.Abs(EnergyToChange) / 50));
				}
				else
				{
					if (SpriteHandler.CataloguePage != 1)
					{
						SpriteHandler.SetCatalogueIndexSprite(1);
					}
				}

			}
			else
			{
				UpdateManager.Remove(CallbackType.PERIODIC_UPDATE  , UpdateMe);
				IsOn = false;
				if (SpriteHandler.CataloguePage != 0)
				{
					SpriteHandler.SetCatalogueIndexSprite(0);
				}
			}



		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (IsOn == false)
			{
				if (machine.CurrentBatteryCapacity > 0)
				{
					IsOn = true;
					UpdateManager.Add(UpdateMe, 1);
					if (SpriteHandler.CataloguePage != 1)
					{
						SpriteHandler.SetCatalogueIndexSprite(1);
					}
				}
			}
			else
			{
				UpdateManager.Remove(CallbackType.PERIODIC_UPDATE  , UpdateMe);
				IsOn = false;
				if (SpriteHandler.CataloguePage != 0)
				{
					SpriteHandler.SetCatalogueIndexSprite(0);
				}
			}
		}
	}
}
