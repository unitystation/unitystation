using UnityEngine;

namespace ScriptableObjects
{
	public class RightClickOptionsList : ScriptableObject
	{
		[SerializeField]
		private RightClickOption[] rightClickOptions = new RightClickOption[0];
		public RightClickOption[] RightClickOptions => rightClickOptions;
	}
}
