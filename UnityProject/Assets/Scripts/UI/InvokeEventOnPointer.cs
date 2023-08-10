using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace UI
{
	public class InvokeEventOnPointer : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
	{

		public UnityEvent OnEnter = new UnityEvent();
		public UnityEvent OnExit = new UnityEvent();

		private void OnDestroy()
		{
			OnEnter?.RemoveAllListeners();
			OnExit?.RemoveAllListeners();
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			OnEnter?.Invoke();
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			OnExit?.Invoke();
		}
	}
}