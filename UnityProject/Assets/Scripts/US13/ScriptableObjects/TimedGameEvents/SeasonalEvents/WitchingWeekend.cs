using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using US13.Core.Lifecycle;
using US13.Managers;
using US13.Player;
using US13.Strings;
using US13.Systems.Inventory;
using US13.Systems.Inventory.Populators.Storage;
using US13.Systems.Occupations;
using Util;
using Event = US13.Managers.Event;
using Random = UnityEngine.Random;

namespace US13.ScriptableObjects.TimedGameEvents.SeasonalEvents
{
	[CreateAssetMenu(fileName = "WitchingWeekendTimedEvent", menuName = "ScriptableObjects/Events/TimedGameEvents/WitchingWeekend")]
	public class WitchingWeekend : TimedGameEventSO
	{
		[SerializeField] private List<PlayerSlotStoragePopulator> outfits = new List<PlayerSlotStoragePopulator>();

		private string randomTitle = "Spooky";

		private const string SPOOKY = "Spooky";
		private const string EVIL = "Evil";

		public override IEnumerator EventStart()
		{
			PlayerSpawn.OnBodySpawnedEvent += DressUpPlayer;
			EventManager.AddHandler(Event.PostRoundStarted, AnnounceEventOnRoundStart);
			return base.EventStart();
		}

		public override void Clean()
		{
			PlayerSpawn.OnBodySpawnedEvent -= DressUpPlayer;
			EventManager.RemoveHandler(Event.PostRoundStarted, AnnounceEventOnRoundStart);
			base.Clean();
		}

		private void DressUpPlayer(GameObject player)
		{
			if (player == null) return;
			if (player.TryGetComponent<PlayerScript>(out var playerScript) == false) return;
			if (playerScript.characterSettings == null) return;
			if (playerScript.characterSettings.Name.Contains(SPOOKY) || playerScript.characterSettings.Name.Contains(EVIL)) return;
			if (playerScript.Mind.occupation.JobType is
			    JobType.AI or JobType.CYBORG or // we don't want robots to be spooky.
			    JobType.CAPTAIN or JobType.HOP or JobType.HOS or // probably not
			    JobType.WARDEN or JobType.SECURITY_OFFICER or // be serious
			    JobType.ASHWALKER or JobType.NULL or JobType.FUGITIVE or JobType.ANCIENT_ENGINEER) return; // ghost roles are almost likely never part of the crew.
			if (player.TryGetComponent<DynamicItemStorage>(out var storage) == false) return;
			randomTitle = Random.Range(0f, 1f) >= 0.5 ? SPOOKY : EVIL;
			playerScript.characterSettings.Name = randomTitle + " " + playerScript.characterSettings.Name;
			storage.SetUpFromPopulator(outfits.PickRandom());
		}

		private void AnnounceEventOnRoundStart()
		{
			_ = Announcement();
		}

		private async UniTask Announcement()
		{
			await UniTask.WaitForSeconds(10f);
			CentComm.MakeAnnouncement(ChatTemplates.CentcomAnnounce, "Welcome to the witching weekend, a crew exchange event between Nanotrasen and the Wizards Federation." +
			                                                         "\nWe put our differences aside today to celeberate the legends of old that brought science into the world of darkness that engulfed humanity for hundreds - if not thousands - of years on our origin planet, Earth.", CentComm.UpdateSound.Announce);
		}
	}
}