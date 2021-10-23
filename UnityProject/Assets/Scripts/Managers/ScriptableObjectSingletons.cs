using Systems.CraftingV2;
using HealthV2;
using Items.PDA;
using UnityEngine;
using ScriptableObjects;
using ScriptableObjects.Atmospherics;
using Machines;
using Objects.Atmospherics;
using UnityEngine.Serialization;

/// <summary>
/// In order for the SingletonScriptableObject to work, the singleton instance must
/// be mapped into this component. Otherwise Unity won't include the
/// asset in the build (singleton will work only in editor).
/// </summary>
public class ScriptableObjectSingletons : MonoBehaviour
{
	//put all singletons here and assign them in editor.
	public ItemTypeToTraitMapping ItemTypeToTraitMapping;
	public CommonTraits CommonTraits;
	public DepartmentList DepartmentList;
	public OccupationList OccupationList;
	public BestSlotForTrait BestSlotForTrait;
	public PlayerTextureDataSOs PlayerTextureDataSOs;
	public CommonPrefabs CommonPrefabs;
	public CommonCooldowns CommonCooldowns;
	public UIActionSOSingleton UIActionSOSingleton;
	public SpeechModManager SpeechModManager;
	public MachinePartsItemTraits MachinePartsItemTraits;
	public MachinePartsPrefabs MachinePartsPrefabs;
	public SOAdminJobsList AdminJobsList;
	public PoolConfig PoolConfig;
	public UplinkCategoryList UplinkCategoryList;
	public UplinkPasswordList UplinkPasswordList;
	public PipeTileSingleton PipeTileSingleton;
	public AlcoholicDrinksSOScript AlcoholicDrinksSOScript;
	public SpriteCatalogue SpriteCatalogue;
	[FormerlySerializedAs("SingletonSOSounds")] public CommonSounds commonSounds;
	public RaceSOSingleton RaceSOSingleton;
	public GAS2ReagentSingleton GAS2ReagentSingleton;
	public SurgeryProcedureBaseSingleton SurgeryProcedureBaseSingleton;
	public GasesSingleton GasesSingleton;
	public GasMixesSingleton GasMixesSingleton;
	public CraftingRecipeSingleton CraftingRecipeSingleton;
}
