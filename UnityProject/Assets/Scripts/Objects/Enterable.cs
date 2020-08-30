using Mirror;
using UnityEngine;
using UnityEngine.EventSystems;
using static UnityEngine.EventSystems.EventTrigger;

namespace Assets.Scripts.Objects
{
	/// <summary>
	/// Allows to trigger an event when a player enter in the space occupied by this component.
	/// </summary>
	public class Enterable: NetworkBehaviour
	{
		/// <summary>
		/// This is the function that will handle what happens when a player enters that enterable.
		/// </summary>
		[SerializeField]
		public TriggerEvent enterEvent = null;

		/// <summary>
		/// Triggers the enter event
		/// </summary>
		/// <param name="gameObjectEntering">The object entering</param>
		public void TriggerEnterEvent(GameObject gameObjectEntering)
		{
			BaseEventData eventData = new BaseEventData(EventSystem.current);
			eventData.selectedObject = gameObjectEntering;
			enterEvent.Invoke(eventData);
		}
	}
}
