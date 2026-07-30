using UnityEngine;
using UnityEngine.EventSystems;

namespace US13.UI.Core.Animations
{
	[RequireComponent(typeof(ReversibleObjectScale))]
	public class ScaleOnHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
	{
		private ReversibleObjectScale objectScale;

		private void Awake()
		{
			objectScale = GetComponent<ReversibleObjectScale>();
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			objectScale.TweenScale(true);
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			objectScale.TweenScale(false);
		}
	}
}
