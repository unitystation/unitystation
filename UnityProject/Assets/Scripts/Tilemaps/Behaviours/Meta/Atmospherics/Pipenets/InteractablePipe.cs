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

	protected override bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!base.WillInteract(interaction, side)) return false;
		//only wrench can be used on this
		if (!Validations.IsTool(interaction.HandObject, ToolType.Wrench)) return false;
		return true;
	}

	protected override void ServerPerformInteraction(HandApply interaction)
	{
		pipe.WrenchAct();
	}
}
