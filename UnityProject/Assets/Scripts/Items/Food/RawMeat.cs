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
		Spawn.ServerPrefab(meatSteakPrefab, registerTile.WorldPosition, transform.parent);
		Despawn.ServerSingle(gameObject);
	}
}