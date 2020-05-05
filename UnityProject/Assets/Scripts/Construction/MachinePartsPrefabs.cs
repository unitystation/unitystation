using UnityEngine;

namespace Machines
{
	/// <summary>
	/// Singleton, provides common prefabs with special purposes so they can be easily used
	/// in components without needing to be assigned in editor.
	/// </summary>
	[CreateAssetMenu(fileName = "MachinePartsPrefabsSingleton", menuName = "Singleton/MachinePartsPrefabs")]
	public class MachinePartsPrefabs : SingletonScriptableObject<MachinePartsPrefabs>
	{

		public GameObject MicroManipulator;
		public GameObject NanoManipulator;
		public GameObject PicoManipulator;
		public GameObject FemtoManipulator;

		public GameObject BasicMatterBin;
		public GameObject AdvancedMatterBin;
		public GameObject SuperMatterBin;
		public GameObject BluespaceMatterBin;

		public GameObject BasicMicroLaser;
		public GameObject HighPowerMicroLaser;
		public GameObject UltraHighPowerMicroLaser;
		public GameObject QuadUltraMicroLaser;

		public GameObject BasicScanningModule;
		public GameObject AdvancedScanningModule;
		public GameObject PhasicScanningModule;
		public GameObject TriphasicScanningModule;

		public GameObject BasicCapacitor;
		public GameObject AdvancedCapacitor;
		public GameObject SuperCapacitor;
		public GameObject QuadraticCapacitor;
	}
}