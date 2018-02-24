using System.Collections;
using PlayGroup;
using UI;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///     Food behaviour. The base for every food item in the game
/// </summary>
public class FoodBehaviour : NetworkBehaviour
{
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
			yield return new WaitForSeconds(0.1f);
			pI = PoolManager.Instance;
		}
		yield return new WaitForEndOfFrame();

		PoolManager.Instance.PoolCacheObject(gameObject);
	}

	public virtual void TryEat()
	{
		//FIXME: PNA Cmd is being used to heal the player instead of heal hunger for the TDM
		PlayerManager.LocalPlayerScript.playerNetworkActions.CmdEatFood(gameObject,
			UIManager.Hands.CurrentSlot.eventName);
	}
}