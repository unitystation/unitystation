using UnityEngine;

/// <summary>
/// Allows an object to have an associated network tab that pops up when clicked.
/// If there are additional interactions that can be done on this object
/// please ensure this component is placed below them, otherwise the tab open/close will
/// be the interaction that always takes precedence.
/// </summary>
public class HasNetworkTab : Interactable<HandApply>
{
	[Tooltip("Network tab to display.")]
	public NetTabType NetTabType = NetTabType.None;


	protected override InteractionValidationChain<HandApply> InteractionValidationChain()
	{
		return CommonValidationChains.CAN_APPLY_HAND_CONSCIOUS
			.WithValidation(TargetIs.GameObject(gameObject));
	}

	protected override void ServerPerformInteraction(HandApply interaction)
	{
		TabUpdateMessage.Send( interaction.Performer, gameObject, NetTabType, TabAction.Open );
	}
}
