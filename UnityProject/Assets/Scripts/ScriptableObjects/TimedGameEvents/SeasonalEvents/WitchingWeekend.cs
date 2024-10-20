using System.Collections;
using System.Collections.Generic;
using Systems.Storage;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ScriptableObjects.TimedGameEvents.SeasonalEvents
{
	[CreateAssetMenu(fileName = "WitchingWeekendTimedEvent", menuName = "ScriptableObjects/Events/TimedGameEvents/WitchingWeekend")]
	public class WitchingWeekend : TimedGameEventSO
	{
		[SerializeField] private List<PlayerSlotStoragePopulator> outfits = new List<PlayerSlotStoragePopulator>();

		public override IEnumerator EventStart()
		{
			PlayerSpawn.OnBodySpawnedEvent += DressUpPlayer;
			return base.EventStart();
		}

		public override void Clean()
		{
			PlayerSpawn.OnBodySpawnedEvent -= DressUpPlayer;
			base.Clean();
		}

		private void DressUpPlayer(GameObject player)
		{
			if (player == null) return;
			if (player.TryGetComponent<PlayerScript>(out var playerScript) == false) return;
			if (player.TryGetComponent<DynamicItemStorage>(out var storage) == false) return;
			var randomTitle = Random.Range(0f, 1f) >= 0.5 ? "Spooky" : "Evil";
			playerScript.characterSettings.Name = randomTitle + " " + playerScript.characterSettings.Name;
			storage.SetUpFromPopulator(outfits.PickRandom());
		}
	}
}