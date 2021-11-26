using Mirror;
using UnityEngine;
using UnityEngine.EventSystems;
using static UnityEngine.EventSystems.EventTrigger;

namespace Objects
{
	/// <summary>
	/// Allows to trigger an event when a player enter in the space occupied by this component.
	/// </summary>
	public interface IPlayerEntersTile
	{
		public abstract void OnStep(GameObject eventData);
		public abstract bool WillStep(GameObject eventData);
	}

	/// <summary>
	/// Allows to trigger an event when a player enter in the space occupied by this component.
	/// </summary>
	public interface IObjectEntersTile
	{
		public abstract void OnEnter(GameObject eventData);
		public abstract bool CanEnter(GameObject eventData);
	}
}
