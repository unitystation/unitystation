using HealthV2;
using Logs;
using Mobs.AI;
using Systems.Character;
using UnityEngine;

namespace Mobs
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
			if (Possession.playerSprites.RaceBodyparts is null) Loggy.LogError("[Mob/Awake] Possession.playerSprites.RaceBodyparts is null");
		}

		private void Start()
		{
			MobInit();
		}

		private void MobInit()
		{
			GenerateCharacterSheet();
			Possession.SyncVisibleName("", MobName);
		}

		private void GenerateCharacterSheet()
		{
			var sheet = new CharacterSheet()
			{
				Name = MobName,
				Species = Possession.playerSprites.RaceBodyparts.name,
				PlayerPronoun = MobPronouns
			};
			Possession.characterSettings = sheet;
			Possession.playerSprites.OnCharacterSettingsChange(sheet);
		}
	}
}
