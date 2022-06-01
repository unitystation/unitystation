using UnityEngine;

namespace Util
{
	/// <summary>
	/// Allows to check whether a component exists without doing a null check
	/// DO NOT USE for components on other objects, e.g using this to store a component from another object
	/// </summary>
	public class CheckedComponent<T> where T : MonoBehaviour
	{
		public T Component { get; private set; }

		public bool HasComponent { get; private set; }

		#region Constructors

		public CheckedComponent()
		{
			HasComponent = false;
			Component = null;
		}

		public CheckedComponent(GameObject currentGameObject)
		{
			ResetComponent(currentGameObject);
		}

		public CheckedComponent(MonoBehaviour currentMonoBehaviour)
		{
			ResetComponent(currentMonoBehaviour);
		}

		public CheckedComponent(T checkedMonoBehaviour)
		{
			SetComponent(checkedMonoBehaviour);
		}

		#endregion

		#region Resets

		public void DirectSetComponent(T In)
		{
			SetComponent(In);
		}

		public void ResetComponent(MonoBehaviour monoBehaviour)
		{
			SetComponent(monoBehaviour.GetComponent<T>());
		}

		public void ResetComponent(GameObject gameObject)
		{
			SetComponent(gameObject.GetComponent<T>());
		}

		public void SetToNull()
		{
			SetComponent(null);
		}

		#endregion

		private void SetComponent(T monoBehaviour)
		{
			Component = monoBehaviour;
			HasComponent = Component != null;
		}
	}
}