using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///     Indicates an edible object.
/// </summary>
public class FoodBehaviour : NetworkBehaviour, IInteractable<HandActivate>, IInteractable<HandApply>
{
    public GameObject leavings;
    protected bool isDrink = false;

	//TODO remove after deathmatches
	[Header("Being used for TDM")] public int healAmount;

	public int healHungerAmount;

	public override void OnStartServer()
	{
		//If this wasn't spawned via ItemFactory (i.e via map editing) then add it to
		//poolmanager so it can be safely destroyed
		PoolPrefabTracker pT = GetComponent<PoolPrefabTracker>();
		if (pT == null)
		{
			StartCoroutine(WaitForServerLoad());
		}
		base.OnStartServer();
	}

	private IEnumerator WaitForServerLoad()
	{
		//Checking directly in while loop crashes unity
		PoolManager pI = PoolManager.Instance;
		while (pI == null)
		{
			yield return WaitFor.Seconds(0.1f);
			pI = PoolManager.Instance;
		}
		yield return WaitFor.EndOfFrame;

		PoolManager.PoolCacheObject(gameObject);
	}

	public virtual void TryEat()
	{
		//FIXME: PNA Cmd is being used to heal the player instead of heal hunger for the TDM
		PlayerManager.LocalPlayerScript.playerNetworkActions.CmdEatFood(gameObject,
            UIManager.Hands.CurrentSlot.eventName, isDrink);
	}

	public InteractionControl Interact(HandActivate interaction)
	{
		//eat on activate
		TryEat();
		return InteractionControl.STOP_PROCESSING;
	}

	public InteractionControl Interact(HandApply interaction)
	{
		//eat when we hand apply to ourselves
		if (interaction.Performer == PlayerManager.LocalPlayer &&
		    interaction.UsedObject == gameObject)
		{
			TryEat();
			return InteractionControl.STOP_PROCESSING;
		}

		return InteractionControl.CONTINUE_PROCESSING;
	}
}