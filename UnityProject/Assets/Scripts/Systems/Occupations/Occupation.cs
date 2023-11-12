using System;
using System.Collections.Generic;
using Systems.Clearance;
using AddressableReferences;
using Core.Editor.Attributes;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using ScriptableObjects.Systems.Spells;

/// <summary>
/// Defines all aspects of a particular occupation
/// </summary>
[CreateAssetMenu(fileName = "Occupation", menuName = "Occupation/Occupation", order = 1)]
public class Occupation : ScriptableObject
{
	[Header("Settings")]
	// Settings defining gameplay-related aspects of the job.

	[FormerlySerializedAs("JobType")]
	[SerializeField]
	[Tooltip("Type of occupation.")]
	private JobType jobType = JobType.NULL;
	public JobType JobType => jobType;

	[Tooltip("Whether this is a crew role (to add to crew manifest, security records etc)")]
	[SerializeField]
	private bool isCrewmember = true;
	public bool IsCrewmember => isCrewmember;

	[Tooltip("Whether a late spawn should arrive on the arrivals shuttle or their unique spawn point")]
	[SerializeField]
	private bool lateSpawnIsArrivals = true;
	public bool LateSpawnIsArrivals => lateSpawnIsArrivals;

	[FormerlySerializedAs("InventoryPopulator")]
	[SerializeField]
	[Tooltip("Populator to use to populate the player's inventory" +
			 " on spawn when they choose this occupation.")]
	private ItemStoragePopulator inventoryPopulator = null;
	public ItemStoragePopulator InventoryPopulator => inventoryPopulator;

	[SerializeField]
	[Tooltip("Whether to use the StandardOccupationPopulator too")]
	private bool useStandardPopulator = true;
	public bool UseStandardPopulator => useStandardPopulator;

	[SerializeField]
	[Tooltip("Whether to use the players character settings during spawn (sets player name and race)")]
	private bool useCharacterSettings = true;
	public bool UseCharacterSettings => useCharacterSettings;

	public PlayerHealthData CustomSpeciesOverwrite;


	[FormerlySerializedAs("Limit")]
	[SerializeField]
	[Tooltip("Maximum simultaneous players with this occupation. Set to -1 for unlimited")]
	private int limit = 0;
	public int Limit => limit;

	[SerializeField]
	[Tooltip("Priority for selecting this occupation when requested occupation is not" +
			 " available.")]
	private int priority = 0;
	public int Priority => priority;

	[SerializeField]
	[Tooltip("Default clearance issued to this occupation.")]
	private List<Clearance> issuedClearance = default;
	public List<Clearance> IssuedClearance
	{
		get => issuedClearance;
		set => issuedClearance = value; //Change me to read only when we're ready migrating!
	}

	[SerializeField] [Tooltip("Default clearance issued to this occupation when round is LowPop.")]
	private List<Clearance> issuedLowPopClearance = default;
	public List<Clearance> IssuedLowPopClearance
	{
		get => issuedLowPopClearance;
		set => issuedLowPopClearance = value; //Change me to read only when we're ready migrating!
	}

	[SerializeField]
	[Tooltip("Default spells available for this occupation.")]
	private List<SpellData> spells = null;
	public List<SpellData> Spells => spells;

	[Header("Description")]
	// Information that has no real gameplay impact, but is very useful for the player to see.

	[SerializeField]
	[Tooltip("How much of this job has already been implemented in Unitystation?")]
	[Range(0,100)]
	private int progress = 50;
	public int Progress => progress;


	[FormerlySerializedAs("ChoiceColor")]
	[SerializeField]
	[Tooltip("Color of this occupation's button in the occupation chooser")]
	private Color choiceColor = Color.white;
	public Color ChoiceColor => choiceColor;

	[SerializeField]
	[Tooltip("Sprite showing what the uniform (worn by a player) looks like.")]
	private Sprite previewSprite = null;
	public Sprite PreviewSprite => previewSprite;

	[SerializeField]
	[Tooltip("Display name for this occupation.")]
	private string displayName = null;
	public string DisplayName => displayName;

	[SerializeField]
	[Tooltip("How difficult is this role to play (especially for a new player)?")]
	private OccupationDifficulty difficulty = OccupationDifficulty.Medium;
	public OccupationDifficulty Difficulty => difficulty;

	/// <summary>
	/// How difficult a job is to play.
	/// "Zero" difficulty should be suitable for a player who's completely new to Unitystation.
	/// All other difficulties should depend on:
	/// - The game knowledge required to play the job
	/// - The skill required to successfully perform the job during a round
	/// </summary>
	public enum OccupationDifficulty
	{
		Zero,
		Low,
		Medium,
		High,
		Extreme
	}

	[SerializeField]
	[Tooltip("A concise description of this job's duties, suitable for being displayed on three lines.")]
	[TextArea(3, 3)]
	private string descriptionShort = "";
	public string DescriptionShort => descriptionShort;

	[SerializeField]
	[TextArea(10, 20)]
	[Tooltip("An elaborate job description for newcomers. Should say what playing this job usually entails, similar to descriptionShort.")]
	private string descriptionLong = "";
	public string DescriptionLong => descriptionLong;

	[Header("Custom properties that will be applied \n to new bodies with this occupation")]
	[SerializeField] private SerializableDictionary<string, bool> customProperties = default;
	public SerializableDictionary<string, bool> CustomProperties => customProperties;

	[Header(" Custom properties that will define how the person with this occupation spawns in ")]
	[SerializeReference, SelectImplementation(typeof(OccupationCustomEffectBase))] public List<OccupationCustomEffectBase> BetterCustomProperties = new List<OccupationCustomEffectBase>();


	[Header("If enabled, players with this job can be targeted by antags")]
	[SerializeField] private bool isTargeteable=true;

	public bool IsTargeteable => isTargeteable;

	[BoxGroup("Spawn Banner")]
	[Tooltip("Should this occupation play a sound cue on spawn?")]
	[SerializeField]
	private bool playSound = false;
	public bool PlaySound => playSound;

	[BoxGroup("Spawn Banner")]
	[ShowIf(nameof(playSound))]
	[Tooltip("The sound a player hears when they spawn as this occupation.")]
	[SerializeField]
	private AddressableAudioSource spawnSound = null;

	public AddressableAudioSource SpawnSound => spawnSound;

	[BoxGroup("Spawn Banner")]
	[Tooltip("What color should the text in the spawn banner be for this occupation.")]
	[SerializeField]
	private Color textColor = Color.red;
	public Color TextColor => textColor;

	[BoxGroup("Spawn Banner")]
	[Tooltip("The color for the background of the banner")]
	[SerializeField]
	private Color backgroundColor = Color.red;
	public Color BackgroundColor => backgroundColor;

	[Header("If used will spawn player using this prefab instead of human body.")]
	[SerializeField]
	private GameObject specialPlayerPrefab = null;
	public GameObject SpecialPlayerPrefab => specialPlayerPrefab;


}
