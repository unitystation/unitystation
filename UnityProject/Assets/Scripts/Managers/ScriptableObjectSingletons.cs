using UnityEngine;
using Machines;

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
	public PlayerCustomisationDataSOs PlayerCustomisationDataSOs;
	public PlayerTextureDataSOs PlayerTextureDataSOs;
	public DefaultPlantDataSOs DefaultPlantDataSOs;
	public CommonPrefabs CommonPrefabs;
	public CommonCooldowns CommonCooldowns;
	public AmmoPrefabs AmmoPrefabs;
	public UIActionSOSingleton UIActionSOSingleton;
	public SpeechModManager SpeechModManager;
	public MachinePartsItemTraits MachinePartsItemTraits;
	public MachinePartsPrefabs MachinePartsPrefabs;
}
