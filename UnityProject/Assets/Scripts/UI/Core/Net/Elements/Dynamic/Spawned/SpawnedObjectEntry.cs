using UnityEngine;
using UnityEngine.Events;

namespace UI.Core.NetUI
{
	/// <summary>
	/// For containing objects that are spawned ingame
	/// </summary>
	public class SpawnedObjectEntry : DynamicEntry
	{
		public GameObject TrackedObject {
			get => trackedObject;
			set {
				trackedObject = value;
				ReInit();
			}
		}

		private GameObject trackedObject;

		/// <summary>
		/// Define your logic of updating
		/// </summary>
		public ObjectChangeEvent OnObjectChange;

		/// <summary>
		/// Update entry's internal elements to inform peepers about tracked object updates,
		/// As this entry's value is simply its layout coordinates
		/// </summary>
		public void ReInit()
		{
			for (var i = 0; i < Elements.Length; i++)
			{
				var element = Elements[i];
				string nameBeforeIndex = element.name.Split(DELIMITER)[0];
				OnObjectChange.Invoke(trackedObject, nameBeforeIndex, element);
			}
		}
	}

	public class ObjectChangeEvent : UnityEvent<GameObject, string, NetUIElementBase> { }
}
