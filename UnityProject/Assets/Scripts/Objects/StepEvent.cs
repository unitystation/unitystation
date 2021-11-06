using UnityEngine;
using UnityEngine.EventSystems;


namespace Objects
{
	[RequireComponent(typeof(Enterable))]

	public abstract class StepEvent : MonoBehaviour
	{
		public void OnEnterableData(BaseEventData eventData)
		{
			if (WillStep(eventData))
			{
				OnStep(eventData);
			}
		}

		public abstract void OnStep(BaseEventData eventData);
		public abstract bool WillStep(BaseEventData eventData);
	}

}
