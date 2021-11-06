using System;
using System.Collections.Generic;
using UnityEngine;
using ScriptableObjects.Atmospherics;
using HealthV2;
using Managers;
using Systems.CraftingV2;
using Items.PDA;
using Machines;
using Objects.Atmospherics;

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
		public GAS2ReagentSingleton GAS2ReagentSingleton;
		public GasesSingleton GasesSingleton;
		public GasMixesSingleton GasMixesSingleton;
		public ItemTypeToTraitMapping ItemTypeToTraitMapping;
		public MachinePartsItemTraits MachinePartsItemTraits;
		public MachinePartsPrefabs MachinePartsPrefabs;
		public OccupationList OccupationList;
		public PipeTileSingleton PipeTileSingleton;
		public PlayerTextureDataSOs PlayerTextureDataSOs;
		public PoolConfig PoolConfig;
		public RaceSOSingleton RaceSOSingleton;
		public SOAdminJobsList AdminJobsList;
		public SpeechModManager SpeechModManager;
		public SpriteCatalogue SpriteCatalogue;
		public SurgeryProcedureBaseSingleton SurgeryProcedureBaseSingleton;
		public UIActionSOSingleton UIActionSOSingleton;
		public UplinkCategoryList UplinkCategoryList;
		public UplinkPasswordList UplinkPasswordList;

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
				{ typeof(GAS2ReagentSingleton), GAS2ReagentSingleton },
				{ typeof(GasesSingleton), GasesSingleton },
				{ typeof(GasMixesSingleton), GasMixesSingleton },
				{ typeof(ItemTypeToTraitMapping), ItemTypeToTraitMapping },
				{ typeof(MachinePartsItemTraits), MachinePartsItemTraits },
				{ typeof(MachinePartsPrefabs), MachinePartsPrefabs },
				{ typeof(OccupationList), OccupationList },
				{ typeof(PipeTileSingleton), PipeTileSingleton },
				{ typeof(PlayerTextureDataSOs), PlayerTextureDataSOs },
				{ typeof(PoolConfig), PoolConfig },
				{ typeof(RaceSOSingleton), RaceSOSingleton },
				{ typeof(SOAdminJobsList), AdminJobsList },
				{ typeof(SpeechModManager), SpeechModManager },
				{ typeof(SpriteCatalogue), SpriteCatalogue },
				{ typeof(SurgeryProcedureBaseSingleton), SurgeryProcedureBaseSingleton },
				{ typeof(UIActionSOSingleton), UIActionSOSingleton },
				{ typeof(UplinkCategoryList), UplinkCategoryList },
				{ typeof(UplinkPasswordList), UplinkPasswordList },
			};
		}

		public T GetEntry<T>() where T : ScriptableObject
		{
			return typeSOMap[typeof(T)] as T;
		}
	}
}
