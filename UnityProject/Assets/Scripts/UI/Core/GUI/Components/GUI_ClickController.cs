using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace UI.Core.GUI.Components
{
	public class GUI_ClickController : MonoBehaviour, IPointerClickHandler
	{
		public UnityEvent<GameObject> onLeft;
		public UnityEvent<GameObject> onRight;
		public UnityEvent<GameObject> onMiddle;

		public void OnPointerClick(PointerEventData eventData)
		{
			if (eventData.button == PointerEventData.InputButton.Left)
			{
				onLeft.Invoke(gameObject);
			}
			else if (eventData.button == PointerEventData.InputButton.Right)
			{
				onRight.Invoke(gameObject);
			}
			else if (eventData.button == PointerEventData.InputButton.Middle)
			{
				onMiddle.Invoke(gameObject);
			}
		}
	}
}
