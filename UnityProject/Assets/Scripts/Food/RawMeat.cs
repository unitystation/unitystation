using UnityEngine;

/// <summary>
/// Main component for raw meat
/// </summary>
public class RawMeat : MonoBehaviour
{
	private Integrity integrity;
	[SerializeField]
	private GameObject meatSteakPrefab = null;
	private RegisterTile registerTile;

	private void Awake()
	{
		integrity = GetComponent<Integrity>();
		integrity.OnBurnUpServer += OnBurnUpServer;
		registerTile = GetComponent<RegisterTile>();
	}

	private void OnBurnUpServer(DestructionInfo info)
	{
		//cook the meat by destroying this meat and spawning a meat steak
		Stackable stck = gameObject.GetComponent<Stackable>();
		int numResults = 1;
		if (stck != null)
		{
			numResults = stck.Amount;
		}
		Spawn.ServerPrefab(meatSteakPrefab, registerTile.WorldPosition, transform.parent, count: numResults);
		Despawn.ServerSingle(gameObject);
	}
}