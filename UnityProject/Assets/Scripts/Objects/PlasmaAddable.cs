
/**
 * This is a temporary component to be used while we do not have a system for converting solid plasma
 * into liquid plasma. When this is implemented, this component is to be deleted.
 */

using Atmospherics;
using Objects;
using UnityEngine;

public class PlasmaAddable : NBHandApplyInteractable, IRightClickable
{
	public GasContainer gasContainer;
	public float molesAdded = 15000f;

	protected override bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!base.WillInteract(interaction, side))
		{
			return false;
		}

		if (interaction.TargetObject != gameObject
		    || interaction.HandObject == null
		    || interaction.HandObject.GetComponent<SolidPlasma>() == null)
		{
			return false;
		}

		return true;
	}

	protected override void ServerPerformInteraction(HandApply interaction)
	{
		var handObj = interaction.HandObject;

		if (handObj.GetComponent<SolidPlasma>() == null)
		{
			return;
		}

		var slot = InventoryManager.GetSlotFromOriginatorHand(interaction.Performer, interaction.HandSlot.equipSlot);
		handObj.GetComponent<Pickupable>().DisappearObject(slot);
		
		gasContainer.GasMix = gasContainer.GasMix.AddGasReturn(Gas.Plasma, molesAdded);
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
		Interact(HandApply.ByLocalPlayer(gameObject), nameof(PlasmaAddable));
	}
}
