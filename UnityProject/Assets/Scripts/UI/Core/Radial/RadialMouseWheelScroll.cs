using UnityEngine;

namespace UI.Core.Radial
{
	[RequireComponent(typeof(IRadial))]
	public class RadialMouseWheelScroll : MonoBehaviour
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

		public void Update()
		{
			if (!RadialUI.IsPositionWithinRadial(CommonInput.mousePosition, fullRadius))
			{
				return;
			}

			var scrollDelta = Input.mouseScrollDelta.y;
			// Allow mouse wheel to scroll through items.
			if (scrollDelta < 0)
			{
				RadialUI.RotateRadial(RadialUI.ItemArcAngle * scrollCount);
			}
			else if (scrollDelta > 0)
			{
				RadialUI.RotateRadial(-RadialUI.ItemArcAngle * scrollCount);
			}
		}
	}
}