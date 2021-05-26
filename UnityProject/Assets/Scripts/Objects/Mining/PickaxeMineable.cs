using UnityEngine;

namespace Objects.Mining
{
	/// <summary>
	/// Simple script to allow objects to be mined. Destroys the object upon completion.
	/// </summary>
	public class PickaxeMineable : MonoBehaviour
	{
		[SerializeField] private float mineTime = 3;
		public float MineTime => mineTime;

	
	}
}
