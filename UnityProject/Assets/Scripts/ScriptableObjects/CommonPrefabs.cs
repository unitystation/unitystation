using UnityEngine;

namespace ScriptableObjects
{
	/// <summary>
	/// Singleton, provides common prefabs with special purposes so they can be easily used
	/// in components without needing to be assigned in editor.
	/// </summary>
	[CreateAssetMenu(fileName = "CommonPrefabsSingleton", menuName = "Singleton/CommonPrefabs")]
	public class CommonPrefabs : SingletonScriptableObject<CommonPrefabs>
	{
		public GameObject Metal;
		public GameObject Plasteel;
		public GameObject ReinforcedGlassSheet;
		public GameObject GlassSheet;
		public GameObject WoodenPlank;
		public GameObject MetalRods;
		public GameObject GlassShard;
		public GameObject SingleCableCoil;
		public GameObject Mask;
		public GameObject EmergencyOxygenTank;
		public GameObject MachineFrame;
		public GameObject CameraAssembly;

		public GameObject DiamondSheet;

		public GameObject SparkEffect;

		public GameObject BurningSmall;
		public GameObject BurningLarge;

		public GameObject Cube3D;

		public GameObject DirtyFloorDecal;
	}
}
