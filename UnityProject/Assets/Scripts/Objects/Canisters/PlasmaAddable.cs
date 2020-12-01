/**
 * This is a temporary component to be used while we do not have a system for converting solid plasma
 * into liquid plasma. When this is implemented, this component is to be deleted.
 */

using UnityEngine;
using Systems.Atmospherics;

namespace Objects.Atmospherics
{
	public class PlasmaAddable : MonoBehaviour, ICheckedInteractable<HandApply>, IRightClickable
	{
		public GasContainer gasContainer;
		public float molesAdded = 15000f;

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

			interaction.HandObject.GetComponent<Stackable>().ServerConsume(1);
			gasContainer.GasMix.AddGas(Gas.Plasma, molesAdded);
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
