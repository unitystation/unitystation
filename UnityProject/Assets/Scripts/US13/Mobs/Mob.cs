using Logs;
using UnityEngine;
using UnityEngine.Serialization;
using US13.Core.Addressables.Types;
using US13.HealthV2.Living;
using US13.Mobs.AI;
using US13.Player;
using US13.Player.MovementV2;
using US13.Systems.Lobby;
using US13.UI.Systems.Lobby;

namespace US13.Mobs
{
	[RequireComponent(typeof(LivingHealthMasterBase))]
	[RequireComponent(typeof(PlayerScript))]
	[RequireComponent(typeof(MovementSynchronisation))]
	[RequireComponent(typeof(MobAI))]
	[RequireComponent(typeof(MobPathfinderV2))]
	public class Mob : MonoBehaviour
	{
		public LivingHealthMasterBase Health { get; private set; }
		public PlayerScript Possession { get; private set; } = null;
		public MovementSynchronisation Movement { get; private set; } = null;
		public MobAI AI { get; private set; } = null;
		public MobPathfinderV2 Pathfinder { get; private set; } = null;
		[field: SerializeField] public string MobName { get; private set; } = "Nubby";
		[field: SerializeField] public PlayerPronoun MobPronouns { get; private set; } = PlayerPronoun.They_them;

		public bool IsControlledByPlayer => Possession.Mind is not null;


		private void Awake()
		{
			Health ??= GetComponent<LivingHealthMasterBase>();
			Possession ??= GetComponent<PlayerScript>();
			Movement ??= GetComponent<MovementSynchronisation>();
			Pathfinder ??= GetComponent<MobPathfinderV2>();
			AI ??= GetComponent<MobAI>();
			if (Possession.playerSprites.RaceBodyparts is null) Loggy.Error("[Mob/Awake] Possession.playerSprites.RaceBodyparts is null");
		}

		private void Start()
		{
			MobInit();
		}

		private void MobInit()
		{
			GenerateCharacterSheet();
			Possession.PlayerScriptVisible.SyncVisibleName("", MobName);
		}

		private void GenerateCharacterSheet()
		{
			var sheet = new CharacterSheet()
			{
				Name = MobName,
				Species = Possession.playerSprites.RaceBodyparts.name,
				PlayerPronoun = MobPronouns
			};
			Possession.PlayerScriptVisible.SetcharacterSettings(sheet);
			Possession.playerSprites.OnCharacterSettingsChange(sheet);
		}
	}

	[System.Serializable]
	public struct AudibleMobDialogue
	{
		public AddressableAudioSource audioSource;
		[FormerlySerializedAs("Transcription")] public string transcription;
	}
}
