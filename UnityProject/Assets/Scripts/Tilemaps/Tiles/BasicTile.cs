using System.Collections.Generic;
using Systems.Interaction;
using NaughtyAttributes;
using ScriptableObjects;
using TileManagement;
using AddressableReferences;
using ScriptableObjects.Audio;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

namespace Tiles
{
	public abstract class BasicTile : LayerTile
	{
		[Tooltip("What it sounds like when walked over")]
		[ShowIf(nameof(passable))]
		public FloorSounds floorTileSounds;

		[Tooltip("can the sounds specified be overridden by objects like clown shoes")]
		public bool CanSoundOverride = false;

		[Tooltip("Allow gases to pass through the cell this tile occupies?")]
		[FormerlySerializedAs("AtmosPassable")]
		[SerializeField]
		private bool atmosPassable = false;

		[Tooltip("Does this tile form a seal against the floor?")]
		[FormerlySerializedAs("IsSealed")]
		[SerializeField]
		private bool isSealed = false;

		[Tooltip("Should this tile get initialized with Space gasmix at round start (e.g. asteroids)?")]
		public bool SpawnWithNoAir;

		[Tooltip("Does this tile allow items / objects to pass through it?")]
		[FormerlySerializedAs("Passable")]
		[SerializeField]
		private bool passable = false;

		[Tooltip("Can this tile be mined?")]
		[FormerlySerializedAs("Mineable")]
		[SerializeField]
		private bool mineable = false;

		[Tooltip("How long does it take to mine this tile?")]
		[SerializeField]
		[ShowIf(nameof(mineable))]
		private float miningTime = 5f;

		[Range(1, 10)]
		[Tooltip("How hard is this tile to mine? Higher means certain tools cannot mine it.")]
		[SerializeField]
		[ShowIf(nameof(mineable))]
		private int miningHardness = 1;

		[Tooltip("Can a player construct above this tile?")]
		public bool constructable;

		[Range(0.0f, 1f)]
		[Tooltip("RadiationPassability 0 = 100% Resistant")]
		[SerializeField]
		public float RadiationPassability = 1;

		[Range(0f, 1f)]
		[Tooltip("ThermalConductivity 0 = 100% Conductivity")]
		[SerializeField]
		public float ThermalConductivity = 0.05f;

		[Range(0f, 1000000f)]
		[Tooltip("Heat Capacity 0 to 1000000")]
		[SerializeField]
		public float HeatCapacity = 10000;


		/// <summary>
		/// Can this tile be mined?
		/// </summary>
		public bool Mineable => mineable;
		public float MiningTime => miningTime;
		public int MiningHardness => miningHardness;

		[Tooltip("Will bullets bounce from this tile?")]
		[SerializeField]
		private bool doesReflectBullet = false;

		public bool DoesReflectBullet => doesReflectBullet;

		[Tooltip("Can this tile be damaged at all?")]
		public bool indestructible;

		[Tooltip("Do explosions have to completely destroy the tile before passing it, Otherwise explosion extends all its energy on the wall")]
		public bool ExplosionImpassable;

		[Tooltip("Blocks interactions for tiles underneath it")]
		public bool BlocksTileInteractionsUnder = true;

		[Tooltip("What things are allowed to pass through this even if it is not passable?")]
		[FormerlySerializedAs("PassableException")]
		[SerializeField]
		private SerializableDictionary<CollisionType, bool> passableException = null;

		[Tooltip("What is this tile's max health?")]
		[FormerlySerializedAs("MaxHealth")]
		[SerializeField]
		private float maxHealth = 0f;

		public float MaxHealth => maxHealth;

		[Tooltip("A damage threshold the attack needs to pass in order to apply damage to this item.")]
		public float damageDeflection = 0;

		[Tooltip("Armor of this tile")]
		[FormerlySerializedAs("Armor")]
		[SerializeField]
		private Armor armor = new Armor
		{
			Melee = 90,
			Bullet = 90,
			Laser = 90,
			Energy = 90,
			Bomb = 90,
			Bio = 100,
			Rad = 100,
			Fire = 100,
			Acid = 90
		};

		/// <summary>
		/// Armor of this tile
		/// </summary>
		public Armor Armor => armor;

		[Tooltip(
			"Interactions which can occur on this tile. They will be checked in the order they appear in this list (top to bottom).")]
		[SerializeField]
		private List<TileInteraction> tileInteractions = null;

		/// <summary>
		/// Interactions which can occur on this tile.
		/// </summary>
		public List<TileInteraction> TileInteractions => tileInteractions;

		[field: SerializeField] public List<TileInteraction> OnTileStartMining { get; private set; } = new List<TileInteraction>();
		[field: SerializeField] public List<TileInteraction> OnTileFinishMining { get; private set; } = new List<TileInteraction>();

		[Tooltip(
			"Tile step interactions which can occur on this tile. They will be checked in the order they appear in this list (top to bottom).")]
		[SerializeField]
		private List<TileStepInteraction> tileStepInteractions = null;

		/// <summary>
		/// Tile step interactions which can occur on this tile.
		/// </summary>
		public List<TileStepInteraction> TileStepInteractions => tileStepInteractions;

		[Tooltip("What object to spawn when it's deconstructed or destroyed.")]
		[SerializeField]
		private GameObject spawnOnDeconstruct = null;

		/// <summary>
		/// Object to spawn when deconstructed.
		/// </summary>
		public GameObject SpawnOnDeconstruct => spawnOnDeconstruct;

		[Tooltip("How much of the object to spawn when it's deconstructed. Defaults to 1 if" +
				 " an object is specified and this is 0.")]
		[SerializeField]
		private int spawnAmountOnDeconstruct = 1;

		/// <summary>
		/// How many of the object to spawn when it's deconstructed.
		/// </summary>
		public int SpawnAmountOnDeconstruct => SpawnOnDeconstruct == null ? 0 : Mathf.Max(1, spawnAmountOnDeconstruct);

		[SerializeField] private LootOnDespawn lootOnDespawn = default;

		public LootOnDespawn LootOnDespawn => lootOnDespawn;

		[SerializeField]
		[Tooltip("The tile that will spawn when this tile is destroyed")]
		private LayerTile toTileWhenDestroyed = null;

		public LayerTile ToTileWhenDestroyed => toTileWhenDestroyed;

		[Tooltip("What object to spawn when it's destroyed.")]
		[SerializeField]
		private SpawnableList spawnOnDestroy = null;

		public SpawnableList SpawnOnDestroy => spawnOnDestroy;

		[SerializeField]
		private DamageOverlaySO damageOverlayList = null;

		public DamageOverlaySO DamageOverlayList => damageOverlayList;

		[SerializeField]
		[Foldout("SoundOnHit")]
		private AddressableAudioSource soundOnHit = null;

		public AddressableAudioSource SoundOnHit => soundOnHit;

		[SerializeField]
		public List<AddressableAudioSource> SoundOnDestroy = new List<AddressableAudioSource>();

		public override void RefreshTile(Vector3Int position, ITilemap tilemap)
		{
			foreach (Vector3Int p in new BoundsInt(-1, -1, 0, 3, 3, 1).allPositionsWithin)
			{
				tilemap.RefreshTile(position + p);
			}
		}

		/// <summary>
		/// Checks if the tile is Passable by the ColliderType
		/// It will return the default Passable bool unless an exception is avalaible in PassableException
		/// </summary>
		/// <param name="colliderType"></param>
		/// <returns>IsPassableFromOutside</returns>
		public bool IsPassable(CollisionType colliderType, Vector3Int origin, MetaTileMap metaTileMap)
		{
			if (LayerType == LayerType.Tables)
			{
				if (metaTileMap.IsTableAt(origin))
				{
					return true;
				}
			}

			if (passableException.ContainsKey(colliderType))
			{
				return passableException[colliderType];
			}
			else
			{
				return passable;
			}
		}

		public bool IsAtmosPassable()
		{
			return atmosPassable;
		}

		public bool IsSpace()
		{
			return IsAtmosPassable() && !isSealed;
		}

		//yeah,This needs to be moved out into its own class
		public virtual bool IsTileRepeated(Matrix4x4 thisTransformMatrix, BasicTile basicTile, Matrix4x4 TransformMatrix, MetaDataNode metaDataNode)
		{
			if (basicTile == this)
			{
				return true;
			}

			return false;
		}
	}
}