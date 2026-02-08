using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace US13.UI.Core
{
	public class OnMouseEnterUI : MonoBehaviour, IPointerEnterHandler
	{
		[SerializeField] private UnityEvent OnMouseEnter;

		public void OnPointerEnter(PointerEventData eventData)
		{
			OnMouseEnter?.Invoke();
		}
	}
}