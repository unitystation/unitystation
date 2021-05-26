using UnityEngine;

namespace ScriptableObjects
{
	[CreateAssetMenu(fileName = "RightClickOptionSingleton", menuName = "Singleton/RightClickOptionSingleton")]
	public class RightClickOptionSingleton : SingletonScriptableObject<RightClickOptionSingleton>
	{
		[SerializeField]
		private RightClickOption[] rightClickOptions = new RightClickOption[0];
		public RightClickOption[] RightClickOptions => rightClickOptions;
	}
}
