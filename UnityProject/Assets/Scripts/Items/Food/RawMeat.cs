using UnityEngine;

namespace Items
{
	/// <summary>
	/// Main component for raw meat
	/// </summary>
	public class RawMeat : MonoBehaviour
	{
		[SerializeField]
		private GameObject meatSteakPrefab = null;

		private Integrity integrity;
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
			_ = Despawn.ServerSingle(gameObject);
		}
	}
}
