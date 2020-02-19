using System;
using System.Collections;
using UnityEngine;
using Mirror;

/// <summary>
///     Indicates an edible object.
/// </summary>
public class Edible : NetworkBehaviour, IClientInteractable<HandActivate>, IClientInteractable<HandApply>
{
	public GameObject leavings;
	protected bool isDrink = false;

	public int nutritionLevel = 5;

	private void Awake()
	{
		GetComponent<ItemAttributesV2>().AddTrait(CommonTraits.Instance.Food);
	}

	public virtual void TryEat()
	{
		//FIXME: PNA Cmd is being used to heal the player instead of heal hunger for the TDM
		PlayerManager.LocalPlayerScript.playerNetworkActions.CmdEatFood(gameObject,
            UIManager.Hands.CurrentSlot.NamedSlot, isDrink);
	}

	/// <summary>
	/// Used by NPC's' server side
	/// </summary>
	public void NPCTryEat()
	{
		SoundManager.PlayNetworkedAtPos("EatFood", transform.position);
		//Keeping this out allows food to be eaten and disappeared, change to despawn at some point.
		/*if (leavings != null)
		{
			Spawn.ServerPrefab(leavings, transform.position, transform.parent);
		}*/

		GetComponent<CustomNetTransform>().DisappearFromWorldServer();
	}

	public bool Interact(HandActivate interaction)
	{
		//eat on activate
		TryEat();
		return true;
	}

	public bool Interact(HandApply interaction)
	{
		//eat when we hand apply to ourselves
		if (interaction.Performer == PlayerManager.LocalPlayer &&
		    interaction.HandObject == gameObject
		    && interaction.TargetObject == PlayerManager.LocalPlayer)
		{
			TryEat();
			return true;
		}

		return false;
	}
	
}