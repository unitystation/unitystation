using UnityEngine;

namespace Objects.Mining
{
	/// <summary>
	/// Simple script to allow objects to be mined. Destroys the object upon completion.
	/// </summary>
	public class PickaxeMineable : MonoBehaviour
	{
		[SerializeField] private float mineTime = 3;

		[Range(1, 10)]
		[Tooltip("How hard is this object to mine? Higher means certain tools cannot mine it.")]
		[SerializeField] private int mineableHardness = 1;

		public float MineTime => mineTime;
		public int MineableHardness => mineableHardness;
	
	}
}
