using System;
using System.Collections.Generic;
using Logs;
using Shared.Managers;
using UnityEngine;
using UnityEngine.Serialization;
using US13.Actions;
using US13.Clothing;
using US13.Core.Addressables;
using US13.Core.Cooldowns;
using US13.Core.Lifecycle;
using US13.Core.Sprite_Handler;
using US13.HealthV2.Living.MedicalChemistry;
using US13.HealthV2.Living.Surgery;
using US13.Items.PDA;
using US13.Items.Traits;
using US13.Managers.NetworkManagement;
using US13.ScriptableObjects;
using US13.ScriptableObjects.Atmospherics;
using US13.Systems.Construction;
using US13.Systems.CraftingV2;
using US13.Systems.Fluids;
using US13.Systems.Occupations;
using US13.Systems.Spells;
using US13.UI.Items.PDA;

namespace US13.Managers
{
	/// <summary>
	/// In order for the SingletonScriptableObject to work, the singleton instance must
	/// be mapped into this component. Otherwise Unity won't include the
	/// asset in the build (singleton will work only in editor).
	/// </summary>
	public class SOs : SingletonManager<SOs>
	{
		// Put all singletons here (and in the dictionary below) and assign them in editor.
		public AlcoholicDrinksSOScript AlcoholicDrinksSOScript;
		public BestSlotForTrait BestSlotForTrait;
		public CommonCooldowns CommonCooldowns;
		public CommonPrefabs CommonPrefabs;
		public CommonSounds CommonSounds;
		public CommonTraits CommonTraits;
		public CraftingRecipeSingleton CraftingRecipeSingleton;
		public DepartmentList DepartmentList;
		public GasesSingleton GasesSingleton;
		public GasMixesSingleton GasMixesSingleton;
		public ItemTypeToTraitMapping ItemTypeToTraitMapping;
		public MachinePartsItemTraits MachinePartsItemTraits;
		public MachinePartsPrefabs MachinePartsPrefabs;
		public OccupationList OccupationList;
		public PipeTileSingleton PipeTileSingleton;
		public PoolConfig PoolConfig;
		public RaceSOSingleton RaceSOSingleton;
		public SOAdminJobsList AdminJobsList;
		public SpellList SpellList;
		public SpeechModManager SpeechModManager;
		public SpriteCatalogue SpriteCatalogue;
		public SurgeryProcedureBaseSingleton SurgeryProcedureBaseSingleton;
		public UIActionSOSingleton UIActionSOSingleton;
		public UplinkCategoryList UplinkCategoryList;
		public UplinkPasswordList UplinkPasswordList;
		public ThrusterFuelReactions ThrusterFuelReactions;
		public CommonSicknesses CommonSicknesses;

		public SpawnPointSpritesSingleton SpawnPointSpritesSingleton;

		public CommonTiles CommonTiles;

		public CommonMaterials CommonMaterials;

		public CommonReagents CommonReagents;

		[FormerlySerializedAs("PlayerStatesSingleton")]
		public PlayerTypeSingleton playerTypeSingleton;

		private Dictionary<Type, ScriptableObject> typeSOMap;

		public CommonSpriteDataSOs CommonSpriteDataSOs;

		public override void Awake()
		{
			base.Awake();
			typeSOMap = new Dictionary<Type, ScriptableObject>()
			{
				{ typeof(AlcoholicDrinksSOScript), AlcoholicDrinksSOScript },
				{ typeof(BestSlotForTrait), BestSlotForTrait },
				{ typeof(CommonCooldowns), CommonCooldowns },
				{ typeof(CommonPrefabs), CommonPrefabs },
				{ typeof(CommonSounds), CommonSounds },
				{ typeof(CommonTraits), CommonTraits },
				{ typeof(CraftingRecipeSingleton), CraftingRecipeSingleton },
				{ typeof(DepartmentList), DepartmentList },
				{ typeof(GasesSingleton), GasesSingleton },
				{ typeof(GasMixesSingleton), GasMixesSingleton },
				{ typeof(ItemTypeToTraitMapping), ItemTypeToTraitMapping },
				{ typeof(MachinePartsItemTraits), MachinePartsItemTraits },
				{ typeof(MachinePartsPrefabs), MachinePartsPrefabs },
				{ typeof(OccupationList), OccupationList },
				{ typeof(PipeTileSingleton), PipeTileSingleton },
				{ typeof(PoolConfig), PoolConfig },
				{ typeof(RaceSOSingleton), RaceSOSingleton },
				{ typeof(SOAdminJobsList), AdminJobsList },
				{ typeof(SpellList), SpellList },
				{ typeof(SpeechModManager), SpeechModManager },
				{ typeof(SpriteCatalogue), SpriteCatalogue },
				{ typeof(SurgeryProcedureBaseSingleton), SurgeryProcedureBaseSingleton },
				{ typeof(UIActionSOSingleton), UIActionSOSingleton },
				{ typeof(UplinkCategoryList), UplinkCategoryList },
				{ typeof(UplinkPasswordList), UplinkPasswordList },
				{ typeof(PlayerTypeSingleton), playerTypeSingleton },
				{ typeof(ThrusterFuelReactions), ThrusterFuelReactions },
				{ typeof(SpawnPointSpritesSingleton), SpawnPointSpritesSingleton },
				{ typeof(CommonTiles), CommonTiles },
				{ typeof(CommonMaterials), CommonMaterials },
				{ typeof(CommonReagents), CommonReagents },
				{ typeof(CommonSpriteDataSOs), CommonSpriteDataSOs },
				{ typeof(CommonSicknesses), CommonSicknesses },
			};
		}

		public T GetEntry<T>() where T : ScriptableObject
		{
			if (typeSOMap.TryGetValue(typeof(T), out ScriptableObject value))
			{
				if (value == null)
				{
					Loggy.Error($"{typeof(T).FullName} is not assigned to {gameObject.name} prefab.");
					return null;
				}

				return value as T;
			}

			Loggy.Warning($"{nameof(SOs)} is missing entry for {typeof(T).FullName}.");
			return default;
		}
	}
}
