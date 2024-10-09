using UnityEngine;

namespace ScriptableObjects.Health
{
	[CreateAssetMenu(fileName = "BodyPartsBaseSO", menuName = "Body Parts/Base Body Parts", order = 1)]
	public class BodyPartsBaseSO : ScriptableObject
	{
		public GameObject HeadBase;
		public GameObject TorsoBase;
		public GameObject ArmRightBase;
		public GameObject ArmLeftBase;
		public GameObject LegRightBase;
		public GameObject LegLeftBase;
	}
}