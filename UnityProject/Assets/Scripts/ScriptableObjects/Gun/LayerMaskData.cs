using UnityEngine;

namespace ScriptableObjects.Gun
{
	/// <summary>
	/// This scriptable object allows developers to change layers right at run time in play mode
	/// </summary>
	[CreateAssetMenu(fileName = "LayerMaskData", menuName ="ScriptableObjects/Gun/LayerMaskData", order = 0)]
	public class LayerMaskData : ScriptableObject
	{
		[SerializeField] private LayerMask layers;

		public LayerMask Layers => layers;
	}
}