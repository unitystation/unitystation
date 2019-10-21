using UnityEngine;

/// <summary>
/// Main component for raw meat
/// </summary>
public class RawMeat : MonoBehaviour
{
	private Integrity integrity;
	private GameObject meatSteakPrefab;
	private RegisterTile registerTile;

	private void Awake()
	{
		integrity = GetComponent<Integrity>();
		integrity.OnBurnUpServer += OnBurnUpServer;
		meatSteakPrefab = Resources.Load<GameObject>("Meat Steak");
		registerTile = GetComponent<RegisterTile>();
	}

	private void OnBurnUpServer(DestructionInfo info)
	{
		//cook the meat by destroying this meat and spawning a meat steak
		PoolManager.PoolNetworkInstantiate(meatSteakPrefab, registerTile.WorldPosition, transform.parent);
		PoolManager.PoolNetworkDestroy(gameObject);
	}
}