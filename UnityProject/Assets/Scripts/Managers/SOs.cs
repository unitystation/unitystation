using System;
using System.Collections.Generic;
using HealthV2;
using Items.PDA;
using Logs;
using Machines;
using Objects.Atmospherics;
using ScriptableObjects.Atmospherics;
using ScriptableObjects.Systems.Spells;
using Shared.Managers;
using Systems.CraftingV2;
using UnityEngine;
using UnityEngine.Serialization;

namespace ScriptableObjects
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

		[FormerlySerializedAs("PlayerStatesSingleton")]
		public PlayerTypeSingleton playerTypeSingleton;

		private Dictionary<Type, ScriptableObject> typeSOMap;

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
			};
		}

		public T GetEntry<T>() where T : ScriptableObject
		{
			if (typeSOMap.TryGetValue(typeof(T), out ScriptableObject value))
			{
				if (value == null)
				{
					Loggy.LogError($"{typeof(T).FullName} is not assigned to {gameObject.name} prefab.");
					return null;
				}

				return value as T;
			}

			Loggy.LogWarning($"{nameof(SOs)} is missing entry for {typeof(T).FullName}.");
			return default;
		}
	}
}
