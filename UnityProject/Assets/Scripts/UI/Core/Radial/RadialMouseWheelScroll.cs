using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.Core.Radial
{
	[RequireComponent(typeof(IRadial))]
	public class RadialMouseWheelScroll : MonoBehaviour, IScrollHandler
	{
		[Tooltip("The number of items to scroll when using the mouse wheel.")]
		[SerializeField]
		private int scrollCount;

		[Tooltip("Whether to scroll when the mouse is inside the full radius of the radial or only inside the annulus.")]
		[SerializeField]
		private bool fullRadius;

		private IRadial RadialUI { get; set; }

		public Action<PointerEventData> OnScrollEvent { get; set; }

		public int ScrollCount
		{
			get => scrollCount;
			set => scrollCount = value;
		}

		public bool FullRadius
		{
			get => fullRadius;
			set => fullRadius = value;
		}

		public void Awake()
		{
			RadialUI = GetComponent<IRadial>();
		}

		public void OnScroll(PointerEventData eventData)
		{
			if (eventData.scrollDelta == Vector2.zero || eventData.dragging || RadialUI.IsPositionWithinRadial(eventData.position, fullRadius) == false)
			{
				return;
			}

			var scrollDelta = eventData.scrollDelta;
			var delta = RadialUI.ItemArcMeasure * scrollCount;

			if (scrollDelta.y < 0)
			{
				scrollDelta.y = delta;
			}
			else
			{
				scrollDelta.y = -delta;
			}

			eventData.scrollDelta = scrollDelta;
			OnScrollEvent?.Invoke(eventData);
		}
	}
}