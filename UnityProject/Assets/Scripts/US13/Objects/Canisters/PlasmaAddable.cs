/**
 * This is a temporary component to be used while we do not have a system for converting solid plasma
 * into liquid plasma. When this is implemented, this component is to be deleted.
 */

using UnityEngine;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Items;
using US13.Items.Traits;
using US13.Tilemaps.Behaviours.Meta.Atmospherics.Data;
using US13.UI.Core.RightClick;

namespace US13.Objects.Canisters
{
	public class PlasmaAddable : MonoBehaviour, ICheckedInteractable<HandApply>, IRightClickable
	{
		public GasContainer gasContainer;
		public float molesAdded = 15000f;

		public float temperatureKelvin  = 293.15f;

		public float maxPressure  = 15000;
		void Awake()
		{
			gasContainer = GetComponent<GasContainer>();
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side))
			{
				return false;
			}

			if (interaction.TargetObject != gameObject
				|| interaction.HandObject == null
				|| !Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.SolidPlasma))
			{
				return false;
			}

			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			var handObj = interaction.HandObject;

			if (!Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.SolidPlasma))
			{
				return;
			}

			if (gasContainer.GasMixLocal.Pressure >= maxPressure)
			{
				Chat.AddExamineMsg(interaction.Performer, " The gas canister is too high pressure for you to fit the plasma into ");
				return;
			}

			interaction.HandObject.GetComponent<Stackable>().ServerConsume(1);
			gasContainer.GasMixLocal.AddGasWithTemperature(Gas.Plasma, molesAdded, temperatureKelvin);
		}

		public RightClickableResult GenerateRightClickOptions()
		{
			var result = RightClickableResult.Create();

			if (WillInteract(HandApply.ByLocalPlayer(gameObject), NetworkSide.Client))
			{
				result.AddElement("Add Solid Plasma", RightClickInteract);
			}

			return result;
		}

		private void RightClickInteract()
		{
			InteractionUtils.RequestInteract(HandApply.ByLocalPlayer(gameObject), this);
		}
	}
}
