using UnityEngine;

namespace Machines
{
	/// <summary>
	/// Singleton, provides common ItemTraits with special purposes (such as components
	/// used on many prefabs which automatically cause an object to have particular traits) so they can be easily used
	/// in components without needing to be assigned in editor.
	/// </summary>
	[CreateAssetMenu(fileName = "MachineTraitsSingleton", menuName = "Singleton/Traits/MachinePartTraits")]
	public class MachinePartsItemTraits : SingletonScriptableObject<MachinePartsItemTraits>
	{
		//Catagories
		public ItemTrait Manipulator;
		public ItemTrait MatterBin;
		public ItemTrait MicroLaser;
		public ItemTrait ScanningModule;
		public ItemTrait Capacitor;

		public ItemTrait MicroManipulator;
		public ItemTrait NanoManipulator;
		public ItemTrait PicoManipulator;
		public ItemTrait FemtoManipulator;

		public ItemTrait BasicMatterBin;
		public ItemTrait AdvancedMatterBin;
		public ItemTrait SuperMatterBin;
		public ItemTrait BluespaceMatterBin;

		public ItemTrait BasicMicroLaser;
		public ItemTrait HighPowerMicroLaser;
		public ItemTrait UltraHighPowerMicroLaser;
		public ItemTrait QuadUltraMicroLaser;

		public ItemTrait BasicScanningModule;
		public ItemTrait AdvancedScanningModule;
		public ItemTrait PhasicScanningModule;
		public ItemTrait TriphasicScanningModule;

		public ItemTrait BasicCapacitor;
		public ItemTrait AdvancedCapacitor;
		public ItemTrait SuperCapacitor;
		public ItemTrait QuadraticCapacitor;
	}
}