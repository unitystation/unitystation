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

		[Tooltip("Whether to scroll when the mouse is inside the full radius of the radial or just between the outer and inner radius of the radial.")]
		[SerializeField]
		private bool fullRadius;

		private IRadial RadialUI { get; set; }

		public void Awake()
		{
			RadialUI = GetComponent<IRadial>();
		}

		public void OnScroll(PointerEventData eventData)
		{
			if (!RadialUI.IsPositionWithinRadial(eventData.position, fullRadius))
			{
				return;
			}

			var scrollDelta = eventData.scrollDelta.y;
			if (scrollDelta < 0)
			{
				RadialUI.RotateRadial(RadialUI.ItemArcMeasure * scrollCount);
			}
			else if (scrollDelta > 0)
			{
				RadialUI.RotateRadial(-RadialUI.ItemArcMeasure * scrollCount);
			}
		}
	}
}