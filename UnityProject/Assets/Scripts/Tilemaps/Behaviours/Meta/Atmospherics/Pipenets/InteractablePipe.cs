using UnityEngine;

/// <summary>
/// Component for allowing a Pipe to be interacted with
/// </summary>
[RequireComponent(typeof(Pipe))]
[RequireComponent(typeof(Pickupable))]
public class InteractablePipe : NBHandApplyInteractable
{
	Pipe pipe;

	public void Awake()
	{
		pipe = GetComponent<Pipe>();
	}

	protected override InteractionValidationChain<HandApply> InteractionValidationChain()
	{
		return CommonValidationChains.CAN_APPLY_HAND_CONSCIOUS
			.WithValidation(IsToolUsed.OfType(ToolType.Wrench))
			.WithValidation(TargetIs.GameObject(gameObject));
	}

	protected override void ServerPerformInteraction(HandApply interaction)
	{
		pipe.WrenchAct();
	}
}
