using System;
using UnityEngine;

/// <summary>
/// Indicates that the object is a source of welding fuel
/// </summary>
public class WeldingFuelSource : MonoBehaviour, IInteractable<HandApply>
{
	//caching validation chain to reduce gc
	private InteractionValidationChain<HandApply> validations;

	private void Start()
	{
		validations = InteractionValidationChain<HandApply>.Create()
			.WithValidation(CanApply.ONLY_IF_CONSCIOUS)
			.WithValidation(TargetIs.GameObject(gameObject))
			.WithValidation(DoesUsedObjectHaveComponent<Welder>.DOES);
	}

	public InteractionControl Interact(HandApply interaction)
	{
		if (validations.DoesValidate(interaction, NetworkSide.CLIENT))
		{
			PlayerManager.PlayerScript.playerNetworkActions.CmdRefillWelder(interaction.UsedObject, gameObject);
			return InteractionControl.STOP_PROCESSING;
		}

		return InteractionControl.CONTINUE_PROCESSING;
	}
}