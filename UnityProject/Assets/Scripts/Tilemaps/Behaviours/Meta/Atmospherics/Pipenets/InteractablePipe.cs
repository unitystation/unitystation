using UnityEngine;

/// <summary>
/// TODO: Not much need for this to be separate from Pipe.
/// Component for allowing a Pipe to be interacted with
/// </summary>
[RequireComponent(typeof(Pipe))]
[RequireComponent(typeof(Pickupable))]
public class InteractablePipe : MonoBehaviour, ICheckedInteractable<HandApply>
{
	Pipe pipe;

	public void Awake()
	{
		pipe = GetComponent<Pipe>();
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		//only wrench can be used on this
		if (!Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wrench)) return false;
		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		pipe.ServerWrenchAct();
	}
}
